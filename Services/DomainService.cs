using System.IO;
using Toltech.App.Models;
using Toltech.App.Services.Logging;
using Toltech.App.Services.Notification;
using Toltech.App.ToltechCalculation.Helpers;
using Toltech.App.Utilities;
using Toltech.App.Utilities.Result;
using static Toltech.App.Services.EventsManager;

namespace Toltech.App.Services
{
    // Domain catch → ErrorCode.Unknown : valide si pas de besoin de distinguer les erreurs DB. TO DO Affiner plus tard si nécessaire .
    /// <summary>
    /// A clean business orchestration layer, independent from the UI,
    /// that centralizes business rules and returns results consumable by the ViewModel
    /// </summary>
    public class DomainService
    {
        private readonly INotificationService _notificationService;
        private readonly ILoggerService _logger;

        private readonly DatabaseService _databaseService;
        private readonly MetaModelDatabaseService _dbModelService;
        private readonly ComputeValidationService _computeValidationService;
        public DomainService(
            DatabaseService databaseService,
            MetaModelDatabaseService dbModelService,
            ComputeValidationService computeValidationService,
            ILoggerService loggerService
            )
        {
            _databaseService = databaseService;
            _dbModelService = dbModelService;
            _computeValidationService = computeValidationService;
            _logger = loggerService;

            _notificationService = App.NotificationService;
        }

        #region Service ModelData

        public async Task<Result<PartWithDatasResult>> CreatePartAndDatasAsync(string nomPiece)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result<PartWithDatasResult>.Failure("No active model.", ErrorCode.NoActiveModel);

                if (NameValidationHelper.NamingValidation(nomPiece).IsFailure)
                    return Result<PartWithDatasResult>.Failure("Nom de pièce invalide.", ErrorCode.Unknown);

                // 1. vérification existence
                if (await _databaseService.IsNamePartExisteAsync(nomPiece))
                    return Result<PartWithDatasResult>.Failure("Le nom de la pièce existe déjà dans la base.", ErrorCode.Unknown);

                // 2. création part
                var newPart = await InsertPartAsync(nomPiece);

                var datas = await AddDataOfPartExtremiteAsync(newPart.Id, 6);

                _ = _notificationService.ShowNotifAsync($"Pièce \"{nomPiece}\" ajoutée avec succès !", false);



                return Result<PartWithDatasResult>.Success(new PartWithDatasResult
                {
                    Part = newPart,
                    Datas = datas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("CreatePartAndDatas failed", "", ex);
                return Result<PartWithDatasResult>.Failure("Une erreur est survenue lors de la création de la pièce et des données.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<ModelData>> CreateDataAsync(int idPartActif)
        {
            if (!ModelValidationHelper.CheckModelActif(false))
                return Result<ModelData>.Failure("No active model.", ErrorCode.NoActiveModel);

            bool isExist = await _databaseService.PartExistsByIdAsync(idPartActif);

            if (!isExist)
                return Result<ModelData>.Failure("Pas de pièce valide à la création.", ErrorCode.InvalidInput);
            try
            {
                var newDatas = await AddDataOfPartExtremiteAsync(idPartActif, 1);

                var firstData = newDatas.FirstOrDefault();

                if (firstData == null)
                    return null;

                var data = new ModelData();
                data.LoadFromDb(firstData);

                return Result<ModelData>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateData failed", "", ex);
                return Result<ModelData>.Failure("Une erreur est survenue lors de la création de la donnée.", ErrorCode.Unknown);
            }
        }

        public Task<Result> DeleteDataAsync(ModelData data)
        {
            return DeleteDataAsync(new[] { data });
        }
        public async Task<Result> DeleteDataAsync(IEnumerable<ModelData> datas)
        {
            try
            {
                var list = datas?.Where(d => d != null && d.Id > 0).ToList();

                if (list == null || list.Count == 0)
                    return Result.Failure("Aucune donnée valide à supprimer.", ErrorCode.Unknown);

                // 1. récupération des entités existantes en base
                var ids = list.Select(d => d.Id).ToList();

                var existingDatas = await _databaseService.GetModelDataByIdsAsync(ids);

                if (existingDatas == null || existingDatas.Count == 0)
                    return Result.Failure("Aucune correspondance trouvée en base de données.", ErrorCode.Unknown);

                // 2. suppression atomique
                await _databaseService.DeleteRangeAsync(existingDatas);

                // 3. notification batch
                _ = _notificationService.ShowNotifAsync(
                    $"{ids.Count} donnée(s) supprimée(s) avec succès.",
                    false);

                // 4. events métier



                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteDataAsync failed", "", ex);
                return Result.Failure(
                    "Une erreur est survenue lors de la suppression des données.",
                    ErrorCode.Unknown);
            }
        }
        public async Task<Result> DeleteDatasByIdsAsync(int dataId)
        {
            return await DeleteDatasByIdsAsync(new[] { dataId });
        }
        public async Task<Result> DeleteDatasByIdsAsync(IEnumerable<int> dataIds)
        {
            try
            {
                // 1. Validation
                var ids = dataIds?
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (ids == null || ids.Count == 0)
                    return Result.Failure("Aucune donnée valide à supprimer.", ErrorCode.Unknown);

                // 2. Vérifier existence
                var existingDatas = await _databaseService.GetModelDataByIdsAsync(ids);

                if (existingDatas == null || existingDatas.Count == 0)
                    return Result.Failure("Aucune correspondance trouvée en base de données.", ErrorCode.NotFound);

                // Option : ne garder que ceux réellement présents
                var existingIds = existingDatas.Select(d => d.Id).ToList();

                // 3. Suppression (idéalement transactionnelle)
                await _databaseService.DeleteRangeAsync(existingDatas);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteDatasByIdsAsync failed", "", ex);

                return Result.Failure(
                    "Une erreur est survenue lors de la suppression des données.",
                    ErrorCode.Unknown);
            }
        }
        /// <summary>
        /// Supprime une part et ses données associées.
        /// </summary>
        public async Task<Result<PartWithDatasResult>> DeletePartWithDatasByIdAsync(int idPart)
        {
            var result = await DeletePartsWithDatasByIdsAsync(new List<int> { idPart });
            if (result.IsFailure) return Result<PartWithDatasResult>.Failure(result.Error);

            var single = result.Value.FirstOrDefault();
            return single is not null
                ? Result<PartWithDatasResult>.Success(single)
                : Result<PartWithDatasResult>.Failure("Part introuvable.", ErrorCode.NotFound);
        }

        /// <summary>
        /// Supprime une collection de parts et leurs données associées en une seule transaction.
        /// Retourne les données de chaque part avant suppression.
        /// </summary>
        public async Task<Result<IReadOnlyList<PartWithDatasResult>>> DeletePartsWithDatasByIdsAsync(
            IReadOnlyList<int> idParts)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result<IReadOnlyList<PartWithDatasResult>>.Failure(
                        "No active model.", ErrorCode.NoActiveModel);

                if (idParts is null || !idParts.Any())
                    return Result<IReadOnlyList<PartWithDatasResult>>.Failure(
                        "Aucun id fourni.", ErrorCode.InvalidInput);

                // 1. Charger les données AVANT suppression
                var results = new List<PartWithDatasResult>();
                foreach (var id in idParts)
                {
                    var part = await _databaseService.GetPartByIdAsync(id);
                    var datas = await _databaseService.GetModelDataByPartIdAsync(id);
                    results.Add(new PartWithDatasResult
                    {
                        Part = part,
                        Datas = datas ?? new List<ModelData>()
                    });
                }

                // 2. Suppression atomique
                await _databaseService.DeletePartsWithDatasRangeAsync(idParts.ToList());

                // 3. Notification
                var label = idParts.Count == 1
                    ? $"Données supprimées pour la pièce {idParts[0]}."
                    : $"{idParts.Count} pièces supprimées.";

                _ = _notificationService.ShowNotifAsync(label, false);

                return Result<IReadOnlyList<PartWithDatasResult>>.Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeletePartsWithDatasByIds failed", "", ex);
                return Result<IReadOnlyList<PartWithDatasResult>>.Failure(
                    "Une erreur est survenue lors de la suppression.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<string?>> GetPartNameByIdAsync(int idPart)
        {
            try
            {
                if (idPart <= 0)
                    return Result<string?>.Failure("ID de pièce invalide.", ErrorCode.Unknown);

                var partName = await _databaseService.GetPartNameByID(idPart);
                return Result<string?>.Success(partName);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPartNameById failed", "", ex);
                return Result<string?>.Failure("Une erreur est survenue lors de la récupération du nom de la pièce.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> UpdateModelDataAsync(List<ModelData> toSave)
        {
            foreach (var d in toSave)
                d.MarkSaving();

            try
            {
                var renamed = await ResolveUniqueNamesAsync(toSave, _databaseService.NumberOfNameDataAsync);

                foreach (var (original, resolved) in renamed)
                    _logger.LogInfo($"Contact renommé : \"{original}\" → \"{resolved}\"");

                await _databaseService.UpdateRangeAsync(toSave);

                foreach (var d in toSave)
                {
                    d.ClearDirty();
                    d.ClearSaving();
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                foreach (var d in toSave.Where(d => d.IsSaving))
                {
                    d.MarkOutOfSync();
                    d.ClearSaving();
                }

                _logger.LogError("SaveModelData failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la sauvegarde des données.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> UpdateModelDataNameAsync(int dataId, string newName)
        {
            try
            {
                // Validation ID
                if (dataId <= 0)
                    return Result.Failure("ID invalide.", ErrorCode.Unknown);

                // Validation nom
                if (string.IsNullOrWhiteSpace(newName))
                    return Result.Failure("Nom invalide.", ErrorCode.Unknown);

                // Récupération entité
                var data = await _databaseService.GetModelDataByIdAsync(dataId);

                if (data == null)
                    return Result.Failure("Donnée introuvable.", ErrorCode.NotFound);

                // Mise à jour
                data.Model = newName.Trim();

                var renamed = await ResolveUniqueNamesAsync(data, _databaseService.NumberOfNameDataAsync);

                foreach (var (original, resolved) in renamed)
                    _logger.LogInfo($"Contact renommé : \"{original}\" → \"{resolved}\"");

                await _databaseService.UpdateAsync(data);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateModelDataNameAsync failed", "", ex);

                return Result.Failure(
                    "Erreur lors de la mise à jour de la donnée.",
                    ErrorCode.Unknown);
            }
        }

        public async Task<Result<List<ModelData>>> LoadPartDataAsync(int partId)
        {
            try
            {
                var data = await _databaseService.GetModelDataByPartIdAsync(partId);
                return Result<List<ModelData>>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadPartData failed", "", ex);
                return Result<List<ModelData>>.Failure("Une erreur est survenue lors du chargement des données de la pièce.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<IEnumerable<ModelData>>> LoadSortedDataAsync(List<ModelData> dataList, int partId)
        {
            try
            {
                var data = await _databaseService.GetDatasSortedByNodeOrder(dataList, partId);
                return Result<IEnumerable<ModelData>>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadSortedData failed", "", ex);
                return Result<IEnumerable<ModelData>>.Failure("Une erreur est survenue lors du chargement des données triées.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<List<ModelData>>> SortDatasAsync(List<ModelData> datas, int partId)
        {
            try
            {
                var data = await _databaseService.GetDatasSortedByNodeOrder(datas, partId);
                return Result<List<ModelData>>.Success(data.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("SortDatas failed", "", ex);
                return Result<List<ModelData>>.Failure("Une erreur est survenue lors du tri des données.", ErrorCode.Unknown);
            }
        }
        public async Task<Result<ValidationResult>> CheckIsoPartAsync(int? selectedPartId)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result<ValidationResult>.Failure("No active model.", ErrorCode.NoActiveModel);

                if (!selectedPartId.HasValue)
                    return Result<ValidationResult>.Failure("Aucune pièce sélectionnée.", ErrorCode.Unknown);

                // 1. récupération part
                Part partActif = await _databaseService.GetPartByIdAsync(selectedPartId.Value);

                if (partActif == null)
                    return Result<ValidationResult>.Failure("Pièce introuvable.", ErrorCode.Unknown);

                // 2. validation métier ISO
                var inverse = await _computeValidationService.ValidationPart(partActif);
                return Result<ValidationResult>.Success(inverse);
            }
            catch (Exception ex)
            {
                _logger.LogError("CheckIsoPart failed", "", ex);
                return Result<ValidationResult>.Failure("Une erreur est survenue lors de la vérification ISO de la pièce.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<List<Part>>> UpdateFixedPartAsync(Part part)
        {
            try
            {
                if (part == null)
                    return Result<List<Part>>.Failure("Part is null", ErrorCode.Unknown);

                var result = await _databaseService.SetFixedPartAsync(part);

                return Result<List<Part>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateFixedPart failed", "", ex);
                return Result<List<Part>>.Failure("Error UpdateFixedPartAsync", ErrorCode.Unknown);
            }
        }

        public async Task<Result<Part?>> GetFixedPartAsync()
        {
            try
            {
                var part = await _databaseService.GetFixedPartAsync();
                return Result<Part?>.Success(part);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFixedPart failed", "", ex);
                return Result<Part?>.Failure("Une erreur est survenue lors de la récupération du Part fixé.", ErrorCode.Unknown);
            }
        }

        public async Task ToggleActiveModelDataByIdAsync(int modelDataId)
        {
            try
            {
                if (modelDataId <= 0)
                    return;

                var data = await _databaseService.GetModelDataByIdAsync(modelDataId);

                if (data == null)
                    return;

                data.Active = !data.Active;

                await _databaseService.UpdateAsync(data);
            }
            catch (Exception ex)
            {
                _logger.LogError("ToggleActiveModelDataByIdAsync failed", "", ex);
                throw;
            }
        }

        #region Helper Methods
        private async Task<List<ModelData>> AddDataOfPartExtremiteAsync(int partId, int count)
        {
            if (partId <= 0)
                throw new ArgumentException(nameof(partId));

            if (count <= 0)
                throw new ArgumentException(nameof(count));

            var datas = CreateDefaultModelDatas(partId, count);

            await _databaseService.InsertRangeAsync(datas);

            return datas;
        }

        private static List<ModelData> CreateDefaultModelDatas(int partId, int count)
        {
            var result = new List<ModelData>(count);

            for (int i = 0; i < count; i++)
            {
                string randomName = $"PO_{Guid.NewGuid().ToString("N")[..6]}";

                bool useRandom = true;
                result.Add(new ModelData
                {
                    CoordX = useRandom ? Random.Shared.Next(-300, 301) : 0,
                    CoordY = useRandom ? Random.Shared.Next(-300, 301) : 0,
                    CoordZ = useRandom ? Random.Shared.Next(-300, 301) : 0,

                    CoordU = useRandom ? Random.Shared.Next(-300, 301) : 1,
                    CoordV = useRandom ? Random.Shared.Next(-300, 301) : 0,
                    CoordW = useRandom ? Random.Shared.Next(-300, 301) : 0,

                    OriginePartId = 0,
                    ExtremitePartId = partId,

                    Active = true,
                    Model = randomName
                });
            }

            return result;
        }

        #endregion

        #endregion

        #region Service Requirements
        public async Task<Result<Requirements?>> CreateRequirementAsync()
        {
            string nomRequirement = $"Req_{Guid.NewGuid().ToString("N")[..6]}";

            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result<Requirements?>.Failure("No active model.", ErrorCode.NoActiveModel);

                if (NameValidationHelper.NamingValidation(nomRequirement).IsFailure)
                    return Result<Requirements?>.Failure("Nom du requirement invalide", ErrorCode.InvalidInput);

                // 1. vérification doublon
                if (await _databaseService.NameReqExisteAsync(nomRequirement))
                {
                    _ = _notificationService.ShowNotifAsync(
                        $"L'exigence \"{nomRequirement}\" existe déjà.",
                        true);

                    return Result<Requirements?>.Failure("Nom d'exigence déjà similaire", ErrorCode.InvalidInput);
                }

                var newRequirement = new Requirements
                {
                    NameReq = nomRequirement,
                    PartReq1Id = 0,
                    PartReq2Id = 0,
                    Description1 = "",
                    Description2 = "",
                    tol1 = 0,
                    tol2 = 0,
                    CoordX = 0,
                    CoordY = 0,
                    CoordZ = 0,
                    CoordU = 1, // Vecteur direction par défaut
                    CoordV = 0,
                    CoordW = 0,
                    IsActive = true,
                };

                // 2. création DB
                await _databaseService.InsertAsync(newRequirement);

                // 3. mapping métier → UI model
                var uiModel = new Requirements();
                uiModel.LoadFromDb(newRequirement);

                // 4. notification
                _ = _notificationService.ShowNotifAsync($"Exigence \"{nomRequirement}\" ajoutée avec succès !", false);



                return Result<Requirements?>.Success(uiModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateRequirement failed", "", ex);

                _ = _notificationService.ShowNotifAsync(
                    $"Erreur lors de la création de \"{nomRequirement}\".",
                    true);

                return Result<Requirements?>.Failure("Erreur innatentu lors de la création de l'exigence", ErrorCode.Unknown);
            }
        }
        public async Task<Result<List<Requirements>>> LoadAllRequirementsAsync()
        {
            try
            {
                var modelsFromDb = await _databaseService.GetAllRequirementsAsync();

                var result = modelsFromDb
                    .Select(dbModel =>
                    {
                        var uiModel = new Requirements();
                        uiModel.LoadFromDb(dbModel);
                        return uiModel;
                    })
                    .ToList();

                return Result<List<Requirements>>.Success(result); ;
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadAllRequirements failed", "", ex);
                return Result<List<Requirements>>.Failure("Erreur lors du chargement des exigences.", ErrorCode.Unknown); ;
            }
        }

        public async Task<Result> SaveRequirementsAsync(List<Requirements> toSave)
        {
            foreach (var req in toSave)
                req.MarkSaving();

            try
            {
                var renamed = await ResolveUniqueNamesAsync(toSave, _databaseService.NumberOfReqAsync);

                foreach (var (original, resolved) in renamed)
                    _logger.LogInfo($"Requirement renommé : \"{original}\" → \"{resolved}\"");

                await _databaseService.UpdateRangeAsync(toSave);

                foreach (var req in toSave)
                {
                    req.ClearDirty();
                    req.ClearSaving();
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                foreach (var req in toSave.Where(r => r.IsSaving))
                    req.ClearSaving();

                _logger.LogError("SaveRequirements failed", "", ex);
                return Result.Failure("Erreur lors de la sauvegarde des exigences.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> UpdateRequirementNameAsync(int? requirementId, string newName)
        {
            try
            {
                if (!requirementId.HasValue || requirementId.Value <= 0)
                    return Result.Failure("ID de requirement invalide.", ErrorCode.Unknown);

                if (string.IsNullOrWhiteSpace(newName))
                    return Result.Failure("Nom invalide.", ErrorCode.Unknown);

                var requirement = await _databaseService.GetReqsByIdAsync(requirementId.Value);

                if (requirement == null)
                    return Result.Failure("Requirement introuvable.", ErrorCode.NotFound);

                requirement.NameReq = newName.Trim();

                var renamed = await ResolveUniqueNamesAsync(requirement, _databaseService.NumberOfReqAsync);

                foreach (var (original, resolved) in renamed)
                    _logger.LogInfo($"Requirement renommé : \"{original}\" → \"{resolved}\"");

                await _databaseService.UpdateAsync(requirement);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateRequirementNameAsync failed", "", ex);

                return Result.Failure(
                    "Erreur lors de la mise à jour du requirement.",
                    ErrorCode.Unknown);
            }
        }

        public Task<Result> DeleteRequirementAsync(Requirements req)
        {
            return DeleteRequirementAsync(new[] { req });
        }

        public async Task<Result> DeleteRequirementAsync(IEnumerable<Requirements> reqs)
        {
            try
            {
                var list = reqs?.Where(r => r != null).ToList();

                if (list == null || list.Count == 0)
                    return Result.Failure("Exigence invalide.", ErrorCode.InvalidInput);

                await _databaseService.DeleteRangeAsync(list);




                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Delete requirement failed ({reqs?.Count() ?? 0} items)",
                    "",
                    ex);

                return Result.Failure(
                    "Erreur lors de la suppression de l'exigence.",
                    ErrorCode.Unknown);
            }
        }

        public async Task<Result> ToggleActiveRequirementByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue || idReq.Value <= 0)
                    return Result.Failure("ID de l'exigence invalide.", ErrorCode.InvalidInput);

                // 1. récupération métier
                var req = await _databaseService.GetReqsByIdAsync(idReq.Value);

                if (req == null)
                    return Result.Failure("Exigence introuvable.", ErrorCode.NotFound);

                // 2. règle métier
                req.IsActive = !req.IsActive;

                await _databaseService.UpdateAsync(req);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("ToggleActiveRequirementByIdAsync failed", "", ex);

                return Result.Failure(
                    "Une erreur est survenue lors de la modification de l'exigence.",
                    ErrorCode.Unknown);
            }
        }

        public async Task<Result> DeleteRequirementByIdAsync(int? idReq)
        {
            var req = await _databaseService.GetReqsByIdAsync(idReq);

            if (req == null)
                return Result.Failure("Exigence introuvable.", ErrorCode.NotFound);

            return await DeleteRequirementAsync(new[] { req });
        }

        /// <summary>
        /// Wrapper
        /// </summary>
        public async Task<Result<Requirements>> GetReqByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue || idReq.Value <= 0)
                    return Result<Requirements>.Failure("ID de l'exigence invalide.", ErrorCode.InvalidInput);

                var requirement = await _databaseService.GetReqsByIdAsync(idReq.Value);

                return Result<Requirements>.Success(requirement);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRequirementNameById failed", "", ex);
                return Result<Requirements>.Failure("Une erreur est survenue lors de la récupération du nom de l'exigence.", ErrorCode.Unknown);
            }
        }

        #region Helper Requirements

        #endregion

        #endregion

        #region Service Model

        private async Task NotifyModelOpen(string path)
        {

            await EventsManager.RaiseModelOpenedAsync(new ModelOpenedEvent
            {
                Path = path,
            });
        }
        public async Task<Result> CreateModelAsync(string modelName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelName))
                    return Result.Failure("Le nom du modèle est invalide.");

                if (NameValidationHelper.NamingValidation(modelName).IsFailure)
                    return Result.Failure("Le nom du modèle est invalide.");

                // 1. Construction du chemin
                string modelPath = Path.Combine(
                    ModelManager.AppDataPath,
                    modelName + ".tolx");

                // 2. Vérification existence
                if (File.Exists(modelPath))
                {
                    return Result.Failure("Le modèle existe déjà à l'emplacement par défaut.");
                }

                // 3. Définir modèle actif
                ModelManager.ModelActif = modelPath;

                // 1. création physique
                await _databaseService.CreateDatabaseAsync(modelPath);

                // 2. ouverture
                await _databaseService.Open(modelPath);

                // 4. enregistrement global
                var modelId = await RegisterModelAsync(modelName, modelPath);

                await NotifyModelOpen(modelPath);

                // 7. Notification
                _ = _notificationService.ShowNotifAsync(
                    $"Modèle '{modelName}' créé avec succès",
                    false);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateModel failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la création du modèle.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> OpenModelAsync(string selectedFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedFile))
                    return Result.Failure("Le fichier sélectionné est invalide.");

                // 1. Notification
                _ = _notificationService.ShowNotifAsync(
                    $"Modèle '{System.IO.Path.GetFileNameWithoutExtension(selectedFile)}' ouvert",
                    false);

                // 2. Mise à jour modèle actif
                ModelManager.ModelActif = selectedFile;

                // 3. Switch DB (IMPORTANT : instance existante)
                await _databaseService.Open(selectedFile);

                await NotifyModelOpen(selectedFile);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("OpenModel failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de l'ouverture du modèle.");
            }
        }

        public async Task<Result> DeleteModelAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return Result.Failure("Le chemin du modèle est invalide.");

                // 1. Si modèle actif → fermer la DB
                if (path == ModelManager.ModelActif)
                {
                    await _databaseService.CloseConnection();
                }

                // 2. Suppression avec retry (verrou fichier)
                var deleted = false;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(path);
                        deleted = true;
                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(100);
                    }
                }

                if (!deleted)
                    return Result.Failure(
                        "Le fichier est verrouillé par une autre application.",
                        ErrorCode.FileLocked);

                // 3. Suppression meta
                await DeleteMetaModelAsync(path);

                // 4. Réouvrir DB par défaut (temp/template)
                await _databaseService.Open();

                await NotifyModelOpen("");

                // 5. Notification
                _ = _notificationService.ShowNotifAsync($"Modèle '{System.IO.Path.GetFileNameWithoutExtension(path)}' supprimé", false);

                return Result.Success();
            }
            catch (UnauthorizedAccessException)
            {
                return Result.Failure(
                    "Accès refusé : permissions insuffisantes pour supprimer ce fichier.",
                    ErrorCode.Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteModel failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la suppression du modèle.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> DuplicateModelAsync(string sourcePath, string destinationFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationFolder))
                    return Result.Failure("Le chemin source ou le dossier de destination est invalide.", ErrorCode.InvalidPath);

                if (!File.Exists(sourcePath))
                    return Result.Failure("Le fichier source n'existe pas.", ErrorCode.NotFound);

                string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                string extension = Path.GetExtension(sourcePath);

                string newFilePath = Path.Combine(destinationFolder, fileName + "_copy" + extension);

                int counter = 2;

                // Boucle tant que le fichier existe
                while (File.Exists(newFilePath))
                {
                    newFilePath = Path.Combine(
                        destinationFolder,
                        $"{fileName}_copy_{counter}{extension}"
                    );

                    counter++;
                }

                // 1. duplication fichier
                File.Copy(sourcePath, newFilePath, overwrite: false);

                // 2. mise à jour modèle actif
                ModelManager.ModelActif = newFilePath;

                // 3. enregistrement meta
                await RegisterModelAsync(fileName, newFilePath);

                // 4. ouverture DB modèle
                await _databaseService.Open(newFilePath);
                await NotifyModelOpen(newFilePath);
                // 5. notification
                _ = _notificationService.ShowNotifAsync(
                    "Modèle dupliqué avec succès",
                    false);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("DuplicateModel failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la duplication du modèle.", ErrorCode.Unknown);
            }
        }

        private async Task DeleteMetaModelAsync(string fullPath)
        {
            try
            {
                var exists = await MetaModelDatabaseService.ActiveInstance.IsExistModelDBAsync(fullPath);
                if (!exists)
                    return;

                await MetaModelDatabaseService.ActiveInstance.DeleteModelFromMetaDbAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteMetaModel failed", "", ex);
            }
        }

        /// <summary>
        /// Crée un ID unique pour le model et le lié a la DB global
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="nameModel"></param>
        /// <param name="modelPath"></param>
        /// <returns></returns>
        public async Task<Result<Guid>> RegisterModelAsync(string nameModel, string modelPath)
        {
            try
            {
                Guid modelId = Guid.NewGuid();

                if (modelPath == null)
                    modelPath = ModelManager.ModelActif;

                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    return Result<Guid>.Failure("Invalid model path.", ErrorCode.Unknown);
                }

                if ((await IsExistModelRegisterAsync(modelPath)).IsSuccess)
                    return Result<Guid>.Success(modelId);

                if (NameValidationHelper.NamingValidation(nameModel).IsFailure)
                {
                    return Result<Guid>.Failure("Invalid model name.", ErrorCode.Unknown);
                }

                // Fermer la connexion base de données si ouverte (en tâche de fond)
                await Task.Run(() => _databaseService.CloseConnection());

                // 4. enregistrement global
                await _databaseService.InitializeModelAsync(modelId, nameModel, modelPath);
                await MetaModelDatabaseService.ActiveInstance.RegisterModelAsync(modelId, modelPath, "");

                return Result<Guid>.Success(modelId);

            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure("Error during model registration.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> IsExistModelRegisterAsync(String modelPath)
        {
            var IsExist = await MetaModelDatabaseService.ActiveInstance.IsExistModelDBAsync(modelPath);
            if (IsExist)
            {
                return Result.Success();
            }
            return Result.Failure("Model not found.", ErrorCode.None);
        }

        public async Task<Result> SaveModelAsync(ModelMeta meta)
        {
            try
            {
                if (meta == null)
                    return Result.Failure("Invalid model state.", ErrorCode.Unknown);
                if (!meta.IsDirty || meta.IsSaving)
                    return Result.Success();

                meta.MarkSaving();

                // 3. règle métier
                meta.LastModified = DateTime.Now;

                // 4. update meta DB
                await _dbModelService.UpdateModelMetaAsync(meta);

                // 5. commit état UI
                meta.ClearDirty();
                meta.ClearSaving();

                return Result.Success();
            }
            catch (Exception ex)
            {
                meta.MarkOutOfSync();
                meta.ClearSaving();

                _logger.LogError("SaveModel failed", "", ex);
                return Result.Failure("Error during save operation.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<List<ModelMeta>>> LoadModelsAsync()
        {
            try
            {
                var modelsFromDb = await _dbModelService.GetAllModelsAsync();

                var models = modelsFromDb
                             .Select(dbModel =>
                             {
                                 var uiModel = new ModelMeta();
                                 uiModel.LoadFromDb(dbModel);
                                 return uiModel;
                             })
                             .ToList();

                return Result<List<ModelMeta>>.Success(models);
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadModels failed", "", ex);
                return Result<List<ModelMeta>>.Failure(
                    "Impossible de charger les modèles.",
                    ErrorCode.DatabaseError);
            }
        }

        #endregion

        #region Service Part

        /// <summary>
        /// Create new Part
        /// TO DO : Create Batch function for multiple add part
        /// </summary>
        /// <param name="newPart"></param>
        /// <returns></returns>
        public async Task<int> InsertPartAsync(Part newPart)
        {
            if (newPart == null) newPart = CreateDefaultPart("");

            var renamed = await ResolveUniqueNamesAsync(newPart, _databaseService.NumberOfNamePartAsync);

            await _databaseService.InsertAsync(newPart);

            return newPart.Id;
        }

        /// <summary>
        /// Delete multiple Parts and their associated data
        /// </summary>
        /// <param name="parts"></param>
        /// <returns>
        /// A <see cref="Result"/> indicating whether the operation succeeded or failed.
        /// </returns>
        public async Task<Result> DeletePartsAsync(IEnumerable<Part> parts)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result.Failure("No active model.", ErrorCode.NoActiveModel);

                var partList = parts?.ToList();

                if (partList == null || partList.Count == 0)
                    return Result.Success();

                var ids = partList.Select(p => p.Id).ToList();

                // Suppression atomique
                await _databaseService.DeletePartsWithDatasRangeAsync(ids);

                // Notification (batch)
                _ = _notificationService.ShowNotifAsync(
                    $"{ids.Count} pièce(s) supprimée(s).",
                    false);

                // Events métier






                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("DeletePartsAsync failed", "", ex);
                return Result.Failure(
                    "Une erreur est survenue lors de la suppression des pièces.",
                    ErrorCode.Unknown);
            }
        }

        /// <summary>
        /// Update one Part
        /// </summary>
        public async Task<Result> UpdatePartsAsync(Part part)
        {
            return await UpdatePartsAsync(new[] { part });
        }

        /// <summary>
        /// Update multiple Parts
        /// </summary>
        /// <param name="parts"></param>
        /// <returns>
        /// A <see cref="Result"/> indicating whether the operation succeeded or failed.
        /// </returns>
        public async Task<Result> UpdatePartsAsync(IEnumerable<Part> parts)
        {
            var list = parts?.Where(p => p != null).ToList() ?? new List<Part>();
            if (list.Count == 0)
                return Result.Success();

            try
            {
                // --- Validation métier (exemple : unicité des noms) ---
                var renamed = await ResolveUniqueNamesAsync(list, _databaseService.NumberOfNamePartAsync);

                foreach (var (original, resolved) in renamed)
                    _logger.LogInfo($"Part renommée : \"{original}\" → \"{resolved}\"");

                // --- Séparation insert / update ---
                var toInsert = list.Where(p => p.Id == 0).ToList();
                var toUpdate = list.Where(p => p.Id != 0).ToList();

                // --- Transaction unique ---
                await _databaseService.SavePartsRangeAsync(toUpdate, toInsert);

                // Reset uniquement après succès global
                foreach (var part in parts)
                    part.ClearDirty();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("SavePartsAsync failed", "", ex);
                return Result.Failure("Erreur lors de la sauvegarde.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> UpdatePartNameAsync(int partId, string newName)
        {
            try
            {
                if (partId <= 0)
                    return Result.Failure("ID de pièce invalide.", ErrorCode.Unknown);

                if (string.IsNullOrWhiteSpace(newName))
                    return Result.Failure("Nom invalide.", ErrorCode.Unknown);

                var part = await _databaseService.GetPartByIdAsync(partId);

                if (part == null)
                    return Result.Failure("Pièce introuvable.", ErrorCode.NotFound);

                part.NamePart = newName.Trim();

                await _databaseService.UpdateAsync(part);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdatePartNameAsync failed", "", ex);

                return Result.Failure(
                    "Erreur lors de la mise à jour de la pièce.",
                    ErrorCode.Unknown);
            }
        }

        public async Task<Result> ToggleActivePartByIdAsync(int idPart)
        {
            try
            {
                if (idPart <= 0)
                    return Result.Failure("ID de pièce invalide.", ErrorCode.Unknown);

                var partResult = await GetPartByIdAsync(idPart);

                if (!partResult.IsSuccess || partResult.Value == null)
                    return Result.Failure("Pièce introuvable.", ErrorCode.NotFound);

                await SetActivePart_PartAsync(partResult.Value);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("ReverseActivePartByIdAsync failed", "", ex);

                return Result.Failure(
                    "Erreur lors de la modification de l'état actif.",
                    ErrorCode.Unknown);
            }
        }

        #region Wrapper DB
        public async Task<Result<List<Part>>> GetAllPartsAsync()
        {
            try
            {
                var parts = await _databaseService.GetAllPartsAsync();
                return parts == null ? Result<List<Part>>.Failure("Erreur lors du chargement des pièces.", ErrorCode.NotFound) : Result<List<Part>>.Success(parts);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllPartsAsync failed", "", ex);
                return Result<List<Part>>.Failure("Erreur lors du chargement des pièces.", ErrorCode.Unknown);
            }
        }
        public async Task<Result> SetActivePart_PartAsync(Part part)
        {
            try
            {
                await _databaseService.SetActivePart_PartAsync(part);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("SetActivePart_PartAsync failed", "", ex);
                return Result.Failure("Erreur lors de la mise à jour de la pièce active.", ErrorCode.Unknown);
            }
        }
        public async Task<Result<Part>> GetPartByIdAsync(int idPart)
        {
            try
            {
                var part = await _databaseService.GetPartByIdAsync(idPart);
                return Result<Part>.Success(part);
            }
            catch (Exception ex)
            {
                _logger.LogError("SetActivePart_GetPartByIdAsyncPartAsync failed", "", ex);
                return Result<Part>.Failure("Erreur lors de la récupération de la pièce.", ErrorCode.Unknown);
            }
        }
        #endregion

        #region Helper Parts

        /// <summary>
        /// Insertion de nouvelle pièce avec nom par défaut.
        /// Sans syncronisation des tables car fonction utiliser en parralele de la création 
        /// des contacts => pas de surcharge de syncronisation
        /// </summary>
        private async Task<Part> InsertPartAsync(string nameNewPart)
        {
            Part newPart = CreateDefaultPart(nameNewPart);
            await InsertPartAsync(newPart);
            _logger.LogInfo($"Création de la Part '{nameNewPart}' - ID :{newPart.Id}", nameof(DatabaseService));
            return newPart;
        }

        private Part CreateDefaultPart(string nameNewPart = "")
        {
            if (nameNewPart == "") nameNewPart = "Nouvelle Pièce";
            return new Part
            {
                NamePart = nameNewPart,
                MasseVol = 0.0,
                Comment = "",
                IsActive = true,
                ImagePart = null
            };
        }


        #endregion

        #endregion

        #region Visualization 

        public async Task<Result<List<ModelData>>> GetAllModelDataAsync()
        {
            try
            {
                var modelDataList = await _databaseService.GetAllModelDataAsync();
                return Result<List<ModelData>>.Success(modelDataList);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllModelDataAsync failed", "", ex);
                return Result<List<ModelData>>.Failure("Erreur lors du chargement des données du modèle.", ErrorCode.Unknown);
            }
        }
        public async Task<Result<List<Requirements>>> GetAllRequirementsAsync()
        {
            try
            {
                var requirements = await _databaseService.GetAllRequirementsAsync();
                return Result<List<Requirements>>.Success(requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllRequirementsAsync failed", "", ex);
                return Result<List<Requirements>>.Failure("Erreur lors du chargement des données du modèle.", ErrorCode.Unknown);
            }
        }

        #endregion

        #region Helper

        /// <summary>
        /// Résout les noms en doublon dans <paramref name="toSave"/> en ajoutant un incrément.
        /// Vérifie à la fois la DB et les doublons internes à la liste.
        /// Ex: "Req" → "Req (1)", "Req (2)"...
        /// </summary>
        /// <param name="toSave">Liste d'entités à résoudre.</param>
        /// <param name="nameExistsAsync">Délégué vérifiant l'existence du nom en DB.</param>
        /// <returns>Noms modifiés — vide si aucun changement.</returns>
        private async Task<Dictionary<string, string>> ResolveUniqueNamesAsync<T>(
            List<T> toSave,
            Func<T, Task<bool>> nameExistsAsync)
            where T : INameResolvable
        {
            var resolvedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var renamedMap = new Dictionary<string, string>(); // baseName → candidate

            foreach (var entity in toSave)
            {
                var baseName = entity.Name;
                var candidate = baseName;
                int increment = 1;

                while (resolvedNames.Contains(candidate) || await nameExistsAsync(entity))
                {
                    candidate = $"{baseName} ({increment++})";
                    entity.Name = candidate; // ← mettre à jour avant le prochain check
                }

                if (candidate != baseName)
                    renamedMap[baseName] = candidate;

                resolvedNames.Add(candidate);
            }

            return renamedMap;
        }

        // Surcharge unitaire
        private async Task<Dictionary<string, string>> ResolveUniqueNamesAsync<T>(
            T toSave,
            Func<T, Task<bool>> nameExistsAsync)
            where T : INameResolvable
            => await ResolveUniqueNamesAsync(new List<T> { toSave }, nameExistsAsync);

        #endregion

    }
}

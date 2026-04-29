using System.IO;
using DocumentFormat.OpenXml.Office2010.Excel;
using Toltech.App.Models;
using Toltech.App.Services.Logging;
using Toltech.App.Services.Notification;
using Toltech.App.ToltechCalculation.Helpers;
using Toltech.App.Utilities;
using Toltech.App.Utilities.Result;

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
        private readonly DbModelService _dbModelService;
        private readonly ComputeValidationService _computeValidationService;
        public DomainService(
            DatabaseService databaseService,
            DbModelService dbModelService,
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

        public async Task<Result> CreatePartAndDatasAsync(string nomPiece)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result<ModelData>.Failure("No active model.", ErrorCode.NoActiveModel);

                if (NameValidationHelper.NamingValidation(nomPiece).IsFailure)
                    return Result.Failure("Nom de pièce invalide.", ErrorCode.Unknown);

                // 1. vérification existence
                if (await _databaseService.IsNamePartExisteAsync(nomPiece))
                    return Result.Failure("Le nom de la pièce existe déjà dans la base.", ErrorCode.Unknown);

                // 2. création part
                int newPartID = await InsertPartAsync(nomPiece);

                await AddDataOfPartExtremiteAsync(newPartID, 6);

                _ = _notificationService.ShowNotifAsync($"Pièce \"{nomPiece}\" ajoutée avec succès !", false);

                // 5. event métier
                await EventsManager.RaisePartSelectedChangedAsync(newPartID);

                await EventsManager.RaiseModelDataAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("CreatePartAndDatas failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la création de la pièce et des données.", ErrorCode.Unknown);
            }
        }

        public async Task<Result<ModelData>> CreateDataAsync(int idPartActif)
        {
            if (!ModelValidationHelper.CheckModelActif(false))
                return Result<ModelData>.Failure("No active model.", ErrorCode.NoActiveModel);

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
                await _databaseService.DeleteRangeAsync(ids);

                // 3. notification batch
                _ = _notificationService.ShowNotifAsync(
                    $"{ids.Count} donnée(s) supprimée(s) avec succès.",
                    false);

                // 4. events métier
                await EventsManager.RaiseModelDataAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

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

        public async Task<Result> DeletePartWithDatasByIdAsync(int idPart)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(false))
                    return Result<ModelData>.Failure("No active model.", ErrorCode.NoActiveModel);

                // Suppression atomique (transaction recommandée côté service)
                await _databaseService.DeletePartsWithDatasRangeAsync(new List<int> { idPart });

                // 3. notification
                _ = _notificationService.ShowNotifAsync(
                    $"Données supprimées pour la pièce {idPart}.",
                    false);

                // 4. event métier
                await EventsManager.RaisePartAddOrDeletedAsync();
                await EventsManager.RaiseModelDataAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("DeletePartById failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la suppression de la pièce.", ErrorCode.Unknown);
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

        public async Task<Result> SaveModelDataAsync(List<ModelData> toSave)
        {
            foreach (var d in toSave)
                d.MarkSaving();

            try
            {
                await _databaseService.UpdateRangeAsync(toSave);

                foreach (var d in toSave)
                {
                    d.ClearDirty();
                    d.ClearSaving();
                }

                await EventsManager.RaiseModelDataAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

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

        public async Task<Result> UpdateFixedPartAsync(Part part)
        {
            try
            {
                if (part == null)
                    return Result.Failure("Part is null", ErrorCode.Unknown);

                await _databaseService.SetFixedPartAsync(part);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateFixedPart failed", "", ex);
                return Result.Failure("Error UpdateFixedPartAsync", ErrorCode.Unknown);
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
            string randomName = $"PO_{Guid.NewGuid().ToString("N")[..6]}";

            for (int i = 0; i < count; i++)
            {
                result.Add(new ModelData
                {
                    CoordX = 0,
                    CoordY = 0,
                    CoordZ = 0,
                    CoordU = 1,
                    CoordV = 0,
                    CoordW = 0,

                    OriginePartId = 0,
                    ExtremitePartId = partId,

                    TolOri = 0,
                    TolInt = 0,
                    TolExtr = 0,

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

                await EventsManager.RaiseRequirementAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

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
                await _databaseService.UpdateRangeAsync(toSave);

                foreach (var req in toSave)
                {
                    req.ClearDirty();
                    req.ClearSaving();
                }

                await EventsManager.RaiseRequirementAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

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


        public Task<Result> RemoveRequirementAsync(Requirements req)
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

                await EventsManager.RaiseRequirementAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

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

        public async Task<Result> ReverseActiveReqByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue)
                    return Result.Failure("ID de l'exigence invalide.", ErrorCode.InvalidInput);

                // 1. récupération métier
                var req = await _databaseService.GetReqsByIdAsync(idReq);

                if (req == null)
                    return Result.Failure("Exigence introuvable.", ErrorCode.NotFound);

                // 2. application règle métier
                req.IsActive = !req.IsActive;

                await _databaseService.UpdateAsync(req);

                await EventsManager.RaiseRequirementAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError("ReverseActiveReqById failed", "", ex);
                return Result.Failure("Une erreur est survenue lors de la modification de l'exigence.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> DeleteRequirementByIdAsync(int? idReq)
        {
            var req = await _databaseService.GetReqsByIdAsync(idReq);

            if (req == null)
                return Result.Failure("Exigence introuvable.", ErrorCode.NotFound);

            return await DeleteRequirementAsync(new[] { req });
        }

        public async Task<Result<string?>> GetRequirementNameByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue || idReq.Value <= 0)
                    return Result<string?>.Failure("ID de l'exigence invalide.", ErrorCode.InvalidInput);

                var name = await _databaseService.GetReqNameByIdAsync(idReq.Value);

                return string.IsNullOrWhiteSpace(name) ? Result<string?>.Failure("Nom de l'exigence introuvable.", ErrorCode.NotFound) : Result<string?>.Success(name);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRequirementNameById failed", "", ex);
                return Result<string?>.Failure("Une erreur est survenue lors de la récupération du nom de l'exigence.", ErrorCode.Unknown);
            }
        }

        #endregion

        #region Service Model

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
                    return Result.Failure("Le modèle existe déjà.");
                }

                // 3. Définir modèle actif
                ModelManager.ModelActif = modelPath;

                // 4. Enregistrement meta DB
                int newModelId = await DbModelService.ActiveInstance.RegisterModelInMetaDb(modelName, modelPath, modelName);

                // 5. Ouverture / création DB modèle
                await _databaseService.Open(modelPath);

                // 6. Initialisation DB modèle
                await _databaseService.UpdateModelIdAsync(
                    newModelId,
                    modelName,
                    modelPath);

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
                await RegisterModelAsync();

                // 4. ouverture DB modèle
                await _databaseService.Open(newFilePath);

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

        public async Task RenameModelFileIfNeededAsync(ModelMeta dbModel, ModelMeta currentModel)
        {
            try
            {
                if (dbModel == null || currentModel == null)
                    return;

                if (string.Equals(
                        dbModel.NameData,
                        currentModel.NameData,
                        StringComparison.OrdinalIgnoreCase))
                    return;

                string oldFilePath = dbModel.FilePathModel;

                if (string.IsNullOrWhiteSpace(oldFilePath) ||
                    !File.Exists(oldFilePath))
                    return;

                string directory = Path.GetDirectoryName(oldFilePath)!;
                string extension = Path.GetExtension(oldFilePath);

                string newFilePath =
                    Path.Combine(directory, currentModel.NameData + extension);

                // 1. opération filesystem
                await Task.Run(() =>
                {
                    File.Move(oldFilePath, newFilePath);
                });

                // 2. update modèle mémoire
                currentModel.FilePathModel = newFilePath;

                // 3. update DB
                await _databaseService.UpdateModelIdAsync(
                    currentModel.IdModel,
                    currentModel.NameData,
                    newFilePath);
            }
            catch (IOException ioEx)
            {
                _logger.LogError("RenameModelFile failed", "", ioEx);

                throw new InvalidOperationException(
                    $"Impossible de renommer le fichier du modèle '{currentModel.NameData}'.",
                    ioEx);
            }
        }

        private async Task DeleteMetaModelAsync(string fullPath)
        {
            try
            {
                var exists = await DbModelService.ActiveInstance.IsExistModelDB(fullPath);
                if (!exists)
                    return;

                await DbModelService.ActiveInstance.DeleteModelFromMetaDb(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteMetaModel failed", "", ex);
            }
        }

        public async Task<Result> RegisterModelAsync(string modelPath = null)
        {
            try
            {
                if (modelPath == null)
                    modelPath = ModelManager.ModelActif;

                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    return Result.Failure("Invalid model path.", ErrorCode.Unknown);
                }

                if ((await IsExistModelRegisterAsync(modelPath)).IsSuccess)
                    return Result.Success();
                // Extraire le nom du modèle depuis le chemin
                string modelName = Path.GetFileNameWithoutExtension(modelPath);

                if (NameValidationHelper.NamingValidation(modelName).IsFailure)
                {
                    return Result.Failure("Invalid model name.", ErrorCode.Unknown);
                }

                // Fermer la connexion base de données si ouverte (en tâche de fond)
                await Task.Run(() => _databaseService.CloseConnection());

                int modelId = await DbModelService.ActiveInstance.RegisterModelInMetaDb(modelName, modelPath, "");

                // Ecraser l'entrée correspondante dans la base de données du modèle
                await _databaseService.UpdateModelIdAsync(modelId, modelName, modelPath);
                return Result.Success();

            }
            catch (Exception ex)
            {
                return Result.Failure("Error during model registration.", ErrorCode.Unknown);
            }
        }

        public async Task<Result> IsExistModelRegisterAsync(String modelPath)
        {
            var IsExist = await DbModelService.ActiveInstance.IsExistModelDB(modelPath);
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

                // 1. récupération état DB existant
                var dbModel = await _dbModelService.GetModelMetaByIdAsync(meta.IdModel);

                if (dbModel == null)
                {
                    meta.ClearSaving();
                    return Result.Failure("Model not found.", ErrorCode.Unknown);
                }

                // 2. cohérence fichier si rename
                await RenameModelFileIfNeededAsync(dbModel, meta);

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
                    .OrderByDescending(m => m.IdModel)
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
        public async Task<int> InsertPartAsync(Part newPart)
        {
            if (newPart == null) newPart = CreateDefaultPart("");
            await _databaseService.InsertAsync(newPart);

            await EventsManager.RaisePartAddOrDeletedAsync();
            await _databaseService.Refactor_SynchronizeNodeGraphAsync();

            return newPart.Id;
        }
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
                await EventsManager.RaisePartAddOrDeletedAsync();
                await EventsManager.RaiseModelDataAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

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

        public async Task<Result> UpdatePartsAsync(IEnumerable<Part> parts)
        {
            var list = parts?.Where(p => p != null).ToList() ?? new List<Part>();
            if (list.Count == 0)
                return Result.Success();

            try
            {
                // --- Validation métier (exemple : unicité des noms) ---
                // 1) doublons dans le lot
                var dupInBatch = list
                    .GroupBy(p => p.NamePart)
                    .FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

                if (dupInBatch != null)
                    return Result.Failure($"Nom en double dans la sélection : '{dupInBatch.Key}'.", ErrorCode.DuplicateEntry);

                // 2) conflits avec la base (en excluant les Id existants)
                foreach (var p in list)
                {
                    bool exists = await _databaseService.IsNamePartExisteAsync(p.NamePart,p.Id);
                    if (exists)
                        return Result.Failure($"Le nom '{p.NamePart}' existe déjà.", ErrorCode.DuplicateEntry);
                }

                // --- Séparation insert / update ---
                var toInsert = list.Where(p => p.Id == 0).ToList();
                var toUpdate = list.Where(p => p.Id != 0).ToList();

                // --- Transaction unique ---
                await _databaseService.SavePartsRangeAsync(toUpdate,toInsert);

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

        public async Task<Result<int>> UpdatePartAsync(Part part)
        {
            if (part == null)
                return Result<int>.Failure("Part null.", ErrorCode.InvalidInput);

            try
            {
                part.MarkSaving();

                bool exists = await _databaseService.IsNamePartExisteAsync(part.NamePart, part.Id);
                if (exists)
                {
                    part.ClearSaving();
                    return Result<int>.Failure($"Le nom '{part.NamePart}' existe déjà.", ErrorCode.DuplicateEntry);
                }

                await _databaseService.UpdateAsync(part);

                part.ClearDirty();
                part.ClearSaving();

                await EventsManager.RaisePartAddOrDeletedAsync();
                await _databaseService.Refactor_SynchronizeNodeGraphAsync();

                return Result<int>.Success(part.Id);
            }
            catch (Exception ex)
            {
                part.ClearSaving(); // très important

                _logger.LogError("UpdatePartAsync failed", "", ex);

                return Result<int>.Failure("Erreur lors de la mise à jour.", ErrorCode.Unknown);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Insertion de nouvelle pièce avec nom par défaut.
        /// Sans syncronisation des tables car fonction utiliser en parralele de la création 
        /// des contacts => pas de surcharge de syncronisation
        /// </summary>
        private async Task<int> InsertPartAsync(string nameNewPart)
        {
            Part newPart = CreateDefaultPart(nameNewPart);
            await InsertPartAsync(newPart);
            _logger.LogInfo($"Création de la Part '{nameNewPart}' - ID :{newPart.Id}", nameof(DatabaseService));
            return newPart.Id;
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

    }
}

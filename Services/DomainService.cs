using System.IO;
using System.Windows;
using TOLTECH_APPLICATION.FrontEnd.Interfaces;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services.Logging;
using TOLTECH_APPLICATION.ToltechCalculation.Helpers;
using TOLTECH_APPLICATION.Utilities;

namespace TOLTECH_APPLICATION.Services
{
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

        public async Task<int?> CreatePartAndDatasAsync(string nomPiece)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(true))
                    return null;

                if (string.IsNullOrWhiteSpace(nomPiece))
                    return null;

                if (!NameValidationHelper.ValiderNomDePiece(nomPiece))
                    return null;

                // 1. vérification existence
                if (await _databaseService.NamePartExisteAsync(nomPiece))
                {
                    //_dialog.Error($"La pièce \"{nomPiece}\" existe déjà dans la base.");
                    return null;
                }

                // 2. création part
                int newPartID = await _databaseService.InsertPartAsync(nomPiece);

                // 3. création datas initiales
                await _databaseService.AddDataOfPartExtremiteAsync(newPartID, 6);

                // 4. notification
                _ = _notificationService.ShowNotifAsync(
                    $"Pièce \"{nomPiece}\" ajoutée avec succès !",
                    false);

                // 5. event métier
                await EventsManager.RaisePartSelectedChangedAsync(newPartID);

                return newPartID;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreatePartAndDatas failed", "", ex);
                return null;
            }
        }

        public async Task<ModelData> CreateDataAsync(int idPartActif)
        {
            if (!ModelValidationHelper.CheckModelActif(true))
                return null;

            try
            {
                var newDatas = await _databaseService.AddDataOfPartExtremiteAsync(idPartActif, 1);

                var firstData = newDatas.FirstOrDefault();

                if (firstData == null)
                    return null;

                var data = new ModelData();
                data.LoadFromDb(firstData);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateData failed", "", ex);
                return null;
            }
        }

        public async Task<bool> DeleteDataAsync(ModelData data)
        {
            try
            {
                if (data == null || data.Id <= 0)
                    return false;

                // 1. récupération en base
                var existing = await _databaseService.GetModelDataByIdAsync(data.Id);

                if (existing == null)
                {
                    //_dialog.Error("Aucune correspondance trouvée en base de données.");
                    return false;
                }

                // 2. suppression DB
                await _databaseService.DeleteModelDataAsync(existing);

                // 3. récupération info complémentaire
                string nameData = await _databaseService.GetPartNameByID(data.Id);

                // 4. notification
                _ = _notificationService.ShowNotifAsync(
                    $"Contact {data.Model} supprimé avec succès.",
                    false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteData failed", "", ex);
                return false;
            }
        }

        public async Task<bool> DeletePartByIdAsync(int idPart)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(true))
                    return false;

                // 1. récupération nom (pour notification + event)
                string partName = await _databaseService.GetPartNameByID(idPart);

                // 2. suppression métier
                await _databaseService.DeletePartAsync(idPart);
                await _databaseService.DeleteDatasOfPartExtremiteAsync(idPart);

                // 3. notification
                _ = _notificationService.ShowNotifAsync(
                    $"Données supprimées pour la pièce {partName}.",
                    false);

                // 4. event métier
                await EventsManager.RaisePartAddOrDeletedAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeletePartById failed", "", ex);
                return false;
            }
        }

        public async Task<string?> GetPartNameByIdAsync(int idPart)
        {
            try
            {
                if (idPart <= 0)
                    return null;

                return await _databaseService.GetPartNameByID(idPart);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPartNameById failed", "", ex);
                return null;
            }
        }

        public async Task SaveModelDataAsync(List<ModelData> toSave)
        {
            foreach (var d in toSave)
                d.MarkSaving();

            try
            {
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    foreach (var d in toSave)
                    {
                        if (d.Id == 0)
                            await _databaseService.InsertModelDataAsync(d);
                        else
                            await _databaseService.UpdateModelDataAsync(d);
                    }
                });

                foreach (var d in toSave)
                {
                    d.ClearDirty();
                    d.ClearSaving();
                }
            }
            catch (Exception ex)
            {
                foreach (var d in toSave.Where(d => d.IsSaving))
                {
                    d.MarkOutOfSync();
                    d.ClearSaving();
                }

                _logger.LogError("SaveModelData failed", "", ex);
                throw;
            }
        }

        public async Task<List<ModelData>> LoadPartDataAsync(int partId)
        {
            return await _databaseService.GetModelDataByPartIdAsync(partId);
        }

        public async Task<IEnumerable<ModelData>> LoadSortedDataAsync(List<ModelData> dataList, int partId)
        {
            return await _databaseService.GetDatasSortedByNodeOrder(dataList, partId);
        }

        public async Task<List<ModelData>> SortDatasAsync(List<ModelData> datas, int partId)
        {
            return (await _databaseService
                .GetDatasSortedByNodeOrder(datas, partId))
                .ToList();
        }

        public async Task<bool?> CheckIsoPartAsync(int? selectedPartId)
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif(true))
                    return null;

                if (!selectedPartId.HasValue)
                    return null;

                // 1. récupération part
                Part partActif = await _databaseService.GetPartByIdAsync(selectedPartId.Value);

                if (partActif == null)
                    return null;

                // 2. validation métier ISO
                bool inverse = await _computeValidationService.ValidationPart(partActif);

                return inverse;
            }
            catch (Exception ex)
            {
                _logger.LogError("CheckIsoPart failed", "", ex);
                return null;
            }
        }

        public async Task UpdateFixedPartAsync(Part part)
        {
            try
            {
                if (part == null)
                    return;

                await _databaseService.SetFixedPartAsync(part);
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateFixedPart failed", "", ex);
            }
        }

        public async Task<Part?> GetFixedPartAsync()
        {
            try
            {
                return await _databaseService.GetFixedPartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFixedPart failed", "", ex);
                return null;
            }
        }

        #endregion

        #region Service Requirements
        public async Task<Requirements?> CreateRequirementAsync()
        {
            string nomRequirement =
                   $"Req_{Guid.NewGuid().ToString("N")[..6]}";
            try
            {
                if (!ModelValidationHelper.CheckModelActif(true))
                    return null;

                if (!NameValidationHelper.ValiderNomDePiece(nomRequirement))
                    return null;

                // 1. vérification doublon
                if (await _databaseService.NameReqExisteAsync(nomRequirement))
                {
                    _ = _notificationService.ShowNotifAsync(
                        $"L'exigence \"{nomRequirement}\" existe déjà.",
                        true);

                    return null;
                }

                // 2. création DB
                var newReq = await _databaseService.AddReqAsync(nomRequirement);

                if (newReq == null)
                    return null;

                // 3. mapping métier → UI model
                var uiModel = new Requirements();
                uiModel.LoadFromDb(newReq);

                // 4. notification
                _ = _notificationService.ShowNotifAsync(
                    $"Exigence \"{nomRequirement}\" ajoutée avec succès !",
                    false);

                return uiModel;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateRequirement failed", "", ex);

                _ = _notificationService.ShowNotifAsync(
                    $"Erreur lors de la création de \"{nomRequirement}\".",
                    true);

                return null;
            }
        }
        public async Task<List<Requirements>> LoadAllRequirementsAsync()
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

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadAllRequirements failed", "", ex);
                return new List<Requirements>();
            }
        }

        public async Task SaveRequirementsAsync(List<Requirements> toSave)
        {
            foreach (var req in toSave)
                req.MarkSaving();

            try
            {
                await _databaseService.RunInTransactionAsync(async () =>
                {
                    foreach (var r in toSave)
                    {
                        if (r.Id_req == 0)
                            await _databaseService.InsertRequirementAsync(r);
                        else
                            await _databaseService.UpdateRequirementsAsync(r);
                    }
                });

                foreach (var req in toSave)
                {
                    req.ClearDirty();
                    req.ClearSaving();
                }
            }
            catch (Exception ex)
            {
                foreach (var req in toSave.Where(r => r.IsSaving))
                    req.ClearSaving();

                _logger.LogError("SaveRequirements failed", "", ex);
                throw;
            }
        }

        public async Task<bool> RemoveRequirementAsync(Requirements req)
        {
            try
            {
                if (req == null)
                    return false;

                // suppression DB uniquement si existe déjà en base
                if (req.Id_req != 0)
                    await _databaseService.DeleteRequirementAsync(req);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Delete requirement failed {req?.NameReq} ({req?.Id_req})",
                    "",
                    ex);

                return false;
            }
        }

        public async Task ReverseActiveReqByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue)
                    return;

                // 1. récupération métier
                var req = await _databaseService.GetReqsByIdAsync(idReq);

                if (req == null)
                    return;

                // 2. application règle métier
                await _databaseService.SetActiveReq_Async(req);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReverseActiveReqById failed", "", ex);
            }
        }

        public async Task<bool> DeleteRequirementByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue)
                    return false;

                // 1. récupération nom (métier)
                string nameReq = await _databaseService.GetReqNameByIdAsync(idReq);

                if (string.IsNullOrWhiteSpace(nameReq))
                    return false;

                // 2. récupération entité
                Requirements req = await _databaseService.GetReqsByIdAsync(idReq);

                if (req == null)
                    return false;

                // 3. suppression DB
                await _databaseService.DeleteRequirementAsync(req);

                // 4. notification
                _ = _notificationService.ShowNotifAsync(
                    $"Exigence \"{nameReq}\" supprimée avec succès !",
                    false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteRequirementById failed", "", ex);
                return false;
            }
        }

        public async Task<string?> GetRequirementNameByIdAsync(int? idReq)
        {
            try
            {
                if (!idReq.HasValue || idReq.Value <= 0)
                    return null;

                var name = await _databaseService.GetReqNameByIdAsync(idReq.Value);

                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetRequirementNameById failed", "", ex);
                return null;
            }
        }

        #endregion

        #region Service Model

        public async Task<bool> CreateModelAsync(string modelName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(modelName))
                    return false;

                if (!NameValidationHelper.ValiderNomDePiece(modelName))
                    return false;

                // 1. Construction du chemin
                string modelPath = Path.Combine(
                    ModelManager.AppDataPath,
                    modelName + ".tolx");

                // 2. Vérification existence
                if (File.Exists(modelPath))
                {
                    return false;
                }

                // 3. Définir modèle actif
                ModelManager.ModelActif = modelPath;

                // 4. Enregistrement meta DB
                int newModelId = await DbModelService.ActiveInstance
                    .RegisterModelInMetaDb(modelName, modelPath, modelName);

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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateModel failed", "", ex);
                return false;
            }
        }

        public async Task<bool> OpenModelAsync(string selectedFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(selectedFile))
                    return false;

                // 1. Notification
                _ = _notificationService.ShowNotifAsync(
                    $"Modèle '{System.IO.Path.GetFileNameWithoutExtension(selectedFile)}' ouvert",
                    false);

                // 2. Mise à jour modèle actif
                ModelManager.ModelActif = selectedFile;

                // 3. Switch DB (IMPORTANT : instance existante)
                await _databaseService.Open(selectedFile);

                // 4. Vérification
                var status = await DbModelService.ActiveInstance.CheckModelExistenceAsync(selectedFile, true);

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError("OpenModel failed", "", ex);
                return false;
            }
        }

        public async Task<bool> DeleteModelAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                // 1. Si modèle actif → fermer la DB
                if (path == ModelManager.ModelActif)
                {
                    await _databaseService.CloseConnection();
                }

                // 2. Suppression avec retry (verrou fichier)
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(path);
                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(100);
                    }
                }

                // 3. Suppression meta
                await DeleteMetaModelAsync(path);

                // 4. Réouvrir DB par défaut (temp/template)
                await _databaseService.Open();

                // 5. Notification
                _ = _notificationService.ShowNotifAsync($"Modèle '{System.IO.Path.GetFileNameWithoutExtension(path)}' supprimé", false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteModel failed", "", ex);
                return false;
            }
        }

        public async Task<bool> DuplicateModelAsync(string sourcePath, string destinationFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationFolder))
                    return false;

                if (!File.Exists(sourcePath))
                    return false;

                string fileName = Path.GetFileName(sourcePath);
                string newFilePath = Path.Combine(destinationFolder, fileName);

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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("DuplicateModel failed", "", ex);
                return false;
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
                // Extraire le nom du modèle
                string modelName = Path.GetFileNameWithoutExtension(fullPath);

                // Vérifier si le modèle existe en base
                var exists = await DbModelService.ActiveInstance.IsExistModelDB(fullPath);
                if (!exists)
                {
                    MessageBox.Show($"Le modèle '{modelName}' n'existe pas dans la base.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Supprimer le modèle
                await DbModelService.ActiveInstance.DeleteModelFromMetaDb(fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression du modèle actif : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<bool> RegisterModelAsync(string modelPath = null)
        {
            try
            {
                if (modelPath == null)
                    modelPath = ModelManager.ModelActif;

                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    MessageBox.Show("Aucun modèle actif valide n'est sélectionné.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (await IsExistModelRegisterAsync(modelPath))
                    return false;
                // Extraire le nom du modèle depuis le chemin
                string modelName = Path.GetFileNameWithoutExtension(modelPath);

                if (!NameValidationHelper.ValiderNomDePiece(modelName))
                {
                    MessageBox.Show("Le nom du modèle actif n'est pas valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Fermer la connexion base de données si ouverte (en tâche de fond)
                await Task.Run(() => _databaseService.CloseConnection());

                int modelId = await DbModelService.ActiveInstance.RegisterModelInMetaDb(modelName, modelPath, "");

                // Ecraser l'entrée correspondante dans la base de données du modèle
                await _databaseService.UpdateModelIdAsync(modelId, modelName, modelPath);
                return true;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement du modèle actif dans la bibliothèque: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async Task<bool> IsExistModelRegisterAsync(String modelPath)
        {
            var IsExist = await DbModelService.ActiveInstance.IsExistModelDB(modelPath);
            return IsExist;
        }

        public async Task SaveModelAsync(ModelMeta meta)
        {
            try
            {
                if (meta == null || !meta.IsDirty || meta.IsSaving)
                    return;

                meta.MarkSaving();

                // 1. récupération état DB existant
                var dbModel = await _dbModelService
                    .GetModelMetaByIdAsync(meta.IdModel);

                if (dbModel == null)
                {
                    meta.ClearSaving();
                    return;
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
            }
            catch (Exception ex)
            {
                meta.MarkOutOfSync();
                meta.ClearSaving();

                _logger.LogError("SaveModel failed", "", ex);
            }
        }

        public async Task<List<ModelMeta>> LoadModelsAsync()
        {
            try
            {
                var modelsFromDb = await _dbModelService.GetAllModelsAsync();

                return modelsFromDb
                    .OrderByDescending(m => m.IdModel)
                    .Select(dbModel =>
                    {
                        var uiModel = new ModelMeta();
                        uiModel.LoadFromDb(dbModel);
                        return uiModel;
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadModels failed", "", ex);
                return new List<ModelMeta>();
            }
        }

        #endregion

        #region Service Divers



        #endregion

    }
}

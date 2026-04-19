using System.Diagnostics;
using System.IO;
using System.Windows;
using DocumentFormat.OpenXml.Drawing;
using SQLite;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services.Logging;
using static TOLTECH_APPLICATION.Models.NodesDefinition;

// DatabaseService
// Description : Ce fichier gère toutes les opérations CRUD (Créer, Lire, Mettre à jour, Supprimer) avec la base de données SQLite.
// Il fournit des méthodes pour initialiser la base de données, insérer des données, récupérer des données, et filtrer des données en fonction de critères spécifiques.

namespace TOLTECH_APPLICATION.Services
{
    // Classe DatabaseService
    public class DatabaseService
    {
        // Instance globale unique
        private static string _modelPath = "template_db.tolx";
        private SQLiteAsyncConnection _db; // Connexion à la base de données SQLite
        private string _dbPath; // Chemin d'accès à la base de données
        public static DatabaseService ActiveInstance { get; private set; }
        private static ILoggerService _logger;
        public DatabaseService(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                string tempPath = ModelManager.GetTolTechTempPath();
                dbPath = System.IO.Path.Combine(tempPath, _modelPath);
            }

            _dbPath = dbPath;
            _logger = App.Logger ?? throw new InvalidOperationException("LoggerService non défini");
         
            ActiveInstance = this;

            if (!string.IsNullOrEmpty(dbPath))
                _db = new SQLiteAsyncConnection(_dbPath);
        }

        public async Task Open(string modelPath = "")
        {
            if (string.IsNullOrEmpty(ModelManager.AppDataPath))
            {
                ModelManager.AppDataPath = ModelManager.AppDataPathDefault();
            }

            Directory.CreateDirectory(ModelManager.AppDataPath);

            bool isTempPath = false;
            if (string.IsNullOrEmpty(modelPath))
            {
                isTempPath = true;
                string tempPath = ModelManager.GetTolTechTempPath();
                modelPath = System.IO.Path.Combine(tempPath, _modelPath);
            }

            // Même DB → rien à faire
            if (_db != null &&
                string.Equals(_dbPath, modelPath, StringComparison.OrdinalIgnoreCase))
            {
                NotifyModelOpen();
                return;
            }

            // Ferme ancienne connexion
            if (_db != null)
            {
                await CloseConnection();
            }

            Debug.WriteLine("DATABASESERVICE SWITCH CONNEXION");

            // 🔥 CHANGEMENT ICI : on NE recrée PAS l'objet
            _dbPath = modelPath;
            _db = new SQLiteAsyncConnection(_dbPath);

            // TODO important : maintenir compatibilité
            ActiveInstance = this;

            if (!isTempPath)
                _logger.LogInfo($"Nouvelle connexion : {modelPath}", nameof(DatabaseService));

            await InitAsync();
            NotifyModelOpen();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task RunInTransactionAsync(Func<Task> action)
        {
            await _db.RunInTransactionAsync(_ =>
            {
                //TODO revoir le principe async
                action().GetAwaiter().GetResult();
            });
        }
        public SQLiteAsyncConnection GetConnection()
        {
            if (_db == null)
                throw new InvalidOperationException("Connexion DB non initialisée.");
            return _db;
        }

        #region await EventsManager
        private async Task NotifyModelDataChanged()
        {
            Debug.WriteLine("[DatabaseService] - NotifyModelDataChanged()");
            await EventsManager.RaiseModelDataAddOrDeletedAsync();
        }
        private async Task NotifyRequirementChanged()
        {
            Debug.WriteLine("[DatabaseService] - NotifyRequirementChanged()");
            await EventsManager.RaiseRequirementAddOrDeletedAsync();
        }
        private async Task NotifyNodeUpdated()
        {
            Debug.WriteLine("[DatabaseService] - NotifyNodeUpdated()");
            await EventsManager.RaiseNodesUpdatedAsync();
        }
        public async Task PublicNotifyNodeUpdated()
        {
            Debug.WriteLine("[DatabaseService] - PublicNotifyNodeUpdated()");
            await NotifyNodeUpdated();
        }

        private async Task NotifyModelOpen()
        {
            Debug.WriteLine("[DatabaseService] - NotifyModelOpen()");
            await EventsManager.RaiseModelOpenAsync();
        }
        private async Task NotifyModelDeleted()
        {
            // TODO enlever car pas dappel au 02 / 04 / 2026
            Debug.WriteLine("[DatabaseService] - NotifyModelOpen()");
            await EventsManager.RaiseModelDeleteAsync();
        }

        private async Task NotifyPartAddDeleted()
        {
            Debug.WriteLine("[DatabaseService] - NotifyPartAddDeleted()");
            await EventsManager.RaisePartAddOrDeletedAsync();
        }
        #endregion

        public async Task InitAsync()
        {
            try
            {
                // Vérifie si la base est valide
                await _db.ExecuteScalarAsync<int>("SELECT 1");

                // Optionnel : créer les tables si elles n'existent pas
                await _db.CreateTableAsync<Requirements>();
                await _db.CreateTableAsync<ModelData>();
                await _db.CreateTableAsync<DBTolerances>();
                await _db.CreateTableAsync<NodesDefinition>();
                await _db.CreateTableAsync<ModelDB>();
                await _db.CreateTableAsync<Part>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation de la base : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task CloseConnection()
        {
            if (_db != null)
            {
                try
                {
                    _db.CloseAsync();

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Echec lors de la fermeture du modèle '{_db}'", nameof(DatabaseService), ex);
                }
            }
        }


        // 
        public static string? PromptForFolderPath()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Sélectionnez un dossier pour déplacer la base de données";
                dialog.UseDescriptionForTitle = true;

                var result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    //ModelManager.AppDataPath = dialog.SelectedPath;
                    return dialog.SelectedPath;
                }
                return null;
            }
        }

        // Fonction pour insérer une tol dans la base de données
        public async Task InsertToleranceAsync(DBTolerances tolerances)
        {
            await _db.InsertAsync(tolerances);
        }


        #region Fonctions Part

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
    
        /// <summary>
        /// Insertion de nouvelle pièce avec nom par défaut
        /// Sans syncronisation des tables car fonction utiliser en parralele de la création 
        /// des contacts => pas de surcharge de syncronosation
        /// </summary>
        /// <param name="nameNewPart"></param>
        /// <returns></returns>
        public async Task<int> InsertPartAsync(string nameNewPart)
        {
            try
            {
                var newPart = CreateDefaultPart(nameNewPart);
                await _db.InsertAsync(newPart);
                //await InternalSyncTablesAsync();
                NotifyPartAddDeleted();
                _logger.LogInfo($"Création de la Part '{nameNewPart}' - ID :{newPart.Id}", nameof(DatabaseService));
                return newPart.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec lors de la création de la Part '{nameNewPart}'", nameof(DatabaseService), ex);
                return 0;
            }
        }

        public async Task<int> InsertPartAsync(Part newPart)
        {
            if (newPart == null) newPart = CreateDefaultPart("");
            await _db.InsertAsync(newPart);
            RequestSyncAsync();
            await NotifyPartAddDeleted();
            return newPart.Id;
        }

        public async Task DeletePartAsync(string partName)
        {
            if (string.IsNullOrWhiteSpace(partName))
                return;

            Part part = await _db.Table<Part>()
                                .FirstOrDefaultAsync(p => p.NamePart == partName);

            if (part == null)
                return;

            await DeletePartAsync(part);
        }

        public async Task DeletePartAsync(int iDPart)
        {
            Part part = await _db.Table<Part>()
                                .FirstOrDefaultAsync(p => p.Id == iDPart);
            await DeletePartAsync(part);
        }

        public async Task DeletePartAsync(Part part)
        {
            try
            {
                await _db.DeleteAsync(part);
                RequestSyncAsync();
                await NotifyPartAddDeleted();
                _logger.LogInfo($"Suppresion de la Part '{part.NamePart}' - ID :{part.Id}", nameof(DatabaseService));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec lors de la suppresion de la Part '{part.NamePart}' - ID :{part.Id}", nameof(DatabaseService), ex);
            }
        }

        public async Task<List<Part>> GetAllPartsAsync()
        {
            try
            {
                return await _db.Table<Part>()
                                .OrderBy(p => p.NamePart)
                                .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] Erreur GetAllPartsAsync : {ex.Message}");
                return new List<Part>();
            }
        }

        public async Task<int> GetPartsCountAsync()
        {
            try
            {
                return await _db.Table<Part>().CountAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] Erreur GetPartsCountAsync : {ex.Message}");
                return 0;
            }
        }

        public async Task UpdatePartAsync(Part part)
        {
            if (part == null)
                throw new ArgumentNullException(nameof(part));

            try
            {
                await _db.UpdateAsync(part);
                _logger.LogInfo($"Sauvegarde de la Part '{part.NamePart}' - ID :{part.Id}", nameof(DatabaseService));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec lors de la sauvegarde de la Part '{part.NamePart}' - ID :{part.Id}", nameof(DatabaseService));
                throw;
            }

        }

        // Check si une pièce du meme nom est deja dans la DB 
        public async Task<bool> NamePartExisteAsync(string nomPiece)
        {
            try
            {
                var count = await _db.Table<Part>()
                                     .Where(p => p.NamePart == nomPiece)
                                     .CountAsync();

                return count > 1;
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la vérification de l'existence de la pièce : " + ex.Message);
            }
        }

        // Rename part 
        public async Task UpdatePartNameAsync(int partId, string nouveauNom)
        {
            if (partId <= 0)
                throw new ArgumentException("Identifiant de part invalide.", nameof(partId));

            if (string.IsNullOrWhiteSpace(nouveauNom))
                throw new ArgumentException("Le nouveau nom de la part ne peut pas être vide.", nameof(nouveauNom));

            // Récupération de la part
            var part = await _db.Table<Part>()
                                .Where(p => p.Id == partId)
                                .FirstOrDefaultAsync();

            if (part == null)
                throw new InvalidOperationException($"Aucune part trouvée pour l'id {partId}");

            // Mise à jour du nom
            part.NamePart = nouveauNom;

            await UpdatePartAsync(part);

            await NotifyPartAddDeleted();
            await NotifyModelDataChanged();
            RequestSyncAsync();
        }

        public async Task<Part?> GetPartByIdAsync(int partId)
        {
            if (partId <= 0)
                return null;

            var part = await _db
                .Table<Part>()
                .Where(p => p.Id == partId)
                .FirstOrDefaultAsync();

            return part;
        }

        public async Task<String> GetPartNameByID(int partId)
        {
            var part = await DatabaseService.ActiveInstance.GetPartByIdAsync(partId);

            if (part == null)
                return "";
            string namePart = part.NamePart;
            return namePart;
        }

        public async Task SetFixedPartAsync(Part part)
        {
            if (part == null) return;
            await SetFixedPart_PartAsync(part);
            await SetFixedPart_NodeDefinitionAsync(part);
        }
        private async Task SetFixedPart_NodeDefinitionAsync(Part part)
        {
            // Mettre à jour tous les NodeDefinition liés à cette pièce et de type 4
            var nodesToFix = await _db.Table<NodesDefinition>()
                                      .Where(n => n.LinkedOriginalId == part.Id && n.Type == NodeType.PartNode)
                                      .ToListAsync();

            foreach (var node in nodesToFix)
            {
                node.IsFixed = true;
            }
            await _db.UpdateAllAsync(nodesToFix);

            // Désactiver les autres NodeDefinition de type 4
            var otherNodes = await _db.Table<NodesDefinition>()
                                      .Where(n => n.Type == NodeType.PartNode && n.LinkedOriginalId != part.Id)
                                      .ToListAsync();

            foreach (var node in otherNodes)
            {
                node.IsFixed = false;
            }
            await UpdateAllNodesAsync(otherNodes);

            await NotifyNodeUpdated();
        }
        private async Task SetFixedPart_PartAsync(Part part)
        {
            if (part == null) return;

            var dbPart = await _db.Table<Part>().Where(p => p.Id == part.Id).FirstOrDefaultAsync();
            if (dbPart != null)
            {
                dbPart.IsFixed = true;
                await _db.UpdateAsync(dbPart);

                var otherParts = await _db.Table<Part>().Where(p => p.Id != part.Id).ToListAsync();
                foreach (var p in otherParts)
                {
                    p.IsFixed = false;
                }
                await _db.UpdateAllAsync(otherParts);
            }
        }
        public async Task<Part> GetFixedPartAsync()
        {
            try
            {
                // Récupère la première part dont IsFixed est true
                return await _db.Table<Part>()
                                    .Where(p => p.IsFixed)
                                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] Erreur GetFirstFidedPartIdAsync : {ex.Message}");
                return null;
            }
        }

        public async Task SetActivePart_PartAsync(Part part)
        {
            if (part == null) return;

            var dbPart = await _db.Table<Part>().Where(p => p.Id == part.Id).FirstOrDefaultAsync();
            if (dbPart != null)
            {
                dbPart.IsActive = !dbPart.IsActive;
                await _db.UpdateAsync(dbPart);
            }

            _logger.LogInfo($"Changement de la pièce fixe en '{part.NamePart}' - ID :{part.Id}", nameof(DatabaseService));

            RequestSyncAsync();
        }

        #endregion

        #region Fonctions ModelData

        // Fonction pour insérer des données de modèle dans la base de données
        public async Task InsertModelDataAsync(ModelData modelData)
        {
            await _db.InsertAsync(modelData);
            _logger.LogInfo($"Création de données pour le contact '{modelData.Model}' - ID :{modelData.Id}", nameof(DatabaseService));

            //await RequestSync();
            //NotifyModelDataChanged();
        }

        // Fonction pour récupérer toutes les données de modèle de la base de données
        // TBD revoir si besoin de prendre toutes les données 
        public async Task<List<ModelData>> GetAllModelDataAsync()
        {
            Debug.WriteLine("[DataBaseService] - GetAllModelDataAsync()");
            await _db.CreateTableAsync<ModelData>(); // Évite l'exception si la table n'est pas encore créée
            return await _db.Table<ModelData>().ToListAsync();
        }

        public async Task<List<ModelData>> GetModelDataByPartIdAsync(int partId)
        {
            Debug.WriteLine("[DataBaseService] - GetModelDataByPartIdAsync()");

            await _db.CreateTableAsync<ModelData>();

            return await _db.Table<ModelData>()
                .Where(d => d.ExtremitePartId == partId)
                .ToListAsync();
        }

        // Fonction pour mettre à jour des données de modèle dans la base de données
        public async Task UpdateModelDataAsync(ModelData modelData)
        {
            try
            {
                await _db.UpdateAsync(modelData);
                _logger.LogInfo($"Sauvegarde des données pour le contact '{modelData.Model}' - ID :{modelData.Id}", nameof(DatabaseService));

                RequestSyncAsync();
                await NotifyModelDataChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec de la sauvegarde des données pour le contact '{modelData.Model}' - ID :{modelData.Id}", nameof(DatabaseService));
                throw;
            }
        }

        // Récuperation de data par ID
        public async Task<ModelData> GetModelDataByIdAsync(int id)
        {
            return await _db.FindAsync<ModelData>(id);
        }

        // Fonction pour supprimer des données de modèle de la base de données
        public async Task DeleteModelDataAsync(ModelData modelData)
        {
            try
            {
                await _db.DeleteAsync(modelData);
                _logger.LogInfo($"Suppression des données pour le contact '{modelData.Model}' - ID :{modelData.Id}", nameof(DatabaseService));

                RequestSyncAsync();
                await NotifyModelDataChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec de la suppression des données pour le contact '{modelData.Model}' - ID :{modelData.Id}", nameof(DatabaseService));
                throw;
            }
        }



        // Code pour gerer la création de la nouvelle Pièce du modèle => METTRE DANS LA DB SERVICE ??
        public async Task<List<ModelData>> AddDataOfPartExtremiteAsync(int partId, int count)
        {
            if (partId <= 0 || count <= 0)
                return new List<ModelData>();

            var newDatas = CreateModelDatas(partId, count);
            await InsertModelDataRangeAsync(newDatas);

            if (count <= 5 && newDatas?.FirstOrDefault() is { } first)
            {
                _logger.LogInfo($"Ajout du contact - ID :{first.Id}", nameof(DatabaseService));
            }

            RequestSyncAsync();
            await NotifyModelDataChanged();

            return newDatas ?? new List<ModelData>();
        }

        private static List<ModelData> CreateModelDatas(int partId, int count)
        {
            var result = new List<ModelData>(count);
            string randomName = $"PO_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            for (int i = 1; i <= count; i++)
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
        public async Task InsertModelDataRangeAsync(IEnumerable<ModelData> datas)
        {
            if (datas == null)
                return;

            await _db.RunInTransactionAsync(tran =>
            {
                foreach (var data in datas)
                    tran.Insert(data);
            });
        }


        /// <summary>
        /// Supprime toutes les lignes de ModelData où Extremite correspond à l'ID de la pièce.
        /// </summary>
        /// <param name="iDPart">ID de la pièce à supprimer</param>
        public async Task DeleteDatasOfPartExtremiteAsync(int iDPart)
        {
            // Vérifie que l'ID est valide
            if (iDPart <= 0)
                throw new ArgumentException("L'ID de la pièce doit être supérieur à 0.", nameof(iDPart));

            // Récupère les lignes correspondant à cet ID
            var modelDataToDelete = await _db.Table<ModelData>()
                .Where(p => p.ExtremitePartId == iDPart)
                .ToListAsync();

            if (modelDataToDelete.Count == 0)
            {
                _logger.LogError($"Aucune donnée trouvé avec l'ID {iDPart} pour la suppression", nameof(DatabaseService));
                return;
            }

            // Supprime les lignes trouvées
            foreach (var piece in modelDataToDelete)
            {
                await _db.DeleteAsync(piece);
            }
            RequestSyncAsync();
            await NotifyModelDataChanged();
        }

        public async Task<string> GetExtremiteByIdAsync(int? id)
        {
            // Cherche l'élément correspondant à l'ID
            var model = await _db.Table<ModelData>()
                                 .Where(m => m.Id == id)
                                 .FirstOrDefaultAsync();

            // Si trouvé, retourne l'Extremite, sinon null
            return model?.Extremite;
        }

        #endregion

        #region Fonctions Table Requirements

        // Fonction pour insérer une exigence dans la base de données
        public async Task InsertRequirementAsync(Requirements requirement)
        {
            try
            {
                await _db.InsertAsync(requirement);
                _logger.LogInfo($"Création de l'exigence '{requirement.NameReq}'", nameof(DatabaseService));
                RequestSyncAsync();
                await NotifyRequirementChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec lors de la création de l'exigence {requirement.NameReq}", nameof(DatabaseService), ex);
            }

        }

        // Fonction pour mettre à jour des données de modèle dans la base de données
        public async Task UpdateRequirementsAsync(Requirements requirement)
        {
            try
            {
                await _db.UpdateAsync(requirement);
                _logger.LogInfo($"Sauvegarde de l'exigence '{requirement.NameReq}'", nameof(DatabaseService));
                RequestSyncAsync();
                await NotifyRequirementChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec lors de la sauvegarde de l'exigence '{requirement.NameReq}'", nameof(DatabaseService), ex);
            }
        }

        // Fonction pour supprimer des données de modèle de la base de données
        public async Task DeleteRequirementAsync(Requirements requirement)
        {
            try
            {
                await _db.DeleteAsync(requirement);
                _logger.LogInfo($"Suppression de l'exigence '{requirement.NameReq}'", nameof(DatabaseService));
                RequestSyncAsync();
                await NotifyRequirementChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Echec lors de la suppression de l'exigence '{requirement.NameReq}' - ({requirement.Id_req})", nameof(DatabaseService), ex);
            }
        }

        // Check si une exigence du meme nom est deja dans la DB
        public async Task<bool> NameReqExisteAsync(string nomRequirement)
        {

            try
            {
                return await _db.Table<Requirements>()
                               .Where(r => r.NameReq == nomRequirement)
                               .CountAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la vérification de l'existence de la pièce : " + ex.Message);
            }
        }
        // Fonction pour récupérer toutes les exigences de la base de données
        public async Task<List<Requirements>> GetAllRequirementsAsync()
        {
            return await _db.Table<Requirements>().ToListAsync();
        }

        // Récuperation de Reqs par ID
        public async Task<Requirements> GetReqsByIdAsync(int? id)
        {
            return await _db.FindAsync<Requirements>(id);
        }
        // Récuperation de ReqsName par ID
        public async Task<string> GetReqNameByIdAsync(int? id)
        {
            Requirements req = await _db.FindAsync<Requirements>(id);
            return req?.NameReq;
        }

        // nombre de Requirements totale
        public async Task<int> GetNumberReqAsync()
        {
            try
            {
                return await _db.Table<Requirements>().CountAsync();
            }
            catch (SQLiteException ex) when (ex.Message.Contains("no such table"))
            {
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<Requirements> AddReqAsync(string nomRequirement)
        {
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

            await InsertRequirementAsync(newRequirement);
            return newRequirement;
        }

        public async Task UpdateRequirementNameAsync(int? idReq, string newName)
        {
            var requirement = await _db.Table<Requirements>()
                           .Where(r => r.Id_req == idReq)
                           .FirstOrDefaultAsync();

            if (requirement == null)
                throw new InvalidOperationException($"Aucune exigence trouvée avec Id = {idReq}");

            // Met à jour le nom
            requirement.NameReq = newName;

            // Sauvegarde dans la DB
            await UpdateRequirementsAsync(requirement);

        }

        public async Task SetActiveReq_Async(Requirements req)
        {
            if (req == null) return;

            var dbReq = await _db.Table<Requirements>().Where(p => p.Id_req == req.Id_req).FirstOrDefaultAsync();
            if (dbReq != null)
            {
                dbReq.IsActive = !dbReq.IsActive;
                await _db.UpdateAsync(dbReq);
            }

            RequestSyncAsync();
        }
        #endregion

        #region Fonction DBTolerances

        // Fonction pour mettre à jour les données Tolérances
        public async Task UpdateToleranceAsync(DBTolerances tolerances)
        {
            await _db.UpdateAsync(tolerances);
        }

        // Fonction pour supprimer les données Tolérances
        public async Task DeleteToleranceAsync(DBTolerances tolerances)
        {
            await _db.DeleteAsync(tolerances);
        }

        //   Fonction pour récupérer toutes les données de DB Tolérances
        public async Task<List<DBTolerances>> GetTolerancesAsync()
        {
            return await _db.Table<DBTolerances>().ToListAsync();
        }

        // Récuperation de tolerance par ID
        public async Task<DBTolerances> GetTolerancesByIdAsync(int id)
        {
            Debug.WriteLine("tet");
            return await _db.FindAsync<DBTolerances>(id);
        }

        #endregion

        #region NodesDefinition

        #region Syncronisation NodesDefinition
        /// <summary>
        /// Synchronise la table des nœuds <see cref="NodesDefinition"/> avec les données existantes 
        /// dans les tables <see cref="Requirements"/> et <see cref="ModelData"/>.
        /// </summary>
        /// <remarks>
        /// Cette méthode effectue plusieurs opérations pour maintenir la cohérence entre la base de données
        /// et l'arborescence des nœuds :
        /// <list type="bullet">
        ///     <item>Récupère tous les nœuds existants, requirements et modèles.</item>
        ///     <item>Filtre les nœuds pertinents : <see cref="NodeType.PartNode"/> et <see cref="NodeType.RequirementNode"/>.</item>
        ///     <item>Compare les nœuds existants avec les données sources en utilisant <c>LinkedOriginalId</c> comme référence unique.</item>
        ///     <item>Mets à jour les nœuds dont les propriétés ont changé (par exemple <c>NodeName</c>).</item>
        ///     <item>Ajoute les nœuds manquants pour les requirements et les parts.</item>
        ///     <item>Supprime les nœuds orphelins ne correspondant plus à aucune entrée des tables sources.</item>
        ///     <item>Assure l’unicité des <see cref="NodeType.PartNode"/> par <c>Extremite</c> pour éviter les doublons.</item>
        /// </list>
        /// </remarks>
        /// 
        public async Task SyncNodesTableAsync()
        {
            // --- 1. Récupération des données ---
            var allRequirements = await GetAllRequirementsAsync();
            var allModelData = await GetAllModelDataAsync();
            var allPart = await GetAllPartsAsync();
            var allNodes = await GetAllNodesAsync();

            // --- 2. Filtrer les nœuds existants par type ---
            var existingRequirementNodes = allNodes
                .Where(n => n.Type == NodeType.RequirementNode)
                .ToList();

            var existingPartNodes = allNodes
                .Where(n => n.Type == NodeType.PartNode)
                .ToList();

            // --- 3. Préparer les dictionnaires pour la synchronisation ---
            var requirementsById = allRequirements
                .Where(r => r.Id_req > 0)
                .ToDictionary(r => r.Id_req, r => r);

            // Part name list
            var uniqueModelsByExtremite = allPart
                .Where(m => m.Id > 0)
                .ToDictionary(m => m.Id, m => m);

            var partsById = allPart
                .Where(p => p.Id > 0)
                .ToDictionary(p => p.Id);

            var existingDataNodes = allNodes
                .Where(n => n.Type == NodeType.DataNode)
                .ToList();


            await SyncRequirementNodes(existingRequirementNodes, requirementsById, allNodes);
            await SyncPartNodes(existingPartNodes, uniqueModelsByExtremite, allNodes, allModelData);
            await SyncDataNodes(existingDataNodes, partsById, allModelData, allNodes);
        }

        // --- Méthode dédiée à la synchronisation des RequirementNodes ---
        private async Task SyncRequirementNodes(
          List<NodesDefinition> existingRequirementNodes,
          Dictionary<int, Requirements> requirementsById,
          List<NodesDefinition> allNodes)
        {
            // Indexer les nodes existants par LinkedRequirementId
            var existingNodesByRequirementId = existingRequirementNodes
                .Where(n => n.LinkedRequirementId.HasValue && n.LinkedRequirementId.Value > 0)
                .ToDictionary(n => n.LinkedRequirementId!.Value);

            // Synchroniser les nœuds existants
            foreach (var kvp in existingNodesByRequirementId)
            {
                int requirementId = kvp.Key;
                var node = kvp.Value;

                if (requirementsById.TryGetValue(requirementId, out var requirement))
                {
                    var newName = requirement.NameReq.Trim();
                    if (node.NodeName != newName || node.IsActive != requirement.IsActive)
                    {
                        node.NodeName = newName;
                        node.IsActive = requirement.IsActive;
                        await _db.UpdateAsync(node);
                        NodesDefinition.RaiseNodeChanged(NodeChangeType.Updated, node);
                    }

                    // Marqué comme traité
                    requirementsById.Remove(requirementId);
                }
                else
                {
                    // Requirement supprimé → node orphelin
                    await DeleteNodeAsync(node);
                    NodesDefinition.RaiseNodeChanged(NodeChangeType.Deleted, node);
                }
            }

            // Ajouter les RequirementNode manquants
            foreach (var requirement in requirementsById.Values)
            {
                var newNode = new NodesDefinition
                {
                    NodeName = requirement.NameReq.Trim(),
                    Type = NodeType.RequirementNode,
                    LinkedOriginalId = 0,
                    LinkedRequirementId = requirement.Id_req,
                    ParentId = 0, // TBD
                    DisplayOrder = allNodes.Count(n => n.ParentId == null)
                };

                await _db.InsertAsync(newNode);
                NodesDefinition.RaiseNodeChanged(NodeChangeType.Added, newNode);
                allNodes.Add(newNode);
            }
        }


        // --- Méthode dédiée à la synchronisation des PartNodes ---
        private async Task SyncPartNodes(
            List<NodesDefinition> existingPartNodes,
            Dictionary<int, Part> listPart,
            List<NodesDefinition> allNodes,
            List<ModelData> modeldatas)
        {
            // Créer un dictionnaire des nœuds existants par LinkedOriginalId
            var existingPartNodesById = existingPartNodes
                .Where(n => n.LinkedOriginalId > 0)
                .ToDictionary(n => n.LinkedOriginalId, n => n);

            // Synchroniser les nœuds existants
            foreach (var kvp in listPart)
            {
                int partId = kvp.Key;
                var part = kvp.Value;

                if (existingPartNodesById.TryGetValue(partId, out var existingNode))
                {
                    if (existingNode.NodeName != part.NamePart.Trim() || existingNode.IsActive != part.IsActive)
                    {
                        existingNode.NodeName = part.NamePart.Trim();
                        existingNode.IsActive = part.IsActive;
                        await _db.UpdateAsync(existingNode);
                        NodesDefinition.RaiseNodeChanged(NodeChangeType.Updated, existingNode);
                    }
                    existingPartNodesById.Remove(partId);
                }
                else
                {
                    // Ajouter un nouveau nœud
                    var newNode = new NodesDefinition
                    {
                        NodeName = part.NamePart.Trim(),
                        Type = NodeType.PartNode,
                        LinkedOriginalId = part.Id,
                        LinkedRequirementId = null,
                        ParentId = 0, //TBD
                        IsActive = part.IsActive,
                        DisplayOrder = allNodes.Count(n => n.ParentId == null)
                    };
                    await _db.InsertAsync(newNode);

                    NodesDefinition.RaiseNodeChanged(NodeChangeType.Added, newNode);
                    allNodes.Add(newNode);
                }
            }

            // Supprimer les nœuds orphelins
            foreach (var orphanNode in existingPartNodesById.Values)
            {
                await DeleteNodeAsync(orphanNode);
                NodesDefinition.RaiseNodeChanged(NodeChangeType.Deleted, orphanNode);
            }
        }

        private async Task SyncDataNodes(
            List<NodesDefinition> existingDataNodes,
            Dictionary<int, Part> partsById,
            List<ModelData> allModelData,
            List<NodesDefinition> allNodes)
        {
            var partNodesByPartId = allNodes
                .Where(n => n.Type == NodeType.PartNode && n.LinkedOriginalId > 0)
                .ToDictionary(n => n.LinkedOriginalId, n => n.Id);

            var existingDataByModelId = existingDataNodes
                .Where(n => n.LinkedOriginalId > 0)
                .ToDictionary(n => n.LinkedOriginalId);

            foreach (var data in allModelData)
            {
                if (!data.ExtremitePartId.HasValue || !partNodesByPartId.TryGetValue(data.ExtremitePartId.Value, out var parentNodeId))
                    continue;

                if (existingDataByModelId.TryGetValue(data.Id, out var dataNode))
                {
                    // Mise à jour éventuelle
                    if (dataNode.NodeName != data.Model || dataNode.ParentId != parentNodeId || dataNode.IsActive != data.Active)
                    {
                        dataNode.NodeName = data.Model;
                        dataNode.ParentId = parentNodeId;
                        dataNode.IsActive = data.Active;
                        await _db.UpdateAsync(dataNode);
                        NodesDefinition.RaiseNodeChanged(NodeChangeType.Updated, dataNode);
                    }
                    existingDataByModelId.Remove(data.Id);
                }
                else
                {
                    var newDataNode = new NodesDefinition
                    {
                        NodeName = data.Model,
                        Type = NodeType.DataNode,
                        LinkedOriginalId = data.Id,
                        ParentId = parentNodeId,
                        IsActive = data.Active,
                        DisplayOrder = allNodes
                                        .Where(n => n.ParentId == parentNodeId)
                                        .Select(n => n.DisplayOrder)
                                        .DefaultIfEmpty(0)
                                        .Max() + 1

                    };

                    await _db.InsertAsync(newDataNode);
                    NodesDefinition.RaiseNodeChanged(NodeChangeType.Added, newDataNode);
                    allNodes.Add(newDataNode);
                }
            }

            // Supprimer les DataNodes orphelins
            foreach (var orphan in existingDataByModelId.Values)
            {
                await DeleteNodeAsync(orphan);
                NodesDefinition.RaiseNodeChanged(NodeChangeType.Deleted, orphan);
            }
        }


        public Task<List<NodesDefinition>> GetChildrenAsync(int? parentId)
        {
            return _db.Table<NodesDefinition>()
                      .Where(n => n.ParentId == parentId)
                      .OrderBy(n => n.DisplayOrder)
                      .ToListAsync();
        }

        private Task UpdateNodeParent(int nodeId, int? parentId, int? displayOrder = 0)
        {
            return Task.Run(async () =>
            {
                var node = await _db.Table<NodesDefinition>().FirstOrDefaultAsync(n => n.Id == nodeId);
                if (node != null)
                {
                    node.ParentId = parentId;
                    if (displayOrder.HasValue)
                        node.DisplayOrder = displayOrder.Value;

                    await _db.UpdateAsync(node);
                }
            });
        }


        public async Task<List<NodesDefinition>> GetAllNodesAsync()
        {
            await _db.CreateTableAsync<NodesDefinition>();

            // Charger les nœuds triés
            var nodes = await _db.Table<NodesDefinition>()
                                 .OrderBy(p => p.DisplayOrder)
                                 .ToListAsync();

            return nodes ?? new List<NodesDefinition>();
        }

        public async Task UpdateNodeAsync(NodesDefinition node)
        {
            await _db.UpdateAsync(node);
        }
        public async Task UpdateAllNodesAsync(IEnumerable<NodesDefinition> nodes)
        {
            if (nodes == null || !nodes.Any()) return;

            await _db.UpdateAllAsync(nodes);
        }

        #endregion

        public async Task DeleteNodeAsync(NodesDefinition part)
        {
            await _db.DeleteAsync(part);
        }

        public async Task<int> AddNodeAsync(NodesDefinition node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            try
            {
                int result = await _db.InsertAsync(node);
                await NotifyNodeUpdated();

                return node.Id;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB] AddNodeAsync error: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Récupère tous les nœuds enfants d’un dossier donné via son ParentId.
        /// </summary>
        public async Task<List<NodesDefinition>> GetNodesByParentIdAsync(int parentId)
        {
            try
            {
                var connection = await GetConnectionAsync();
                return await connection.Table<NodesDefinition>()
                                       .Where(n => n.ParentId == parentId)
                                       .OrderBy(n => n.DisplayOrder)
                                       .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] Erreur GetNodesByParentIdAsync : {ex.Message}");
                return new List<NodesDefinition>();
            }
        }
        public async Task UpdateNodeExpansionAsync(NodesDefinition node, bool isExpanded)
        {
            try
            {
                if (node != null)
                {
                    node.IsExpanded = isExpanded;
                    await _db.UpdateAsync(node);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] Erreur UpdateNodeExpansionAsync : {ex.Message}");
            }
        }
        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_db == null)
                throw new InvalidOperationException("Connexion DB non initialisée.");
            return _db;
        }

        /// <summary>
        /// 
        /// </summary>

        public async Task<IEnumerable<ModelData>> GetDatasSortedByNodeOrder(
                   IEnumerable<ModelData> allData, int? idData)
        {
            if (allData == null)
                return Enumerable.Empty<ModelData>();

            var allNodes = await GetAllNodesAsync();
            if (allNodes == null)
                return allData.ToList();

            // 1. Node parent de la Part active ou Select
            var parentNode = allNodes.FirstOrDefault(n =>
                n.LinkedOriginalId == idData &&
                n.Type == NodeType.PartNode);

            if (parentNode == null)
                return allData.Reverse().ToList(); // fallback
        

        // 2. Nodes enfants du parent
        var childNodes = allNodes
                .Where(n => n.ParentId == parentNode.Id)
                .ToList();

            // 3. DisplayOrder max (>= 0)
            int maxDisplayOrder = childNodes
                .Where(n => n.DisplayOrder >= 0)
                .Select(n => n.DisplayOrder)
                .DefaultIfEmpty(0)
                .Max();

            // 4. Mapping LinkedOriginalId → DisplayOrder normalisé
            var nodeOrderMap = childNodes
                .Select((n, index) => new
                {
                    n.LinkedOriginalId,
                    Order = n.DisplayOrder >= 0
                        ? n.DisplayOrder
                        : maxDisplayOrder + 1 + index
                })
                .ToDictionary(x => x.LinkedOriginalId, x => x.Order);

            // 5. Tri final : DisplayOrder max en premier
            var sortedData = allData
                .Select((d, index) => new
                {
                    Data = d,
                    Order = nodeOrderMap.TryGetValue(d.Id, out var order)
                        ? order
                        : maxDisplayOrder + 1 + nodeOrderMap.Count + index,
                    Index = index
                })
                .OrderByDescending(x => x.Order) // ← point clé
                .ThenBy(x => x.Index)            // stabilité visuelle
                .Select(x => x.Data)
                .ToList();

            return sortedData;
        }



        public async Task UpdateDisplayOrderForMoveAsync(
            IReadOnlyList<NodesDefinition> movedNodes,
            int? parentFolderId,
            int insertIndex)
        {

            if (movedNodes == null || movedNodes.Count == 0)
                return;

            // Chargement initial des siblings
            var siblings = (await GetChildrenAsync(parentFolderId))
                .OrderBy(s => s.DisplayOrder)
                .ToList();

            foreach (var node in movedNodes)
            {
                node.ParentId = parentFolderId;
                node.DisplayOrder = insertIndex;

                // Décalage incrémental existant
                foreach (var s in siblings
                    .Where(s => s.DisplayOrder <= insertIndex && !movedNodes.Contains(s)))
                {
                    s.DisplayOrder -= 1;
                    await UpdateNodeParent(s.Id, s.ParentId, s.DisplayOrder);
                }

                await UpdateNodeParent(node.Id, node.ParentId, node.DisplayOrder);
                insertIndex++;
            }

            // --- Normalisation finale ---
            await NormalizeDisplayOrderAsync(parentFolderId);

            await NotifyNodeUpdated();
        }
        private async Task NormalizeDisplayOrderAsync(int? parentId)
        {
            var siblings = (await GetChildrenAsync(parentId))
                .OrderBy(s => s.DisplayOrder)
                .ToList();

            int index = 0;

            foreach (var node in siblings)
            {
                if (node.DisplayOrder != index)
                {
                    node.DisplayOrder = index;
                    await UpdateNodeParent(node.Id, node.ParentId, node.DisplayOrder);
                }

                index++;
            }
        }

        #endregion

        #region Lien MetaModel
        private SQLiteAsyncConnection _dbTemp;

        /// <summary>
        /// Met à jour l'identifiant du modèle actif.
        /// Garantit qu'un seul enregistrement existe dans la table ModelDB.
        /// </summary>
        public async Task<bool> UpdateModelIdAsync(int modelId, string modelName, string pathModel)
        {
            try
            {
                _dbTemp = new SQLiteAsyncConnection(pathModel);


                // 1. Récupération des enregistrements existants
                var existingModels = await _dbTemp.Table<ModelDB>().ToListAsync();

                await _dbTemp.DeleteAllAsync<ModelDB>();

                // 3. Création du nouveau modèle
                var newModel = new ModelDB
                {
                    IdModel = modelId,
                    ModelName = modelName,
                    FilePathModel = pathModel,
                    CreatedAtmodel = DateTime.Now
                };

                // 4. Insertion via ORM
                int rows = await _dbTemp.InsertAsync(newModel);

                _dbTemp.CloseAsync();

                _logger.LogDebug($"Enregistrement du modèle '{modelName}' - ID :{modelId}", nameof(DatabaseService));



                return rows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Récupere l'Id du model en cours d'édition
        /// </summary>
        public async Task<int> GetModelIdAsync()
        {
            if (_db == null)
                return 0;

            var result = await _db.Table<ModelDB>().FirstOrDefaultAsync();
            return result?.IdModel ?? 0;
        }


        // Mise à jour des COUNT PART REQUIREMENT 
        private async Task UpdateModelMetaCountsAsync()
        {
            try
            {
                // Récupération de l'ID du modèle
                int? modelId = await GetModelIdAsync();
                if (modelId == 0)
                {
                    return;
                }

                // Récupération du modèle en base
                var existingModel = await DbModelService.ActiveInstance.GetModelMetaByIdAsync(modelId.Value);

                if (existingModel == null)
                {
                    // TO DO
                    MessageBox.Show($"Le modèle '{ModelManager.ModelActif}' n'est pas inclu dans la bibliothèque locale. \n Certaines fonctionnalité en seront affectés.");
                    return;
                }

                // Récupération des comptes
                int numberOfReq = await GetNumberReqAsync();
                int numberOfParts = await GetPartsCountAsync();

                // Mise à jour des champs
                existingModel.RequirementCount = numberOfReq;
                existingModel.PartCount = numberOfParts;

                // Sauvegarde en base
                await DbModelService.ActiveInstance.SaveModelAsync(existingModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour des compteurs : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mise à jour du FILEPATH  
        /// </summary>
        public async Task UpdateModelFilePathAsync(string newFilePath)
        {
            int idModel = await GetModelIdAsync();

            // Récupère le modèle existant par son Id
            var existingModel = await DbModelService.ActiveInstance.GetModelMetaByIdAsync(idModel);
            if (existingModel == null)
                throw new InvalidOperationException($"Le modèle avec Id {idModel} n'existe pas.");

            // Met à jour uniquement le chemin du fichier
            existingModel.FilePathModel = newFilePath;

            // Sauvegarde la modification dans la base
            await DbModelService.ActiveInstance.UpdateModelMetaAsync(existingModel);
        }



        #endregion

        #region Synchronisation des Tables 

        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private bool _syncPending;

        public async Task RequestSyncAsync()
        {
            _syncPending = true;

            // Si le lock est déjà pris, une sync tourne — elle verra _syncPending=true
            // via le while et repassera. Pas besoin d'en lancer une autre.
            if (!await _syncLock.WaitAsync(0))
                return;

            try
            {
                while (_syncPending)
                {
                    _syncPending = false;
                    await InternalSyncTablesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] - RequestSyncAsync() exception : {ex.Message}");
                throw;
            }
            finally
            {
                _syncLock.Release();
            }
        }


        // Appel sync lors de changement de data / req impactant le treeview
        private async Task InternalSyncTablesAsync()
        {
            Debug.WriteLine("[DataBaseService] - InternalSyncTablesAsync()");

            await UpdateModelMetaCountsAsync();
            await SyncNodesTableAsync();
            await NotifyNodeUpdated();
        }
        public async Task PublicSyncTablesAsync()
        {
            Debug.WriteLine("[DataBaseService] - PublicSyncTablesAsync()");
            await SyncNodesTableAsync();
        }


        #endregion
    }
}

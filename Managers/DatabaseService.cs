using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using SQLite;
using Toltech.App.Models;
using Toltech.App.Services.Logging;
using static Toltech.App.Models.NodesDefinition;

// DatabaseService
// Description : Ce fichier gère toutes les opérations CRUD (Créer, Lire, Mettre à jour, Supprimer) avec la base de données SQLite.
// Il fournit des méthodes pour initialiser la base de données, insérer des données, récupérer des données, et filtrer des données en fonction de critères spécifiques.

namespace Toltech.App.Services
{
    /// <summary>
    /// Couche d’infrastructure dédiée à la persistance des données SQLite.
    /// Centralise les opérations d’accès aux données et isole la logique de stockage
    /// du reste de l’application (Domain / UI).
    /// </summary>
    public class DatabaseService 
    {
        // Instance globale unique
        private static string _modelPath = "template_db.tolx";
        private SQLiteAsyncConnection _asyncDb; // Connexion à la base de données SQLite
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
            _logger = App.Logger;

            ActiveInstance = this;

            if (!string.IsNullOrEmpty(dbPath))
                _asyncDb = new SQLiteAsyncConnection(_dbPath);
        }

        /// <summary>
        /// Ouvre une instance Sqlite
        /// </summary>
        public async Task Open(string modelPath="")
        {
            if (string.IsNullOrEmpty(ModelManager.AppDataPath))
                ModelManager.AppDataPath = ModelManager.AppDataPathDefault();

            Directory.CreateDirectory(ModelManager.AppDataPath);

            bool isTempPath = false;
            if (string.IsNullOrEmpty(modelPath))
            {
                isTempPath = true;
                string tempPath = ModelManager.GetTolTechTempPath();
                modelPath = System.IO.Path.Combine(tempPath, _modelPath);
            }


            // Même DB → rien à faire
            if (_asyncDb != null &&
                string.Equals(_dbPath, modelPath, StringComparison.OrdinalIgnoreCase))
            {
                NotifyModelOpen();
                return;
            }

            // Ferme ancienne connexion
            if (_asyncDb != null)
                await CloseConnection();

            Debug.WriteLine("DATABASESERVICE SWITCH CONNECTION");

            _dbPath = modelPath;

            // IMPORTANT : pas de création implicite ici
            _asyncDb = new SQLiteAsyncConnection(_dbPath);

            ActiveInstance = this;

            if (!isTempPath)
                _logger.LogInfo($"Connection opened : {modelPath}", nameof(DatabaseService));

            NotifyModelOpen();
        }
        public async Task CreateDatabaseAsync(string modelPath)
        {
            if (File.Exists(modelPath))
                throw new InvalidOperationException("Database already exists.");

            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

            // Création physique via ouverture temporaire
            var db = new SQLiteAsyncConnection(modelPath);

           await EnsureSchemaAsync(db);

            await db.CloseAsync();
        }

        public async Task InitializeModelAsync(Guid modelId, string name, string path)
        {
            var meta = await _asyncDb.FindAsync<ModelDB>(1);

            if (meta != null)
            {
                meta.IdModel = modelId;
                meta.ModelName = name;
                meta.FilePathModel = path;

                await _asyncDb.UpdateAsync(meta);
                return;
            }

            await _asyncDb.InsertAsync(new ModelDB
            {
                Id = 1,
                IdModel = modelId,
                ModelName = name,
                FilePathModel = path
            });
        }

        public SQLiteAsyncConnection GetConnection()
        {
            if (_asyncDb == null)
                throw new InvalidOperationException("Connexion DB non initialisée.");
            return _asyncDb;
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

        public async Task EnsureSchemaAsync(SQLiteAsyncConnection db)
        {
            await db.ExecuteScalarAsync<int>("SELECT 1");

            await db.CreateTableAsync<Requirements>();
            await db.CreateTableAsync<ModelData>();
            await db.CreateTableAsync<DBTolerances>();
            await db.CreateTableAsync<NodesDefinition>();
            await db.CreateTableAsync<ModelDB>();
            await db.CreateTableAsync<Part>();
        }
      
        public async Task CloseConnection()
        {
            if (_asyncDb != null)
            {
                try
                {
                    _asyncDb.CloseAsync();

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Echec lors de la fermeture du modèle '{_asyncDb}'", nameof(DatabaseService), ex);
                }
            }
        }


        #region CRUD Unitaires
        public async Task InsertAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _asyncDb.InsertAsync(entity);
        }

        public async Task UpdateAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _asyncDb.UpdateAsync(entity);
        }

        public async Task DeleteAsync<T>(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _asyncDb.DeleteAsync(entity);
        }
        #endregion
       
        #region CRUD Bulk
        public async Task InsertRangeAsync<T>(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);
            await _asyncDb.InsertAllAsync(entities);

        }
        public async Task DeleteRangeAsync<T>(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);

            await _asyncDb.RunInTransactionAsync(tran =>
            {
                foreach (var entity in entities)
                {
                    if (entity == null)
                        continue;
                    tran.Delete(entity);
                }
            });
        }
        public async Task UpdateRangeAsync<T>(IEnumerable<T> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);

            await _asyncDb.UpdateAllAsync(entities);
        }

        public async Task RunInTransactionAsync(Func<Task> action)
        {
            if (_asyncDb == null)
                throw new InvalidOperationException("Connexion DB non initialisée.");

            await _asyncDb.RunInTransactionAsync(conn =>
            {
                // On bloque ici le thread SQLite, donc pas de await direct
                action().GetAwaiter().GetResult();
            });
        }

        #endregion

        #region Fonctions Tolerances
        // Fonction pour insérer une tol dans la base de données
        public async Task InsertToleranceAsync(DBTolerances tolerances)
        {
            await _asyncDb.InsertAsync(tolerances);
        }

        #endregion

        #region Fonctions Part

        #region Queries - Lecture des données

        public async Task<List<Part>> GetAllPartsAsync()
        {
            return await _asyncDb.Table<Part>()
                            .OrderBy(p => p.NamePart)
                            .ToListAsync();
        }

        public async Task<int> GetPartsCountAsync()
        {
            return await _asyncDb.Table<Part>().CountAsync();
        }


        /// <summary>
        /// Vérification si une pièce du meme nom est deja dans la DB 
        /// </summary>
        public async Task<bool> IsNamePartExisteAsync(string namePart, int excludePartId = 0)
        {
            var query = _asyncDb.Table<Part>()
                                .Where(p => p.NamePart == namePart);

            if (excludePartId > 0)
            {
                query = query.Where(p => p.Id != excludePartId);
            }

            return await query.CountAsync() > 0;
        }

        public async Task<Part> GetFixedPartAsync()
        {
            // Récupère la première part dont IsFixed est true
            return await _asyncDb.Table<Part>()
                                .Where(p => p.IsFixed)
                                .FirstOrDefaultAsync();
        }

        public async Task<Part?> GetPartByIdAsync(int partId)
        {
            if (partId <= 0)
                throw new ArgumentException("Invalid id.", nameof(partId));

            var part = await _asyncDb
                .Table<Part>()
                .Where(p => p.Id == partId)
                .FirstOrDefaultAsync();

            return part;
        }

        public async Task<String> GetPartNameByID(int partId)
        {
            if (partId <= 0)
                throw new ArgumentException("Invalid id.", nameof(partId));

            var part = await GetPartByIdAsync(partId);

            return part == null ? throw new ArgumentException("Part not found.", nameof(partId)) : part.NamePart;
        }

        #endregion

        #region Operations - Modification de données

        /// <summary>
        /// Specific batch save function to insert and update parts in a single transaction, to avoid multiple calls to the database and ensure data integrity.
        /// </summary>
        public async Task SavePartsRangeAsync(List<Part> toUpdate , List<Part> toInsert=null)
        {
            await _asyncDb.RunInTransactionAsync(conn =>
            {
                if (toInsert?.Count > 0 || toInsert != null)
                    conn.InsertAll(toInsert);

                if (toUpdate?.Count > 0)
                    conn.UpdateAll(toUpdate);
            });
        }

        /// <summary>
        /// Specific delete function to delete parts with their related model data in cascade
        /// </summary>
        public async Task DeletePartsWithDatasRangeAsync(List<int> partIds)
        {
            await _asyncDb.RunInTransactionAsync(conn =>
            {
                conn.Table<ModelData>()
                    .Delete(d => partIds.Contains(d.ExtremitePartId.Value));

                conn.Table<Part>()
                    .Delete(p => partIds.Contains(p.Id));
            });
        }


        // Rename part 
      


        public async Task SetFixedPartAsync(Part part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));

            await SetFixedPart_PartAsync(part);
            await SetFixedPart_NodeDefinitionAsync(part);
        }
       
        private async Task SetFixedPart_NodeDefinitionAsync(Part part)
        {
            // Mettre à jour tous les NodeDefinition liés à cette pièce et de type 4
            var nodesToFix = await _asyncDb.Table<NodesDefinition>()
                                      .Where(n => n.LinkedOriginalId == part.Id && n.Type == NodeType.PartNode)
                                      .ToListAsync();

            foreach (var node in nodesToFix)
            {
                node.IsFixed = true;
            }
            await _asyncDb.UpdateAllAsync(nodesToFix);

            // Désactiver les autres NodeDefinition de type 4
            var otherNodes = await _asyncDb.Table<NodesDefinition>()
                                      .Where(n => n.Type == NodeType.PartNode && n.LinkedOriginalId != part.Id)
                                      .ToListAsync();

            foreach (var node in otherNodes)
            {
                node.IsFixed = false;
            }
            await UpdateRangeAsync(otherNodes);

            await NotifyNodeUpdated();
        }
        private async Task SetFixedPart_PartAsync(Part part)
        {
            if (part == null) return;

            var dbPart = await _asyncDb.Table<Part>().Where(p => p.Id == part.Id).FirstOrDefaultAsync();
            if (dbPart != null)
            {
                dbPart.IsFixed = true;
                await _asyncDb.UpdateAsync(dbPart);

                var otherParts = await _asyncDb.Table<Part>().Where(p => p.Id != part.Id).ToListAsync();
                foreach (var p in otherParts)
                {
                    p.IsFixed = false;
                }
                await _asyncDb.UpdateAllAsync(otherParts);
            }
        }

        public async Task SetActivePart_PartAsync(Part part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));

            var dbPart = await _asyncDb.Table<Part>().Where(p => p.Id == part.Id).FirstOrDefaultAsync();
            if (dbPart != null)
            {
                dbPart.IsActive = !dbPart.IsActive;
                await _asyncDb.UpdateAsync(dbPart);
            }

            _logger.LogInfo($"Changement de la pièce fixe en '{part.NamePart}' - ID :{part.Id}", nameof(DatabaseService));

            RequestSyncAsync();
        }
        #endregion


        #endregion

        #region ModelData

        #region Queries - Lecture des données

        /// <summary>
        /// Fonction pour récupérer toutes les données de modèle de la base de données
        /// </summary>
        /// <returns></returns>
        // TBD revoir si besoin de prendre toutes les données 
        public async Task<List<ModelData>> GetAllModelDataAsync()
        {
            Debug.WriteLine("[DataBaseService] - GetAllModelDataAsync()");
            await _asyncDb.CreateTableAsync<ModelData>(); // Évite l'exception si la table n'est pas encore créée
            return await _asyncDb.Table<ModelData>().ToListAsync();
        }

        public async Task<List<ModelData>> GetModelDataByPartIdAsync(int partId)
        {
            Debug.WriteLine("[DataBaseService] - GetModelDataByPartIdAsync()");

            if (partId <= 0)
                throw new ArgumentException("Identifiant de part invalide.", nameof(partId));

            await _asyncDb.CreateTableAsync<ModelData>();

            return await _asyncDb.Table<ModelData>()
                .Where(d => d.ExtremitePartId == partId)
                .ToListAsync();
        }

        /// <summary>
        /// Récupération de data par ID
        /// </summary>
        public async Task<ModelData> GetModelDataByIdAsync(int dataId)
        {
            if (dataId <= 0)
                throw new ArgumentException("Identifiant de modèle invalide.", nameof(dataId));

            return await _asyncDb.FindAsync<ModelData>(dataId);
        }

        public async Task<List<ModelData>> GetModelDataByIdsAsync(IEnumerable<int> dataIds)
        {
            var ids = dataIds?.Where(id => id > 0).Distinct().ToList();

            if (ids == null || ids.Count == 0)
                return new List<ModelData>();

            return await _asyncDb.Table<ModelData>()
                .Where(d => ids.Contains(d.Id))
                .ToListAsync();
        }
        public async Task<string> GetExtremiteByIdAsync(int? id)
        {
            // Cherche l'élément correspondant à l'ID
            var model = await _asyncDb.Table<ModelData>()
                                 .Where(m => m.Id == id)
                                 .FirstOrDefaultAsync();

            // Si trouvé, retourne l'Extremite, sinon null
            return model?.Extremite;
        }

        public async Task<List<ModelData>> GetByExtremitePartIdAsync(int partId)
        {
            return await _asyncDb.Table<ModelData>()
                .Where(x => x.ExtremitePartId == partId)
                .ToListAsync();
        }
        #endregion

        #region Operations - Modification des données
        #endregion

        #endregion

        #region Fonctions Table Requirements

        #region Queries

        // Fonction pour récupérer toutes les exigences de la base de données
        public async Task<List<Requirements>> GetAllRequirementsAsync()
        {
            return await _asyncDb.Table<Requirements>().ToListAsync();
        }

        // Récuperation de Reqs par ID
        public async Task<Requirements> GetReqsByIdAsync(int? idReq)
        {
            if (idReq <= 0)
                throw new ArgumentException("Identifiant de requirement invalide.", nameof(idReq));

            return await _asyncDb.FindAsync<Requirements>(idReq);
        }

        // Récuperation de ReqsName par ID
        public async Task<string> GetReqNameByIdAsync(int? id)
        {
            Requirements req = await _asyncDb.FindAsync<Requirements>(id);
            return req?.NameReq;
        }

        // nombre de Requirements totale
        public async Task<int> GetNumberReqAsync()
        {
            return await _asyncDb.Table<Requirements>().CountAsync();
        }
        #endregion

        #region Operation
       
        // Check si une exigence du meme nom est deja dans la DB
        public async Task<bool> NameReqExisteAsync(string nomRequirement)
        {
                return await _asyncDb.Table<Requirements>()
                               .Where(r => r.NameReq == nomRequirement)
                               .CountAsync() > 0;
        }



        #endregion

        #endregion

        #region Fonction DBTolerances

        // Fonction pour mettre à jour les données Tolérances
        public async Task UpdateToleranceAsync(DBTolerances tolerances)
        {
            await _asyncDb.UpdateAsync(tolerances);
        }

        // Fonction pour supprimer les données Tolérances
        public async Task DeleteToleranceAsync(DBTolerances tolerances)
        {
            await _asyncDb.DeleteAsync(tolerances);
        }

        //   Fonction pour récupérer toutes les données de DB Tolérances
        public async Task<List<DBTolerances>> GetTolerancesAsync()
        {
            return await _asyncDb.Table<DBTolerances>().ToListAsync();
        }

        // Récuperation de tolerance par ID
        public async Task<DBTolerances> GetTolerancesByIdAsync(int id)
        {
            Debug.WriteLine("tet");
            return await _asyncDb.FindAsync<DBTolerances>(id);
        }

        #endregion

        #region NodesDefinition

        public async Task<NodesDefinition?> GetNodeByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return null;

                return await _asyncDb.Table<NodesDefinition>()
                    .FirstOrDefaultAsync(n => n.Id == id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] GetNodeByIdAsync error: {ex.Message}");
                return null;
            }
        }

        public Task<List<NodesDefinition>> GetChildrenAsync(int? parentId)
        {
            return _asyncDb.Table<NodesDefinition>()
                      .Where(n => n.ParentId == parentId)
                      .OrderBy(n => n.DisplayOrder)
                      .ToListAsync();
        }


        public async Task<List<NodesDefinition>> GetAllNodesAsync()
        {
            await _asyncDb.CreateTableAsync<NodesDefinition>();

            // Charger les nœuds triés
            var nodes = await _asyncDb.Table<NodesDefinition>()
                                      .OrderBy(n => n.DisplayOrder)
                                      .ToListAsync();

            return nodes ?? new List<NodesDefinition>();
        }

        /// <summary>
        /// Récupère tous les nœuds enfants d’un dossier donné via son ParentId.
        /// </summary>
        public async Task<List<NodesDefinition>> GetNodesByParentIdAsync(int parentId)
        {
            try
            {
                return await _asyncDb.Table<NodesDefinition>()
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



        public async Task NormalizeDisplayOrderAsync(int? parentId)
        {
            var children = await GetChildrenAsync(parentId);
            var ordered = children.OrderBy(c => c.DisplayOrder).ToList();
            var nodesToUpdate = new List<NodesDefinition>();

            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].DisplayOrder != i)
                {
                    ordered[i].DisplayOrder = i;
                    nodesToUpdate.Add(ordered[i]); // collection des nœuds à mettre à jour
                }
            }

            if (nodesToUpdate.Any())
            {
                await UpdateRangeAsync(nodesToUpdate);
            }
        }

        #endregion

        #region Lien MetaModel
        private SQLiteAsyncConnection _dbTemp;


        /// <summary>
        /// Récupere l'Id du model en cours d'édition
        /// </summary>
        public async Task<Guid> GetModelIdAsync()
        {
            if (_asyncDb == null)
                return Guid.Empty;

            var result = await _asyncDb.Table<ModelDB>().FirstOrDefaultAsync();
            return result?.IdModel ?? Guid.Empty;
        }


        // Mise à jour des COUNT PART REQUIREMENT 
        private async Task UpdateModelMetaCountsAsync()
        {
            try
            {
                // Récupération de l'ID du modèle
                Guid? modelId = await GetModelIdAsync();
                if (modelId == Guid.Empty)
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

        #endregion

        #region Synchronisation des Tables 

        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private bool _syncPending;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task RequestSyncAsync()
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

        public async Task Refactor_SynchronizeNodeGraphAsync()
        {
            try
            {
                await RequestSyncAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DatabaseService] - Refactor_SynchronizeNodeGraphAsync() exception : {ex.Message}");
                throw;
            }
        }
       
        // Appel sync lors de changement de data / req impactant le treeview
        private async Task InternalSyncTablesAsync()
        {
            Debug.WriteLine("[DataBaseService] - InternalSyncTablesAsync()");

            await UpdateModelMetaCountsAsync();
            //await _nodeSyncService.SyncNodesTableAsync();
            await NotifyNodeUpdated();
        }

        #endregion
    }
}

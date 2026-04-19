using System.Diagnostics;
using System.IO;
using System.Windows;
using SQLite;
using Toltech.App.Models;
using Toltech.App.Services.Logging;

namespace Toltech.App.Services
{
    public class DbModelService
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbPath;


        private static ILoggerService _logger;
        public static DbModelService ActiveInstance { get; private set; }

        /// <summary>
        /// Constructeur principal : crée ou ouvre la base centrale des modèles.
        /// </summary>
        /// <param name="modelPath">Chemin de la base. Si vide, valeur par défaut.</param>
        public DbModelService(string modelPath = "")
        {
            _logger = App.Logger;

            string baseFolder = ModelManager.GetModelMetaPath();

            // Si une instance existe déjà
            if (ActiveInstance != null && modelPath == "")
            {

                // Même chemin => réutiliser la connexion existante
                if (ActiveInstance._dbPath == baseFolder)
                {
                    _db = ActiveInstance._db;
                    _dbPath = modelPath;
                    ActiveInstance = this;
                    return;
                }
                else
                {
                    Directory.CreateDirectory(baseFolder);
                    _dbPath = Path.Combine(baseFolder, "MetaDatasModels.tolx");
                    ActiveInstance = this;

                }
                Debug.WriteLine("DbModelService NEW CONNEXION");
            }
            else
            {
                if (modelPath!="")
                {
                    Directory.CreateDirectory(modelPath);
                    _dbPath = Path.Combine(modelPath, "MetaDatasModels.tolx");
                        ActiveInstance = this;
                }
                else
                {
                    Directory.CreateDirectory(baseFolder);
                    _dbPath = Path.Combine(baseFolder, "MetaDatasModels.tolx");
                    ActiveInstance = this;

                }

            }
            _db = new SQLiteAsyncConnection(_dbPath);

        }

        // Fonction création des lignes de MetaDonnées
        // Appeler à la création d'un modèle
        public async Task<int> RegisterModelInMetaDb(string modelName, string filePath, string description = "")
        {
            await InitAsync();

            var modelMeta = new ModelMeta
            {
                //
                NameData = modelName,
                DescriptionModel = description,
                FilePathModel = filePath,
                CreatedAtmodel = DateTime.Now
            };

            // Insérer le nouveau modèle dans la base MetaModel
            await _db.InsertAsync(modelMeta);

            // Après insertion, modelMeta.IdModel est mis à jour avec l'Id auto-incrémenté
            return modelMeta.IdModel;
        }

        public async Task DeleteModelFromMetaDb(string filePath)
        {
            await InitAsync();

            // Chercher le modèle correspondant au c await notif.hemin du fichier
            var modelMeta = await _db.Table<ModelMeta>()
                                     .Where(m => m.FilePathModel == filePath)
                                     .FirstOrDefaultAsync();

            if (modelMeta != null)
            {
                // Supprimer l'entrée si trouvée
                await _db.DeleteAsync(modelMeta);
            }
            _logger.LogInfo($"Suppression du modèle '{Path.GetFileNameWithoutExtension(filePath)}'", nameof(DbModelService));

        }


        // Initialise la base : crée la table ModelMeta si elle n'existe pas
        public async Task InitAsync()
        {
            try
            {
                // Vérifie si la base est valide
                await _db.ExecuteScalarAsync<int>("SELECT 1");
                await _db.CreateTableAsync<ModelMeta>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation de la base : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task UpdateModelMetaAsync(ModelMeta modelMeta)
        {
            await _db.UpdateAsync(modelMeta);
        }

        // Récupère tous les modèles enregistrés
        public async Task<List<ModelMeta>> GetAllModelsAsync()
        {
            await InitAsync();
            return await _db.Table<ModelMeta>().ToListAsync();
        }

        public async Task<ModelMeta> GetModelMetaByIdAsync(int idModel)
        {
            return await _db.FindAsync<ModelMeta>(idModel);
        }

        #region Fonctions de MAJ des MetaDonnées 

        // Retourne le nombre total de modèles enregistrés dans la base MetaDonneesModeles.tolx.
        public async Task<int> GetNumberOfModelsAsync()
        {
            await InitAsync(); // S'assure que la table existe
            return await _db.Table<ModelMeta>().CountAsync();
        }


        public async Task SaveModelAsync(ModelMeta model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // Vérifie si le modèle existe déjà en base via l'Id
            var existingModel = await _db.FindAsync<ModelMeta>(model.IdModel);

            if (existingModel != null)
            {
                // Mise à jour du modèle existant
                await _db.UpdateAsync(model);
            }
            else
            {
                // Insertion d'un nouveau modèle
                await _db.InsertAsync(model);
            }
        }

        public async Task DeleteModelByIdAsync(int idModel)
        {
            await InitAsync(); // S'assurer que la table est bien créée
                               // Supprime l'entrée correspondant à l'ID
            await _db.ExecuteAsync("DELETE FROM ModelMeta WHERE IdModel = ?", idModel);
        }

        // Vérifie si une ligne avec ce FilePathModel existe déjà
        public async Task<bool> IsExistModelDB(string pathModel)
        {

            var existing = await _db.Table<ModelMeta>()
                                    .Where(m => m.FilePathModel == pathModel)
                                    .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Ligne existante trouvée avec ce chemin, ne pas insérer
                return true;
            }
            return false;
        }


        public enum ModelExistenceStatus
        {
            Exists,
            Registered,
            NotRegistered
        }

        public async Task<bool> CheckModelExistenceAsync(string modelPath, bool withMsg = true)
        {
            bool exists = await IsExistModelDB(modelPath);

            if (withMsg)
            {
                if (exists)
                {
                    //MessageBox.Show("Ce modèle est déjà présent dans la base de données ModelDB.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    var result = MessageBox.Show(
                        "Ce modèle n'est pas présent dans la base de données ModelDB. Voulez-vous l'inclure pour qu'il soit disponible ?",
                        "Information",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        return true;
                    }
                    return false;
                }
            }

            return exists;
        }

        #endregion

        /// <summary>
        /// Ferme la connexion SQLite.
        /// </summary>
        public void CloseConnection()
        {
            _db = null;
            Debug.WriteLine("DbModelService : connexion fermée");
        }

        #region Image Byte

        public async Task UpdateImageForModelAsync(int idModel, string imagePath)
        {

            await InitAsync();
            // Lecture de l'image sous forme de tableau de bytes
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

            // Date de modification actuelle
            string lastModified = DateTime.UtcNow.ToString("o"); // Format ISO 8601

            // Mise à jour de l'image et de la date pour le modèle correspondant
            await _db.ExecuteAsync(
                "UPDATE ModelMeta SET ImageData = ?, LastModified = ? WHERE IdModel = ?",
                imageBytes, lastModified, idModel);
        }



        #endregion

    }
}

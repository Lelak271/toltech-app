using System.Diagnostics;
using System.IO;
using System.Windows;
using SQLite;
using Toltech.App.Models;
using Toltech.App.Services.Logging;

namespace Toltech.App.Services
{
    public class MetaModelDatabaseService
    {
        private readonly SQLiteAsyncConnection _db;
        private readonly string _dbPath;
        private readonly ILoggerService _logger;
        public static MetaModelDatabaseService ActiveInstance { get; private set; }

        public MetaModelDatabaseService(string modelPath = "")
        {
            _logger = App.Logger;

            // Résolution du chemin
            string baseFolder = string.IsNullOrEmpty(modelPath)
                ? ModelManager.GetModelMetaPath()
                : modelPath;

            Directory.CreateDirectory(baseFolder);
            _dbPath = Path.Combine(baseFolder, "MetaDatasModels.tolx");

            // Réutilise la connexion si même chemin
            if (ActiveInstance?._dbPath == _dbPath)
                _db = ActiveInstance._db;
            else
                _db = new SQLiteAsyncConnection(_dbPath);

            ActiveInstance = this;
        }

        // ── Init ─────────────────────────────────────────────────────────

        public async Task InitAsync()
        {
            await _db.ExecuteScalarAsync<int>("SELECT 1");
            await _db.CreateTableAsync<ModelMeta>();
        }

        // ── CRUD ─────────────────────────────────────────────────────────

        public async Task<Guid> RegisterModelAsync(Guid modelId, string filePath, string description = "")
        {
            await InitAsync();
            var modelMeta = new ModelMeta
            {
                IdModel = modelId,
                NameData = Path.GetFileNameWithoutExtension(filePath),
                DescriptionModel = description,
                FilePathModel = filePath,
                CreatedAtmodel = DateTime.Now
            };
            await _db.InsertAsync(modelMeta);
            return modelMeta.IdModel;
        }

        public async Task SaveModelAsync(ModelMeta model)
        {
            ArgumentNullException.ThrowIfNull(model);
            await InitAsync();
            var existing = await _db.FindAsync<ModelMeta>(model.IdModel);
            if (existing != null)
                await _db.UpdateAsync(model);
            else
                await _db.InsertAsync(model);
        }

        public async Task UpdateModelMetaAsync(ModelMeta modelMeta)
            => await _db.UpdateAsync(modelMeta);

        public async Task DeleteModelFromMetaDbAsync(string filePath)
        {
            await InitAsync();
            var modelMeta = await _db.Table<ModelMeta>()
                .Where(m => m.FilePathModel == filePath)
                .FirstOrDefaultAsync();

            if (modelMeta == null) return;

            await _db.DeleteAsync(modelMeta);
            _logger.LogInfo(
                $"Suppression du modèle '{Path.GetFileNameWithoutExtension(filePath)}'",
                nameof(MetaModelDatabaseService));
        }

        public async Task DeleteModelByIdAsync(int idModel)
        {
            await InitAsync();
            await _db.ExecuteAsync("DELETE FROM ModelMeta WHERE IdModel = ?", idModel);
        }

        // ── Queries ──────────────────────────────────────────────────────

        public async Task<List<ModelMeta>> GetAllModelsAsync()
        {
            await InitAsync();
            return await _db.Table<ModelMeta>().ToListAsync();
        }

        public async Task<ModelMeta> GetModelMetaByIdAsync(Guid idModel)
            => await _db.FindAsync<ModelMeta>(idModel);

        public async Task<int> GetNumberOfModelsAsync()
        {
            await InitAsync();
            return await _db.Table<ModelMeta>().CountAsync();
        }

        public async Task<bool> IsExistModelDBAsync(string pathModel)
        {
            var existing = await _db.Table<ModelMeta>()
                .Where(m => m.FilePathModel == pathModel)
                .FirstOrDefaultAsync();
            return existing != null;
        }

        // ── Image ────────────────────────────────────────────────────────

        public async Task UpdateImageForModelAsync(int idModel, string imagePath)
        {
            await InitAsync();
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var lastModified = DateTime.UtcNow.ToString("o");
            await _db.ExecuteAsync(
                "UPDATE ModelMeta SET ImageData = ?, LastModified = ? WHERE IdModel = ?",
                imageBytes, lastModified, idModel);
        }

        // ── Connexion ────────────────────────────────────────────────────

        public async Task CloseConnectionAsync()
        {
            await _db.CloseAsync();
            Debug.WriteLine("MetaModelDatabaseService : connexion fermée");
        }

        // ── Counts ───────────────────────────────────────────────────────────

        public async Task UpdatePartCountAsync(Guid modelId, int count)
        {
            await InitAsync();
            await _db.ExecuteAsync(
                "UPDATE ModelMeta SET PartCount = ? WHERE IdModel = ?",
                count, modelId);
        }

        public async Task UpdateRequirementCountAsync(Guid modelId, int count)
        {

            await InitAsync();
            await _db.ExecuteAsync(
                "UPDATE ModelMeta SET RequirementCount = ? WHERE IdModel = ?",
                count, modelId);
        }

        public async Task UpdateCountsAsync(Guid modelId, int partCount, int requirementCount)
        {
            await InitAsync();
            await _db.ExecuteAsync(
                "UPDATE ModelMeta SET PartCount = ?, RequirementCount = ? WHERE IdModel = ?",
                partCount, requirementCount, modelId);
        }

    }
}

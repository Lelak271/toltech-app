using System.IO;
using System.Text.Json;

namespace TOLTECH_APPLICATION.Properties
{
    /// <summary>
    /// Settings for UI / UX
    /// Persistance via JSON
    /// </summary>
    public class UiSettings
    {
        /// <summary>
        /// Etat des panels (clé = nom unique, ex: "Req_Eye1")
        /// </summary>
        public Dictionary<string, bool> ExpandedPanels { get; set; } = new();

        /// <summary>
        /// Dernier dossier UI (optionnel)
        /// </summary>
        public string LastFolder { get; set; }

        /// <summary>
        /// Autres états UI possibles (extensible)
        /// </summary>
        public string LastSearchText { get; set; }
    }

    /// <summary>
    /// Service de persistance de settings UiSettings
    /// </summary>
    public class UiSettingsService
    {
        private readonly string _filePath;

        public UiSettings Current { get; private set; } = new();

        public UiSettingsService(string fileName = "ui_settings.json")
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Toltech");

            Directory.CreateDirectory(folder);

            _filePath = Path.Combine(folder, fileName);
        }

        /// <summary>
        /// Chargement des settings
        /// </summary>
        public async Task LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                Current = new UiSettings();
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                Current = JsonSerializer.Deserialize<UiSettings>(json)
                          ?? new UiSettings();
            }
            catch
            {
                // fallback safe
                Current = new UiSettings();
            }
        }

        /// <summary>
        /// Sauvegarde des settings
        /// </summary>
        public async Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_filePath, json);
        }

        /// <summary>
        /// Set état panel
        /// </summary>
        public void SetPanelExpanded(string key, bool isExpanded)
        {
            Current.ExpandedPanels[key] = isExpanded;
        }

        /// <summary>
        /// Get état panel
        /// </summary>
        public bool IsPanelExpanded(string key)
        {
            return Current.ExpandedPanels.TryGetValue(key, out var value)
                ? value
                : true;
        }
    }

}

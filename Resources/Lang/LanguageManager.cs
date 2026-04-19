using System.Globalization;

namespace TOLTECH_APPLICATION.Resources.Lang
{
    public enum SupportedLanguage
    {
        fr,
        en,
        it
    }

    /// <summary>
    /// todo
    /// </summary>
    public static class LanguageHelper
    {
        private static readonly Dictionary<SupportedLanguage, CultureInfo> Cultures =
            new()
            {
                { SupportedLanguage.fr,  new CultureInfo("fr-FR") },
                { SupportedLanguage.en, new CultureInfo("en-US") },
                { SupportedLanguage.it, new CultureInfo("it-IT") }
            };

        /// <summary>
        /// string (culture code) -> SupportedLanguage
        /// </summary>
        public static SupportedLanguage GetSupportedLanguage(string cultureCode)
        {
            if (string.IsNullOrWhiteSpace(cultureCode))
                return LanguageManager.LanguageFallBack;

            cultureCode = cultureCode.ToLowerInvariant();

            foreach (var pair in Cultures)
            {
                if (pair.Value.TwoLetterISOLanguageName == cultureCode ||
                    pair.Value.Name.ToLowerInvariant() == cultureCode)
                {
                    return pair.Key;
                }
            }

            return LanguageManager.LanguageFallBack;
        }

        /// <summary>
        /// SupportedLanguage -> CultureInfo
        /// </summary>
        public static CultureInfo GetCulture(SupportedLanguage lang)
        {
            return Cultures.TryGetValue(lang, out var culture)
                ? culture
                : LanguageManager.LanguageFallBackCulture;
        }
    }

    public static class LanguageManager
    {
        private static SupportedLanguage _currentLanguage = SupportedLanguage.fr;
        public static SupportedLanguage CurrentLanguage => _currentLanguage;
        public static SupportedLanguage LanguageFallBack => SupportedLanguage.fr;
        public static CultureInfo LanguageFallBackCulture => LanguageHelper.GetCulture(LanguageFallBack);
        public static CultureInfo CurrentCulture => LocalizationManager.Instance.CurrentCulture;

        /// <summary>
        /// Applique la langue depuis les paramètres utilisateur
        /// </summary>
        public static void ApplyFromSettings()
        {
            var lang = LanguageHelper.GetSupportedLanguage(Properties.Settings.Default.Language);
            ApplyLanguage(lang);
        }

        /// <summary>
        /// Applique une langue
        /// </summary>
        public static void ApplyLanguage(SupportedLanguage language)
        {
            _currentLanguage = language;

            try
            {
                LocalizationManager.Instance.ChangeCulture(
                    LanguageHelper.GetCulture(language).Name);
            }
            catch
            {
                LocalizationManager.Instance.ChangeCulture(
                    LanguageHelper.GetCulture(LanguageFallBack).Name);
            }
        }
    }
}
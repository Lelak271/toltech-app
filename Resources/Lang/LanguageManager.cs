using System.Globalization;

namespace Toltech.App.Resources.Lang
{
    public enum SupportedLanguage
    {
        fr,
        en,
        it
    }

    /// <summary>
    /// Fournit les utilitaires de correspondance entre <see cref="SupportedLanguage"/>,
    /// <see cref="CultureInfo"/> et codes culture (ex: "fr", "fr-FR").
    /// Stateless — aucun effet de bord.
    /// </summary>
    public static class LanguageHelper
    {
        public const SupportedLanguage DefaultLanguage = SupportedLanguage.fr;
        private static readonly CultureInfo DefaultCulture = new CultureInfo("fr-FR");

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
                return DefaultLanguage;

            cultureCode = cultureCode.ToLowerInvariant();

            foreach (var pair in Cultures)
            {
                if (pair.Value.TwoLetterISOLanguageName == cultureCode ||
                    pair.Value.Name.ToLowerInvariant() == cultureCode)
                {
                    return pair.Key;
                }
            }

            return DefaultLanguage;
        }

        /// <summary>
        /// SupportedLanguage -> CultureInfo
        /// </summary>
        public static CultureInfo GetCulture(SupportedLanguage lang)
        {
            return Cultures.TryGetValue(lang, out var culture)
                ? culture
                : DefaultCulture;
        }
    }

    /// <summary>
    /// Gère l'état de la langue active et son application à <see cref="LocalizationManager"/>.
    /// Point d'entrée unique pour tout changement de langue dans l'application.
    /// </summary>
    public static class LanguageManager
    {
        private static SupportedLanguage _currentLanguage = SupportedLanguage.fr;
        public static SupportedLanguage CurrentLanguage => _currentLanguage;
        public static SupportedLanguage LanguageFallBack => LanguageHelper.DefaultLanguage;
        public static CultureInfo CultureFallBackCulture => LanguageHelper.GetCulture(LanguageFallBack);
        public static CultureInfo CurrentCulture => LocalizationManager.Instance.CurrentCulture;

        /// <summary>
        /// Applique la langue persistée dans <see cref="Properties.Settings.Default.Language"/>.
        /// À appeler au démarrage de l'application.
        /// </summary>
        public static void ApplyFromSettings()
        {
            var lang = LanguageHelper.GetSupportedLanguage(Properties.Settings.Default.Language);
            ApplyLanguage(lang);
        }

        /// <summary>
        /// Applique <paramref name="language"/> comme langue active.
        /// Met à jour <see cref="CurrentLanguage"/> et propage la culture à <see cref="LocalizationManager"/>.
        /// En cas d'échec, bascule sur <see cref="LanguageFallBack"/> et maintient un état cohérent.
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
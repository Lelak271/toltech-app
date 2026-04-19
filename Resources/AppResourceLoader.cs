using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TOLTECH_APPLICATION.Properties;
using TOLTECH_APPLICATION.Resources;
using TOLTECH_APPLICATION.Resources.ColorTheme;
using TOLTECH_APPLICATION.Resources.Lang;

namespace TOLTECH_APPLICATION.Resources
{


    #region Other ressources
    /// <summary>
    /// Unités de longueur
    /// </summary>
    public enum SupportedLengthUnit
    {
        Millimeter,  // mm
        Inch         // in
    }
    /// <summary>
    /// Unités d'angle
    /// </summary>
    public enum SupportedAnglehUnit
    {
        Degre,  // mm
        Radian         // in
    }

    /// <summary>
    /// Unités de masse volumique
    /// </summary>
    public enum SupportedDensityUnit
    {
        KgPerCubicMeter, // kg/m³
        GramPerCm3,      // g/cm³
        PoundPerCubicFoot, // lb/ft³
        PoundPerCubicInch  // lb/in³
    }

    #endregion


    /// <summary>
    /// Propriétés modifiables par l'utilisateur 
    /// </summary>
    public class AppSettingsData
    {
        public SupportedLanguage Language { get; set; } = SupportedLanguage.fr;
        public SupportedLengthUnit LengthUnit { get; set; } = SupportedLengthUnit.Millimeter;
        public SupportedAnglehUnit AngleUnit { get; set; } = SupportedAnglehUnit.Degre;
        public SupportedDensityUnit DensityUnit { get; set; } = SupportedDensityUnit.KgPerCubicMeter;
        public AppTheme Theme { get; set; } = AppTheme.Light;


        // --- Méthodes pour charger depuis les paramètres utilisateur ---
        public void LoadFromUserConfig()
        {
            // Langue
            if (Enum.TryParse(Properties.Settings.Default.Language, out SupportedLanguage lang))
                Language = lang;

            // Unité de longueur
            if (Enum.TryParse(Properties.Settings.Default.DefaultUnit, out SupportedLengthUnit lengthUnit))
                LengthUnit = lengthUnit;

            // Unité de masse volumique
            if (Enum.TryParse(Properties.Settings.Default.DensityUnit, out SupportedDensityUnit densityUnit))
                DensityUnit = densityUnit;

            // Thème
            if (Enum.TryParse(Properties.Settings.Default.Theme, out AppTheme theme))
                Theme = theme;

        }

        // --- Méthodes pour sauvegarder dans les paramètres utilisateur ---
        public void SaveToUserConfig()
        {
            Properties.Settings.Default.Language = Language.ToString();
            Properties.Settings.Default.DefaultUnit = LengthUnit.ToString();
            Properties.Settings.Default.AngleUnit = AngleUnit.ToString();
            Properties.Settings.Default.DensityUnit = DensityUnit.ToString();
            Properties.Settings.Default.Theme = Theme.ToString();

            Properties.Settings.Default.Save();

            // Appliquer les ressources mises à jour (langue, thème…)
            AppResourceLoader.ApplySettings();
        }
    }
   
    /// <summary>
    /// Chargement centraliser des ressources
    /// </summary>
    public static class AppResourceLoader
    {

        /// <summary>
        /// Charge les paramètres utilisateur dans les ressources dynamiques de l'application avec fallback.
        /// </summary>
        private static void LoadSettingsToResources()
        {
            // --- Unités de longueur ---
            if (Enum.TryParse(Properties.Settings.Default.DefaultUnit, out SupportedLengthUnit lengthUnit))
            {
                Application.Current.Resources["DefaultUnit"] = lengthUnit.ToString();
            }
            else
            {
                Application.Current.Resources["DefaultUnit"] = SupportedLengthUnit.Millimeter.ToString(); // valeur par défaut
            }

            // --- Unités de angle ---
            if (Enum.TryParse(Properties.Settings.Default.AngleUnit, out SupportedAnglehUnit angleUnit))
            {
                Application.Current.Resources["DefaultAngle"] = angleUnit.ToString();
            }
            else
            {
                Application.Current.Resources["DefaultAngle"] = SupportedAnglehUnit.Degre.ToString(); // valeur par défaut
            }

            // --- Unités de masse volumique ---
            if (Enum.TryParse(Properties.Settings.Default.DensityUnit, out SupportedDensityUnit densityUnit))
            {
                Application.Current.Resources["DensityUnit"] = densityUnit.ToString();
            }
            else
            {
                Application.Current.Resources["DensityUnit"] = SupportedDensityUnit.KgPerCubicMeter.ToString(); // valeur par défaut
            }

            // --- Langue ---
            Application.Current.Resources["Language"] =
                string.IsNullOrWhiteSpace(Properties.Settings.Default.Language)
                ? LanguageManager.LanguageFallBack.ToString()
                : Properties.Settings.Default.Language.ToString();

            // --- Thème ---
            Application.Current.Resources["Theme"] =
                string.IsNullOrWhiteSpace(Properties.Settings.Default.Theme)
                ? ThemeManager.ThemeFallBack.ToString()
                : Properties.Settings.Default.Theme.ToString();
        }

        /// <summary>
        /// Application des settings actuels 
        /// </summary>
        public static async Task ApplySettings()
        {
            LoadSettingsToResources();

            LanguageManager.ApplyFromSettings();
            ThemeManager.ApplyFromSettings();
        }



    }
}

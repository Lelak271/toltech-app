using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Toltech.App.Resources.ColorTheme
{
    public enum AppTheme
    {
        System,
        Light,
        Dark,
        ColorBlind
    }

    public static class ThemeManager
    {
        private const string FluentAssemblyUri = "pack://application:,,,/Fluent;component/Themes/";

        private static AppTheme _currentTheme = AppTheme.Light;
        public static AppTheme CurrentTheme => _currentTheme;
        public static AppTheme ThemeFallBack => AppTheme.Light;

        private static readonly string[] AllThemeFiles =
            Enum.GetValues(typeof(AppTheme))
                .Cast<AppTheme>()
                .Select(GetThemeFileName)
                .ToArray();

        /// <summary>
        /// Applique le thème défini dans les paramètres utilisateur.
        /// </summary>
        public static void ApplyFromSettings()
        {
            var themeValue = Properties.Settings.Default.Theme;

            if (!Enum.TryParse(themeValue, out AppTheme theme))
                theme = AppTheme.Light;

            ApplyTheme(theme);
        }

        /// <summary>
        /// Applique un thème donné à l’application.
        /// </summary>
        private static void ApplyTheme(AppTheme theme)
        {
            _currentTheme = theme;

            if (Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                ApplyFluentTheme(theme);
                ApplyColorTheme(theme);
            });
        }

        #region Fluent theme & AppColor

        private static void ApplyFluentTheme(AppTheme theme)
        {
            var merged = Application.Current.Resources.MergedDictionaries;
            string fluentFile = GetFluentThemeFile(theme);

            var fluentUri = new Uri(FluentAssemblyUri + fluentFile, UriKind.Absolute);

            // Suppression des anciens thèmes Fluent
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var src = merged[i].Source?.OriginalString;

                if (src != null &&
                    src.Contains("/Fluent;component/Themes/") &&
                    !src.EndsWith("Generic.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    merged.RemoveAt(i);
                }
            }

            // Ajouter le dictionnaire uniquement si ce n'est pas Generic.xaml
            if (!fluentFile.EndsWith("Generic.xaml", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Test d'existence de la ressource
                    var resourceInfo = Application.GetResourceStream(fluentUri);
                    if (resourceInfo != null)
                    {
                        merged.Add(new ResourceDictionary { Source = fluentUri });
                    }
                    else
                    {
                        // Optionnel : log ou traitement si la ressource n'existe pas
                        Debug.WriteLine($"La ressource {fluentUri} est introuvable.");
                    }
                }
                catch (IOException ex)
                {
                    // Optionnel : log ou traitement de l'erreur
                    Debug.WriteLine($"Erreur lors de l'accès à {fluentUri}: {ex.Message}");
                }
            }
        }

        private static void ApplyColorTheme(AppTheme theme)
        {
            var merged = Application.Current.Resources.MergedDictionaries;

            string fileName = GetThemeFileName(theme);
            string uri = $"pack://application:,,,/Toltech.App;component/Resources/ColorTheme/{fileName}";

            // Suppression des anciens thèmes couleur
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var source = merged[i].Source?.OriginalString;
                if (source != null &&
                    AllThemeFiles.Contains(System.IO.Path.GetFileName(source)))
                {
                    merged.RemoveAt(i);
                }
            }

            merged.Add(new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Absolute)
            });
        }

        #endregion

        #region Mapping fichiers

        private static string GetFluentThemeFile(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Light => "Generic.xaml",
                AppTheme.Dark => "Themes/Dark.Blue.xaml",
                AppTheme.System => "System.xaml",
                AppTheme.ColorBlind => "Generic.xaml",
                _ => "Generic.xaml"
            };
        }

        private static string GetThemeFileName(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.System => "SystemTheme.xaml",
                AppTheme.Light => "LightTheme.xaml",
                AppTheme.Dark => "DarkTheme.xaml",
                AppTheme.ColorBlind => "ColorBlindTheme.xaml",
                _ => "LightTheme.xaml"
            };
        }

        #endregion
    }

}

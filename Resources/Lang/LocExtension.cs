using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using TOLTECH_APPLICATION.Resources.Lang;

namespace TOLTECH_APPLICATION.Resources.Lang
{
    /// <summary>
    /// MarkupExtension WPF permettant de lier dynamiquement une clé de localisation
    /// à une propriété UI (ex: TextBlock.Text, Button.Content).
    /// 
    /// Utilise <see cref="LocalizationManager"/> pour récupérer la chaîne traduite
    /// correspondant à la clé spécifiée et met automatiquement à jour l'UI
    /// lorsque la culture change.
    /// 
    /// Exemple d'utilisation dans XAML :
    /// <TextBlock Text="{lang:Loc Key=Hello}" />
    /// </summary>
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationManager.Instance,
                Mode = BindingMode.OneWay
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}

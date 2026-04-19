using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TOLTECH_APPLICATION.Resources.Lang;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// Retourne l'image du drapeau correspondant à la culture actuelle.
    /// </summary>
    public class CultureToFlagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Récupère la culture actuelle depuis LocalizationManager
            var currentCulture = LocalizationManager.Instance.CurrentCulture ?? CultureInfo.CurrentUICulture;
            string twoLetter = currentCulture.TwoLetterISOLanguageName;

            // Crée le path de l'image
            string path = $"pack://application:,,,/Asset/Flags/{twoLetter}.png";

            // Retourne une BitmapImage
            try
            {
                return new BitmapImage(new Uri(path));
            }
            catch
            {
                return null; // ou un drapeau par défaut
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// Convertit un booléen en visibilité WPF :
    /// true  => Visible
    /// false => Collapsed
    /// 
    /// Si le paramètre "Invert" est fourni, inverse la logique.
    /// Exemple : ConverterParameter="Invert"
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public BoolToVisibilityConverter() { }

        /// <summary>
        /// Convertit le booléen en Visibility.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Vérifie que la valeur est bien un booléen
            if (value is not bool flag)
                return Visibility.Collapsed;

            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

            if (invert)
                flag = !flag;

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Conversion inverse : Visibility → bool.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Visibility visibility)
                return false;

            bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;

            bool result = visibility == Visibility.Visible;
            return invert ? !result : result;
        }
    }
}

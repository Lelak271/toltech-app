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
    /// Convertit une valeur nulle (ou vide) en Visibility.
    /// Null ou vide → Visible
    /// Non null → Collapsed
    /// </summary>
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Cas null
            if (value == null)
                return Visibility.Visible;

            // Cas tableau vide (ex : byte[])
            if (value is Array array && array.Length == 0)
                return Visibility.Visible;

            // Cas chaîne vide
            if (value is string str && string.IsNullOrWhiteSpace(str))
                return Visibility.Visible;

            // Valeur valide
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("NullToVisibilityConverter ne supporte pas ConvertBack.");
        }
    }
}

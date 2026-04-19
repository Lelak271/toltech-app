using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// Convertit un entier en Visibility.
    /// Retourne Visible si la valeur est 0, sinon Collapsed.
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

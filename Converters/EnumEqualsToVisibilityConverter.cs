using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Toltech.App.Converters
{
    /// <summary>
    /// Compare une valeur enum avec un paramètre (string ou enum)
    /// et retourne Visibility.Visible ou Visibility.Collapsed.
    /// </summary>
    public class EnumEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            bool isEqual;

            // Paramètre string (XAML)
            if (parameter is string paramString)
            {
                isEqual = string.Equals(
                    value.ToString(),
                    paramString,
                    StringComparison.Ordinal);
            }
            // Paramètre enum
            else
            {
                isEqual = value.Equals(parameter);
            }

            return isEqual
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}

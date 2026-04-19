using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Toltech.App.Resources.Styles
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringNullOrEmptyToVisibilityConverter : IMultiValueConverter
    {
        // values[0] : Text (string)
        // values[1] : IsKeyboardFocused (bool)
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var text = values?.Length > 0 ? values[0] as string : null;
            var isFocused = values?.Length > 1 && values[1] is bool b && b;

            // Afficher le hint seulement si texte vide ou null ET pas de focus
            return (string.IsNullOrEmpty(text) && !isFocused) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

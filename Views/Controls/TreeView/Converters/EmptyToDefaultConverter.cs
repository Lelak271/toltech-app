using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TOLTECH_APPLICATION.Views.Controls.TreeView.Converters
{
    public class EmptyToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value?.ToString(); // Convertit explicitement en chaîne (évite les erreurs si value n'est pas un string)
            string defaultValue = parameter?.ToString() ?? "[Vide]"; // Utilise "[Vide]" si parameter est null
            return string.IsNullOrEmpty(str) ? defaultValue : str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Si vous n'avez pas besoin de la conversion inverse, vous pouvez retourner DependencyProperty.UnsetValue
            return DependencyProperty.UnsetValue;
        }
    }
}

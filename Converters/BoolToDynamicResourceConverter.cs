using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Toltech.App.Converters
{
    public class BoolToDynamicResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                // Le paramètre contient les clés des ressources séparées par ";"
                // Exemple : parameter="Fixe;NotFix"
                string[] keys = (parameter as string)?.Split(';');
                if (keys?.Length == 2)
                {
                    string key = b ? keys[0] : keys[1];
                    return Application.Current.TryFindResource(key) ?? key;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

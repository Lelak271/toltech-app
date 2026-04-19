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
    /// Retourne Visibility.Visible si le modèle actif correspond au panel courant.
    /// Sinon retourne Visibility.Collapsed.
    /// </summary>
    public class ActiveModelToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return Visibility.Collapsed;

            string activeModel = values[0] as string;
            string panelPath = values[1] as string;

            return string.Equals(activeModel, panelPath, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Retourne false si le modèle actif correspond au panel courant.
    /// Sinon retourne true.
    public class ActiveModelToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Sécurité : vérifie le nombre et la validité des paramètres
            if (values == null || values.Length < 2)
                return false;

            var activeModel = values[0]?.ToString();
            var panelPath = values[1]?.ToString();

            if (string.IsNullOrWhiteSpace(activeModel) ||
                string.IsNullOrWhiteSpace(panelPath))
                return false;

            return string.Equals(
                activeModel,
                panelPath,
                StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

}

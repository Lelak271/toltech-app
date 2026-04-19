using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// Convertit la combinaison de la longueur du texte et de l'état de focus
    /// en une valeur Visibility pour les bindings WPF.
    /// </summary>
    /// <remarks>
    /// Utiliser dans le template des textblocks de l'application pour le placeholder
    /// Renvoie <see cref="Visibility.Visible"/> si le texte est vide et que le contrôle
    /// n'est pas focalisé, sinon <see cref="Visibility.Collapsed"/>.
    /// Utile, par exemple, pour afficher un texte d'indice (placeholder) uniquement
    /// lorsque l'entrée est vide et non sélectionnée.
    /// </remarks>
    public class TextOrFocusToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return Visibility.Visible;

            var textLength = values[0] as int? ?? 0;
            var isFocused = values[1] as bool? ?? false;

            return (textLength == 0 && !isFocused) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

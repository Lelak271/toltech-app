using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Toltech.App.Models;
using static Toltech.App.Models.ModelData;
using Toltech.Solver.Contracts;

namespace Toltech.App.Converters
{
    /// <summary>
    /// Convertit un LiaisonType en Visibility selon la position (paramètre).
    /// Usage : Visibility="{Binding Linkage, Converter={StaticResource LinkageVisibilityConverter}, ConverterParameter=1}"
    /// </summary>
    public class LinkageVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not LinkageType linkage) return Visibility.Collapsed;
            if (parameter is not string p || !int.TryParse(p, out int idx)) return Visibility.Collapsed;
            return LinkagePanelMap.IsPanelAllowed(linkage, idx) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Active/désactive un bouton selon la liaison en cours
    public class LinkageAllowedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not LinkageType linkage) return false;
            if (parameter is not string p || !int.TryParse(p, out int idx)) return false;
            return LinkagePanelMap.IsPanelAllowed(linkage, idx);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Coche/décoche un RadioButton selon le groupe sélectionné (TwoWay)
    public class SelectedGroupToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selected && parameter is string p && int.TryParse(p, out int group))
                return selected == group;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Appelé quand l'utilisateur clique sur CE radio button précis
            if (value is bool isChecked && isChecked && parameter is string p && int.TryParse(p, out int group))
                return group;
            return Binding.DoNothing; // évite d'écraser avec "false" quand un autre bouton du groupe se décoche
        }
    }

    // Affiche/masque un StackPanel de contenu selon le groupe sélectionné
    public class SelectedGroupToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selected && parameter is string p && int.TryParse(p, out int target))
                return selected == target ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

}
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TOLTECH_APPLICATION.ViewModels;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Resources;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// Convertit un Id de pièce (part) en son nom (NamePart)
    /// </summary>
    public class IdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mainVM = App.MainVM;
            if (mainVM?.Parts == null)
                return string.Empty;

            // Vérifie que value est un int valide
            if (value is not int id || id <= 0)
                return string.Empty;

            // Recherche la pièce correspondant à l'id
            var part = mainVM.Parts.FirstOrDefault(p => p != null && p.Id == id);
            return part?.NamePart ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing; // lecture seule
        }
    }
}

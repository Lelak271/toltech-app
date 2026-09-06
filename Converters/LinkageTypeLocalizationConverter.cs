using System;
using System.Globalization;
using System.Windows.Data;
using Toltech.App.Resources.Lang;
using Toltech.App.Utilities;
using Toltech.Solver.Contracts;

namespace Toltech.App.Converters
{
    public sealed class LinkageTypeLocalizationConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            // values[0] = LinkageType (l'item)
            // values[1] = CurrentCulture de LocalizationManager (sert juste de déclencheur)
            if (values.Length == 0 || values[0] is not LinkageType type)
                return string.Empty;

            return LocalizationManager.Instance[type.GetLocalizationKey()];
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            return new object[] { Binding.DoNothing };
        }
    }
}
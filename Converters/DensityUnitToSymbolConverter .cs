using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TOLTECH_APPLICATION.Resources;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// Convertit un SupportedDensityUnit en sa représentation scientifique lisible (kg/m³, g/cm³, lb/ft³, lb/in³) pour l’affichage dans l’UI WPF.
    /// </summary>
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public class DensityUnitToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            SupportedDensityUnit unit;

            // Si la valeur est déjà un enum
            if (value is SupportedDensityUnit enumValue)
            {
                unit = enumValue;
            }
            // Si la valeur est une string (DynamicResource)
            else if (value is string strValue &&
                     Enum.TryParse(strValue, out SupportedDensityUnit parsed))
            {
                unit = parsed;
            }
            else
            {
                return "";
            }

            // Retourne la représentation scientifique
            return unit switch
            {
                SupportedDensityUnit.KgPerCubicMeter => "kg/m³",
                SupportedDensityUnit.GramPerCm3 => "g/cm³",
                SupportedDensityUnit.PoundPerCubicFoot => "lb/ft³",
                SupportedDensityUnit.PoundPerCubicInch => "lb/in³",
                _ => unit.ToString()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

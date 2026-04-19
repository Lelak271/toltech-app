using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using static TOLTECH_APPLICATION.Models.NodesDefinition;

namespace TOLTECH_APPLICATION.Views.Controls.TreeView.Converters
{
    public class NodeTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not NodeType type)
                return Brushes.WhiteSmoke;

            return type switch
            {
                NodeType.ModelFolder => new LinearGradientBrush(Colors.LightGray, Colors.White, 90),
                NodeType.PositionnementFolder => new SolidColorBrush(Color.FromRgb(210, 225, 245)),
                NodeType.ExigencesFolder => new SolidColorBrush(Color.FromRgb(215, 240, 215)),
                NodeType.PartNode => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                NodeType.RequirementNode => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                _ => Brushes.Gainsboro
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

}

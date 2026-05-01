using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using static Toltech.App.Models.NodesDefinition;

namespace Toltech.App.Views.Controls.TreeView.Converters
{
    public class NodeTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not NodeType type)
                return null;

            string iconPath = type switch
            {
                NodeType.ModelFolder => "pack://application:,,,/Asset/FolderModel.png",
                NodeType.PositionnementFolder => "pack://application:,,,/Asset/PARTS.png",
                NodeType.ExigencesFolder => "pack://application:,,,/Asset/BiPoint.png",
                NodeType.PartNode => "pack://application:,,,/Asset/Engrenage.png",
                NodeType.RequirementNode => "pack://application:,,,/Asset/Exigence.png",
                NodeType.DataNode => "pack://application:,,,/Asset/Contact.png",
                NodeType.Folder => "pack://application:,,,/Asset/SimpleFolder.png",
                _ => "pack://application:,,,/Asset/TestGrapheButton.png"
            };

            // Retourne directement un BitmapImage
            return new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

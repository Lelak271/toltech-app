using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace TOLTECH_APPLICATION.Converters
{
    /// <summary>
    /// TODO
    /// </summary>
    public class EyeStateToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = value as bool? ?? false;

            string iconPath = isChecked
           ? "pack://application:,,,/Asset/eyeshow.png"
           : "pack://application:,,,/Asset/eyehide.png";
            return new BitmapImage(new Uri(iconPath, UriKind.Absolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

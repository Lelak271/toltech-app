using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Toltech.App.Converters
{
    public class BooleanToImageConverter : IValueConverter
    {
        public string TrueImage { get; set; }
        public string FalseImage { get; set; }

        private static readonly Dictionary<string, BitmapImage> _cache = new();

        private static BitmapImage GetCachedImage(string path)
        {
            if (!_cache.TryGetValue(path, out var image))
            {
                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                _cache[path] = image;
            }
            return image;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTrue = value as bool? ?? false;
            string path = isTrue ? TrueImage : FalseImage;

            if (string.IsNullOrEmpty(path))
                return DependencyProperty.UnsetValue;

            return GetCachedImage(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TOLTECH_APPLICATION.Behaviors
{
    public static class PlaceholderService
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(PlaceholderService),
                new FrameworkPropertyMetadata(string.Empty));

        public static void SetPlaceholder(DependencyObject element, string value)
            => element.SetValue(PlaceholderProperty, value);

        public static string GetPlaceholder(DependencyObject element)
            => (string)element.GetValue(PlaceholderProperty);
    }
}

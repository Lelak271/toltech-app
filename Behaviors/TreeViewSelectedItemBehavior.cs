using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Toltech.App.Behaviors
{
    public static class TreeViewSelectedItemBehavior
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object),
                typeof(TreeViewSelectedItemBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged)
            );

        public static void SetSelectedItem(DependencyObject element, object value) =>
            element.SetValue(SelectedItemProperty, value);

        public static object GetSelectedItem(DependencyObject element) =>
            element.GetValue(SelectedItemProperty);

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TreeView treeView)
                return;

            if (e.NewValue != null)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is not TreeView treeView)
                return;

            // Mettre à jour la propriété attachée
            SetSelectedItem(treeView, e.NewValue);

            // Message de debug pour vérifier le changement
            if (e.NewValue != null)
            {
                Debug.WriteLine($"TreeView selected item changed: {e.NewValue}");
            }
            else
            {
                Debug.WriteLine("TreeView selected item changed: null");
            }
        }
    }
}

using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace TOLTECH_APPLICATION.Behaviors
{
    public static class DataGridSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(DataGridSelectedItemsBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static void SetSelectedItems(DependencyObject element, IList value)
            => element.SetValue(SelectedItemsProperty, value);

        public static IList? GetSelectedItems(DependencyObject element)
        {
            return element.GetValue(SelectedItemsProperty) as IList;
        }


        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DataGridSelectedItemsBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject element, bool value)
            => element.SetValue(EnableProperty, value);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid || e.NewValue is not bool enabled || !enabled)
                return;

            grid.SelectionChanged += Grid_SelectionChanged;
        }

        private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            var list = GetSelectedItems(grid);

            // Sécurité absolue
            if (list == null)
                return;

            // Empêche crash lors des clics hors lignes
            if (grid.SelectedItems == null || grid.SelectedItems.Count == 0)
            {
                list.Clear();
                return;
            }

            list.Clear();

            foreach (var item in grid.SelectedItems)
            {
                if (item != null)
                    list.Add(item);
            }
        }

    }
}

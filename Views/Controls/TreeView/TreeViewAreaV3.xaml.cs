using System.Diagnostics;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Toltech.App.Behaviors;
using Toltech.App.Models;
using Toltech.App.Resources;
using Toltech.App.Services;
using Toltech.App.Utilities;
using Toltech.App.Views.Controls.TreeView;
using static Toltech.App.FrontEnd.Controls.Dashboard.BarChartControl;
using static Toltech.App.Models.NodesDefinition;

namespace Toltech.App.FrontEnd.Controls
{
    /// <summary>
    /// Logique d'interaction pour TreeViewAreaV3.xaml
    /// </summary>
    /// // NOTE : La gestion actuelle du Drag & Drop du TreeView est fonctionnelle de manière basique,
    // mais présente des comportements non optimaux (sélection, déclenchement, cohérence UX).
    // Cette implémentation est volontairement temporaire et devra être revue et refactorisée
    // ultérieurement pour une prise en charge plus robuste et conforme aux usages standards.
    public partial class TreeViewAreaV3 : UserControl
    {
        public TreeViewAreaV3ViewModel ViewModel
        {
            get => (TreeViewAreaV3ViewModel)DataContext;
            set => DataContext = value;
        }

        public TreeViewAreaV3()
        {
            InitializeComponent();
        }

        private void TreeViewControlV3_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ViewModel is null) return;
            if (e.NewValue is not NodesDefinition node) return;

            //ViewModel.SelectedNode = node;

            // Sync la sélection simple avec la sélection multiple
            // Si le nœud n'est pas déjà dans la sélection multiple (cas clic simple sans Ctrl)
            if (!ViewModel.SelectedNodes.Contains(node))
                ViewModel.UpdateSelectedNodes(new List<NodesDefinition> { node });
        }

        private async void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if (sender is not TreeViewItem item)
                return;

            if (item.DataContext is not NodesDefinition node)
                return;

            e.Handled = true;

            item.IsSelected = true;
            ViewModel.HandleNodeDoubleClickAsync(node);
        }

        private async void TreeViewControlV3_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi &&
                tvi.DataContext is NodesDefinition node)
            {
                e.Handled = true;
                await ViewModel.OnNodeExpandedAsync(node);
            }
        }

        private async void TreeViewControlV3_Collapsed(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi &&
                tvi.DataContext is NodesDefinition node)
            {
                e.Handled = true;
                await ViewModel.OnNodeCollapsedAsync(node);
            }
        }


        #region Renaming Inline

        /// <summary>
        /// Pure UI :Textblock et TextBox en Visibility inversé via un NodesDefinition.IsEditing (Bool) 
        /// Back : Gère la perte de focus fu champ TextBox utilisé pour renommer un node.
        /// Selon le type du nœud (dossier, pièce ou exigence),
        /// la méthode de fin d’édition correspondante est appelée.
        /// </summary>
        private async void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not NodesDefinition node)
                return;

            if (e.Key is Key.Enter or Key.Return)
            {
                await CommitAndRestoreFocusAsync(tb, node, tb.Text);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                //ViewModel.CancelRename(node);
                await CommitAndRestoreFocusAsync(tb, node, node.NodeName);
                e.Handled = true;
            }
        }

        private async void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not NodesDefinition node)
                return;

            await CommitAndRestoreFocusAsync(tb, node, tb.Text);
        }

        private bool _isCommitting;
        // Seul helper UI restant dans le code-behind
        private async Task CommitAndRestoreFocusAsync(TextBox tb, NodesDefinition node, string newName)
        {
            if (_isCommitting) return;
            _isCommitting = true;

            try
            {
                tb.IsReadOnly = true;

                bool success = await ViewModel.CommitRenameAsync(node, newName);

                if (success)
                    tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource(); // ← pousse NodeName
                else
                    tb.Text = node.NodeName; // ← restaure l'affichage

                RestoreTreeViewItemFocus(tb);
            }
            finally
            {
                _isCommitting = false;
            }
        }

        #region Focus on TextBox For Renaming
        /// <summary>
        /// Parcours du TreeView pour trouver le TextBox IsEditing
        /// Energivore mais pas d'autre solution pour le moement
        /// Avantage, pure UI donc pas gênant
        /// </summary>
        /// <param name="node"></param>
        private void FocusTextBoxForNode(NodesDefinition node)
        {
            if (node == null) return;
            foreach (var item in GetTreeViewItems(TreeViewControlV3))
            {
                if (item.DataContext == node)
                {
                    ActivateRenameTextBox(item); // ← activation complète
                    break;
                }
            }
        }

        /// <summary>
        /// Parcourt récursivement le TreeView pour obtenir tous les TreeViewItems
        /// </summary>
        private IEnumerable<TreeViewItem> GetTreeViewItems(ItemsControl parent)
        {
            foreach (var obj in parent.Items)
            {
                var tvi = parent.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                if (tvi != null)
                {
                    yield return tvi;

                    foreach (var child in GetTreeViewItems(tvi))
                        yield return child;
                }
            }
        }

        // Méthode dédiée à l'activation du mode édition
        private void ActivateRenameTextBox(DependencyObject parent)
        {
            if (parent == null) return;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox tb && tb.Visibility == Visibility.Visible)
                {
                    tb.IsReadOnly = false; // ← uniquement à l'activation
                    tb.Focus();
                    tb.SelectAll();
                    return;
                }
                ActivateRenameTextBox(child);
            }
        }


        private static void RestoreTreeViewItemFocus(TextBox tb)
        {
            DependencyObject parent = tb;

            while (parent != null && parent is not TreeViewItem)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is TreeViewItem item)
            {
                item.IsSelected = true;
                //item.Focus();
            }
        }

        #endregion

        #endregion

        #region ContextMenu Generation

        private void TreeViewControlV3_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not System.Windows.Controls.TreeView tree) return;

            // Récupère l'élément sélectionné
            if (tree.SelectedItem is not NodesDefinition selectedNode) return;

            tree.ContextMenu = BuildContextMenu(selectedNode);
        }

        private ContextMenu BuildContextMenu(NodesDefinition node)
        {
            var menu = new ContextMenu();
            if (node == null) return menu;

            var vm = this.DataContext as TreeViewAreaV3ViewModel;
            if (vm == null) return menu;

            if (node.IsFolder)
                BuildContextMenu_Folder(menu, node, vm);
            if (node.Type == NodeType.PartNode ||
                node.Type == NodeType.RequirementNode)
            {
                BuildContextMenu_Node(menu, node, vm);
            }

            foreach (MenuItem item in menu.Items.OfType<MenuItem>())
                item.HorizontalContentAlignment = HorizontalAlignment.Left;

            return menu;
        }

        private void BuildContextMenu_Folder(ContextMenu menu, NodesDefinition folder, TreeViewAreaV3ViewModel vm)
        {
            // Ajouter un sous-dossier
            var addItem = new MenuItem { Header = "Add Folder" };
            addItem.Command = vm.AddSubFolderCommand;
            addItem.CommandParameter = folder;

            // Supprimer un dossier
            var deleteItem = new MenuItem { Header = "Delete Folder" };
            deleteItem.Command = vm.DeleteFolderCommand;
            deleteItem.CommandParameter = folder;

            // Renommer un dossier
            var renameItem = new MenuItem { Header = "Rename Folder" };
            renameItem.Click += (s, e) =>
            {
                folder.IsEditing = true;  // juste le mode édition

                // Focus sur le TextBox du node
                FocusTextBoxForNode(folder);
            };

            // Créer node Req
            var createReq = new MenuItem { Header = "✏️ Create Req" };
            createReq.Command = vm.CreateRequirementFromTreeCommand;

            // Créer node Part
            var createPart = new MenuItem { Header = "✏️ Create Part" };
            createPart.Command = vm.CreatePartFromTreeCommand;
         
            menu.Items.Add(addItem);

            if (folder.Type == NodeType.Folder)
                menu.Items.Add(deleteItem);
            menu.Items.Add(renameItem);


            if (folder.Type == NodeType.ExigencesFolder)
                menu.Items.Add(createReq);

            if (folder.Type == NodeType.PositionnementFolder)
                menu.Items.Add(createPart);

        }

        private void BuildContextMenu_Node(ContextMenu menu, NodesDefinition node, TreeViewAreaV3ViewModel vm)
        {
            if (node == null) return;

            // Ajouter un sous-dossier
            var addSubFolder = new MenuItem { Header = "🗂 Add A New Subfolder" };
            addSubFolder.Command = vm.GroupSelectionIntoSubFolderCommand;
            addSubFolder.CommandParameter = GetSelectedNodes(); ;

            // Supprimer le node
            var deleteNode = new MenuItem { Header = "🗑 Delete Part" };
            deleteNode.Command = vm.DeleteNodePartCommand;
            deleteNode.CommandParameter = node;

            // Renommer le node
            var renameItem = new MenuItem { Header = "✏️ Rename" };
            renameItem.Click += (s, e) =>
            {
                node.IsEditing = true;  // juste le mode édition
                // Focus sur le TextBox du node
                FocusTextBoxForNode(node);
            };

            // Désactiver part node
            var desactiveNode = new MenuItem { Header = "Activer / Desactiver" };
            desactiveNode.Command = vm.DesactiveNodePartCommand;
            desactiveNode.CommandParameter = node;

            // Créer node Req
            var createReq = new MenuItem { Header = "✏️ Create Req" };
            createReq.Command = vm.CreateRequirementFromTreeCommand;

            // Deleted node Req
            var DeleteReq = new MenuItem { Header = "✏️ Delete Req" };
            DeleteReq.Command = vm.DeleteRequirementFromTreeCommand;
            DeleteReq.CommandParameter = node;

            // Propriétés
            var properties = new MenuItem { Header = "ℹ️ Properties" };
            properties.Command = vm.ShowNodePropertiesCommand;
            properties.CommandParameter = node;


            if (node.Type == NodeType.PartNode)
            {
                menu.Items.Add(addSubFolder);
                menu.Items.Add(new Separator());
                menu.Items.Add(renameItem);
                menu.Items.Add(deleteNode);
                menu.Items.Add(desactiveNode);
                menu.Items.Add(new Separator());
                menu.Items.Add(properties);
            }
            if (node.Type == NodeType.RequirementNode)
            {
                menu.Items.Add(addSubFolder);
                menu.Items.Add(new Separator());
                menu.Items.Add(renameItem);
                menu.Items.Add(desactiveNode);
                menu.Items.Add(DeleteReq);
                menu.Items.Add(createReq);
                menu.Items.Add(new Separator());
                menu.Items.Add(properties);
            }

            // Actions globales
            var actions = new MenuItem { Header = "⚡ Actions" };
            actions.Items.Add(new MenuItem { Header = "Create PArt", Command = vm.CreatePartFromTreeCommand });
            actions.Items.Add(new MenuItem { Header = "📊 Export Data", Command = vm.ExportDataCommand, CommandParameter = node });

            menu.Items.Add(new Separator());
            menu.Items.Add(actions);
        }

        #endregion

        #region Drag & Drop UI

        // ── état drag ──────────────────────────────────────────────
        private Point _dragStartPoint;
        private DateTime _dragStartTime;
        private bool _isDragging;

        // ── état drop ──────────────────────────────────────────────
        private NodesDefinition? _dropTargetNode;
        private bool _insertAbove;

        // ── adorner ────────────────────────────────────────────────
        private AdornerLayer? _adornerLayer;
        private InsertionLineAdorner? _insertionAdorner;

        // ── sélection ──────────────────────────────────────────────
        // Source de vérité unique : ViewModel.SelectedNodes (List<NodesDefinition>)
        // _selectedItems ne sert qu'au rendu visuel WPF
        private readonly List<TreeViewItem> _selectedItems = new();
        private TreeViewItem? _lastSelectedItem;

        #region --- Souris ---

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            if (source is null || IsClickOnTextBox(source) || IsIgnoredElement(source))
                return;

            var item = VisualUpwardSearch<TreeViewItem>(source);
            if (item is null) return;

            _dragStartPoint = e.GetPosition(null);
            _dragStartTime = DateTime.Now;
            _isDragging = false;

            HandleSelection(item, e);
            _lastSelectedItem = item;

            e.Handled = false;
        }

        private void TreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;

            var source = e.OriginalSource as DependencyObject;
            if (source is not null && IsClickOnTextBox(source)) return;

            var diff = _dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) < 6 && Math.Abs(diff.Y) < 6) return;

            // Récupère les nœuds sélectionnés comme données du drag
            var nodesToDrag = GetSelectedNodes();
            if (!nodesToDrag.Any()) return;

            _isDragging = true;
            _dragStartTime = DateTime.Now;

            var dragData = new DataObject(typeof(List<NodesDefinition>), nodesToDrag);
            DragDrop.DoDragDrop((DependencyObject)e.Source, dragData, DragDropEffects.Move);

            _isDragging = false;
        }

        #endregion

        #region --- Drop ---

        private void TreeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            var targetItem = VisualUpwardSearch<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (targetItem?.DataContext is not NodesDefinition dropTarget)
            {
                ClearInsertionLine();
                return;
            }

            var position = e.GetPosition(targetItem);
            _insertAbove = position.Y < targetItem.ActualHeight / 2;
            _dropTargetNode = dropTarget;

            // Folder → surbrillance, sinon → ligne d'insertion
            if (dropTarget.IsFolder)
            {
                ClearInsertionLine();        // enlève la ligne si on vient d'un non-folder
                ShowFolderHighlight(targetItem);
            }
            else
            {
                ClearFolderHighlight();      // enlève la surbrillance si on vient d'un folder
                ShowInsertionLine(targetItem, _insertAbove);
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private async void TreeView_Drop(object sender, DragEventArgs e)
        {
            ClearAllAdorners(); // ← remplace ClearInsertionLine()

            var dragDuration = DateTime.Now - _dragStartTime;
            if (dragDuration < TimeSpan.FromMilliseconds(200))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (!e.Data.GetDataPresent(typeof(List<NodesDefinition>))) return;
            if (_dropTargetNode is null) return;

            var nodesToMove = e.Data.GetData(typeof(List<NodesDefinition>)) as List<NodesDefinition>;
            if (nodesToMove is null || !nodesToMove.Any()) return;

            await ViewModel.MoveNodes(nodesToMove, _dropTargetNode, _insertAbove);

            ClearSelection();
            ViewModel.OnNodeDropped(nodesToMove, _dropTargetNode);
        }

        #endregion

        #region --- Sélection ---

        private void HandleSelection(TreeViewItem item, MouseButtonEventArgs e)
        {
            bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (ctrl)
            {
                ToggleItemSelection(item);
            }
            else if (shift && _lastSelectedItem is not null)
            {
                SelectRange(_lastSelectedItem, item);
            }
            else
            {
                ClearSelection();
                SetItemSelected(item, true);
            }
        }

        private void ToggleItemSelection(TreeViewItem item)
            => SetItemSelected(item, !_selectedItems.Contains(item));

        private void SetItemSelected(TreeViewItem item, bool selected)
        {
            if (item.DataContext is not NodesDefinition node) return;

            node.IsSelected = selected;

            if (selected)
            {
                if (!_selectedItems.Contains(item))
                    _selectedItems.Add(item);
            }
            else
            {
                _selectedItems.Remove(item);
            }

            UpdateItemVisualState(item, selected);
            ViewModel.UpdateSelectedNodes(GetSelectedNodes()); // sync VM
        }

        private void ClearSelection()
        {
            foreach (var item in _selectedItems.ToList())
            {
                if (item.DataContext is NodesDefinition node)
                    node.IsSelected = false;
                UpdateItemVisualState(item, false);
            }
            _selectedItems.Clear();
            ViewModel.UpdateSelectedNodes(new List<NodesDefinition>());
        }

        private void SelectRange(TreeViewItem start, TreeViewItem end)
        {
            var all = GetAllVisibleTreeViewItems();
            int startIdx = all.IndexOf(start);
            int endIdx = all.IndexOf(end);
            if (startIdx == -1 || endIdx == -1) return;

            int min = Math.Min(startIdx, endIdx);
            int max = Math.Max(startIdx, endIdx);

            for (int i = min; i <= max; i++)
                SetItemSelected(all[i], true);
        }

        #endregion

        #region --- Insertion line ---

        private FolderHighlightAdorner? _folderHighlightAdorner;

        private void ShowFolderHighlight(TreeViewItem target)
        {
            ClearAllAdorners();

            if (_folderHighlightAdorner?.AdornedElement == target)
                return; // déjà sur ce folder — pas de recréation inutile

            ClearFolderHighlight();

            _adornerLayer = AdornerLayer.GetAdornerLayer(target);
            if (_adornerLayer is null) return;

            _folderHighlightAdorner = new FolderHighlightAdorner(target);
            _adornerLayer.Add(_folderHighlightAdorner);
        }

        // Nettoie les deux en même temps
        private void ClearAllAdorners()
        {
            ClearInsertionLine();
            ClearFolderHighlight();
            _adornerLayer = null;
        }

        private void ClearFolderHighlight()
        {
            if (_folderHighlightAdorner is null) return;

            // Récupère le layer depuis l'élément décoré existant
            // et non depuis _adornerLayer qui peut pointer ailleurs
            var layer = AdornerLayer.GetAdornerLayer(_folderHighlightAdorner.AdornedElement);
            layer?.Remove(_folderHighlightAdorner);
            _folderHighlightAdorner = null;
        }



        private void ShowInsertionLine(TreeViewItem target, bool insertAbove)
        {
            ClearInsertionLine();
            _adornerLayer = AdornerLayer.GetAdornerLayer(target);
            if (_adornerLayer is null) return;

            _insertionAdorner = new InsertionLineAdorner(target, insertAbove);
            _adornerLayer.Add(_insertionAdorner);
        }

        private void ClearInsertionLine()
        {
            if (_insertionAdorner is null) return;

            var layer = AdornerLayer.GetAdornerLayer(_insertionAdorner.AdornedElement);
            layer?.Remove(_insertionAdorner);
            _insertionAdorner = null;
        }

        #endregion

        #region --- Helpers visuels ---

        private List<NodesDefinition> GetSelectedNodes()
            => _selectedItems
                .Where(i => i.DataContext is NodesDefinition)
                .Select(i => (NodesDefinition)i.DataContext)
                .ToList();

        /// <summary>
        /// Récupère tous les TreeViewItem visibles (expandés uniquement).
        /// </summary>
        private List<TreeViewItem> GetAllVisibleTreeViewItems()
        {
            var items = new List<TreeViewItem>();
            foreach (var root in TreeViewControlV3.Items)
            {
                if (TreeViewControlV3.ItemContainerGenerator
                        .ContainerFromItem(root) is TreeViewItem rootItem)
                    CollectVisibleItems(rootItem, items);
            }
            return items;
        }

        private static void CollectVisibleItems(TreeViewItem item, List<TreeViewItem> list)
        {
            list.Add(item);
            if (!item.IsExpanded) return; // ne descend pas dans les nœuds fermés

            foreach (var child in item.Items)
            {
                if (item.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem childItem)
                    CollectVisibleItems(childItem, list);
            }
        }

        private void UpdateItemVisualState(TreeViewItem item, bool isSelected)
        {
            var border = GetBorder(item);
            if (border is null) return;

            if (isSelected)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(40, 0, 220, 215));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
                border.BorderThickness = new Thickness(0);
                border.CornerRadius = new CornerRadius(5);
            }
            else
            {
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
            }
        }

        private Border? GetBorder(TreeViewItem item)
            => FindChildByName<Border>(item, "PART_SelectionBorder");

        private static T? FindChildByName<T>(DependencyObject? parent, string name)
            where T : FrameworkElement
        {
            if (parent is null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed && typed.Name == name) return typed;
                var result = FindChildByName<T>(child, name);
                if (result is not null) return result;
            }
            return null;
        }

        private static bool IsClickOnTextBox(DependencyObject source)
        {
            while (source is not null)
            {
                if (source is TextBox) return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        private static bool IsIgnoredElement(DependencyObject source)
        {
            while (source is not null)
            {
                if (source is ToggleButton || source is ScrollBar) return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        private static T? VisualUpwardSearch<T>(DependencyObject source)
            where T : DependencyObject
        {
            while (source is not null && source is not T)
                source = VisualTreeHelper.GetParent(source);
            return source as T;
        }

        [Conditional("DEBUG")]
        private void DebugSelectedItems()
        {
            Debug.WriteLine("── Sélection ──");
            foreach (var item in _selectedItems)
            {
                var label = item.DataContext is NodesDefinition n
                    ? $"{n.NodeName} ({n.Type})"
                    : "DataContext inconnu";
                Debug.WriteLine($"• {label}");
            }
        }

        #endregion
        #endregion
    }

    /// <summary>
    /// Renders a visual highlight around a UI element to indicate selection or focus.
    /// </summary>
    public class FolderHighlightAdorner : Adorner
    {
        private static readonly Brush FillBrush =
            new SolidColorBrush(Color.FromArgb(40, 0, 200, 180));

        private static readonly Pen BorderPen =
            new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 180, 160)), 1.5)
            {
                DashStyle = DashStyles.Dash
            };

        static FolderHighlightAdorner()
        {
            FillBrush.Freeze();
            BorderPen.Freeze();
        }

        public FolderHighlightAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            var rect = new Rect(
                new Point(2, 2),
                new Size(
                    AdornedElement.RenderSize.Width - 4,
                    AdornedElement.RenderSize.Height - 4));

            dc.DrawRoundedRectangle(FillBrush, BorderPen, rect, 5, 5);
        }
    }

}

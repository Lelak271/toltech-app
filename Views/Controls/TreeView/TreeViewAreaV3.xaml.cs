using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TOLTECH_APPLICATION.Behaviors;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Resources;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Utilities;
using TOLTECH_APPLICATION.Views.Controls.TreeView;
using static TOLTECH_APPLICATION.Models.NodesDefinition;

namespace TOLTECH_APPLICATION.FrontEnd.Controls
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
            if(ViewModel== null) return; // TODO pourquoi la VM est null
            ViewModel.SelectedNode = e.NewValue as NodesDefinition ?? ViewModel.SelectedNode;
        }
        private async void TreeViewControlV3_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TreeView treeView &&
                treeView.SelectedItem is NodesDefinition node)
            {
                await HandleNodeDoubleClickAsync(node);
            }
        }

        private async void TreeViewItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TreeViewItem item)
                return;

            if (item.DataContext is not NodesDefinition node)
                return;

            // Type = 6 → pas d'expand / collapse au double-clic
            if (node.Type == NodeType.PartNode)
            {
                // Forcer la sélection
                item.IsSelected = true;
                //item.Focus();

                await HandleNodeDoubleClickAsync(node);

                e.Handled = true;

            }
            //
            if (node.Type == NodeType.DataNode)
            {
                // Forcer la sélection
                item.IsSelected = true;
                //item.Focus();

                await HandleNodeDoubleClickAsync(node);

                e.Handled = true;

            }
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

        private async Task HandleNodeDoubleClickAsync(NodesDefinition node)
        {
            if (node.Type == NodeType.PartNode)
            {
                //ModelManager.PartIDActif = node.LinkedOriginalId;
                await EventsManager.RaisePartSelectedChangedAsync(node.LinkedOriginalId);
                Debug.WriteLine("EventsManager.RaisePartSelectedChanged");
                return;
            }
            if (node.Type == NodeType.DataNode)
            {
                ViewModel.PropagateSelectionToDataVM(node);
                return;
            }

            if (node.Type == NodeType.RequirementNode)
            {
                var visibleRequirementIds = await ViewModel.ListIDReqOfSelectFolderAsync();
                var nameParentFolder = await ViewModel.NameParentFolderAsync();

                await EventsManager.RaiseNodReqSelectChangedAsync(
                    visibleRequirementIds,
                    nameParentFolder
                );
            }
        }

        #region Renaming Inline

        /// <summary>
        /// Pure UI :Textblock et TextBox en Visibility inversé via un NodesDefinition.IsEditing (Bool) 
        /// Back : Gère la perte de focus fu champ TextBox utilisé pour renommer un node.
        /// Selon le type du nœud (dossier, pièce ou exigence),
        /// la méthode de fin d’édition correspondante est appelée.
        /// </summary>
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (sender is not TextBox tb || tb.DataContext is not NodesDefinition node)
                return;

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                CommitRename(tb, node);
                RestoreTreeViewItemFocus(tb);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                tb.Text = node.NodeName;
                CommitRename(tb, node);
                RestoreTreeViewItemFocus(tb);
                e.Handled = true;
            }
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not NodesDefinition node)
                return;

            CommitRename(tb, node);
        }

        private bool _renameCommitted;
        private void CommitRename(TextBox tb, NodesDefinition node)
        {
            if (_renameCommitted)
                return;

            _renameCommitted = true;

            switch (node.Type)
            {
                case NodeType.ExigencesFolder:
                case NodeType.PositionnementFolder:
                case NodeType.ModelFolder:
                    EndRenameFolder(node);
                    break;

                case NodeType.PartNode:
                    EndRenamePart(node);
                    break;

                case NodeType.RequirementNode:
                    EndRenameReq(node);
                    break;
                case NodeType.Normal:
                    break;
            }

            // Sortie contrôlée du mode édition
            tb.IsReadOnly = true;
            tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            _renameCommitted = false;
        }

        private void EndRenameFolder(NodesDefinition folder)
        {
            folder.IsEditing = false;
            string newname = folder.NodeName;
            if (!NameValidationHelper.TryValidateName(folder.NodeName, out string errorMessage)) return;

            if (DataContext is TreeViewAreaV3ViewModel vm)
                vm.RenameFolderAsync(folder, folder.NodeName);
        }
        private void EndRenamePart(NodesDefinition part)
        {
            part.IsEditing = false;

            if (!NameValidationHelper.TryValidateName(part.NodeName, out string errorMessage)) return;

            if (DataContext is TreeViewAreaV3ViewModel vm)
                vm.RenamePartAsync(part); // New name déjà présent, on utilise l'ID pour tout renommer
        }
        private void EndRenameReq(NodesDefinition req)
        {
            req.IsEditing = false;

            if (!NameValidationHelper.TryValidateName(req.NodeName, out string errorMessage)) return;

            if (DataContext is TreeViewAreaV3ViewModel vm)
                vm.RenameReqAsync(req, req.NodeName);
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
                    FocusFirstVisibleTextBox(item);
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

        /// <summary>
        /// Parcourt l'arbre visuel à partir d'un parent et focus sur le premier TextBox visible trouvé
        /// </summary>
        private bool FocusFirstVisibleTextBox(DependencyObject parent)
        {
            if (parent == null) return false;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBox tb && tb.Visibility == Visibility.Visible)
                {
                    tb.Focus();
                    tb.CaretIndex = tb.Text.Length; // curseur à la fin
                    return true;
                }

                if (FocusFirstVisibleTextBox(child))
                    return true;
            }

            return false;
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

            if (folder.Type == NodeType.Normal)
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

        private TreeViewItem? _draggedItem;
        private AdornerLayer? _adornerLayer;
        private InsertionLineAdorner? _insertionAdorner;
        private NodesDefinition? _dropTargetNode;
        private TreeViewItem? _lastSelectedItem;
        private bool _insertAbove = true;
        private Point _dragStartPoint;
        private DateTime _dragStartTime;
        private List<TreeViewItem> _selectedItems = new();

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            var source = e.OriginalSource as DependencyObject;

            // Ignore TextBox (édition de nom)
            if (source == null)
                return;

            if (IsClickOnTextBox(source))
                return;

            // Ignore expand/collapse button, scrollbar, ou chevron
            if (IsIgnoredElement(source))
                return;

            var item = VisualUpwardSearch<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;
            _draggedItem = item;

            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift);
            if (isCtrlPressed)
            {
                bool newState = !_selectedItems.Contains(item);
                UpdateItemSelection(item, newState);
            }
            else if (isShiftPressed && _lastSelectedItem != null)
            {
                // Tab + clic => sélectionne tous les items entre le dernier sélectionné et le courant
                SelectRange(_lastSelectedItem, item);
            }

            _lastSelectedItem = item;
            //item.Focus();
            e.Handled = false;
        }

        private void TreeView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = VisualUpwardSearch<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;

            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift);

            // CTRL → toggle
            if (isCtrlPressed)
            {
                bool newState = !_selectedItems.Contains(item);
                UpdateItemSelection(item, newState);
            }
            // SHIFT → sélection en plage si possible
            else if (isShiftPressed && _lastSelectedItem != null)
            {
                SelectRange(_lastSelectedItem, item);
            }
            // Clic simple → sélection unique
            else
            {
                ClearSelection();
                UpdateItemSelection(item, true);
            }

            _lastSelectedItem = item;

        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null)
                return;

            // Ignorer si on clique sur un TextBox (édition de nom)
            var depObj = e.OriginalSource as DependencyObject;
            if (depObj != null && IsClickOnTextBox(depObj))
                return;


            // Vérifier le seuil de déplacement minimal pour éviter le drag parasite
            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;

            // Enregistrer le temps du dernier drag pour éviter les drops parasites
            _dragStartTime = DateTime.Now;
            //Debug.WriteLine($"Début drag : ({_dragStartTime} ms).");
            if (Math.Abs(diff.X) > 6 || Math.Abs(diff.Y) > 6)
            {
                //Debug.WriteLine($"Drag détecté : déplacement X={diff.X}, Y={diff.Y}, Node={(_draggedItem.Tag as NodesDefinition)?.NodeName}");

                // Démarrer le drag
                DragDrop.DoDragDrop(_draggedItem, _draggedItem.DataContext, DragDropEffects.Move);

                // Ne plus traiter cet item jusqu'au prochain clic
                _draggedItem = null;
            }
            else
            {
                // Debug si seuil non atteint
                //Debug.WriteLine($"Déplacement trop faible pour drag : X={diff.X}, Y={diff.Y}");
            }
        }

        private async void TreeView_Drop(object sender, DragEventArgs e)
        {
            // Délai entre le début du drag et le drop
            TimeSpan dragDuration = DateTime.Now - _dragStartTime;
            if (dragDuration < TimeSpan.FromMilliseconds(200)) // seuil minimum
            {
                //Debug.WriteLine($"Drop annulé : drag trop court ({dragDuration.TotalMilliseconds} ms).");
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            //Debug.WriteLine($"Drop ({dragDuration.TotalMilliseconds} ms).");

            if (_draggedItem == null)
                return;

            if (!e.Data.GetDataPresent(typeof(NodesDefinition)))
                return;

            var dropTargetItem = VisualUpwardSearch<TreeViewItem>((DependencyObject)e.OriginalSource);
            var dropTarget = dropTargetItem?.DataContext as NodesDefinition;

            if (dropTarget == null)
                return;

            // Récupérer la liste des nœuds à déplacer
            var nodesToMove = GetSelectedNodes(); // multi-sélection
            var draggedNode = e.Data.GetData(typeof(NodesDefinition)) as NodesDefinition;

            if (draggedNode != null && !nodesToMove.Contains(draggedNode))
                nodesToMove.Add(draggedNode);

            // Appel asynchrone à la VM pour déplacer tous les nœuds
            await ViewModel.MoveNodes(nodesToMove, _dropTargetNode, _insertAbove);

            // Optionnel : clear la sélection et la ligne d’insertion
            ClearSelection();
            ClearInsertionLine();
        }

        // Méthode utilitaire pour remonter l'arborescence
        private static T? VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
                source = VisualTreeHelper.GetParent(source);
            return source as T;
        }

        #region --- Insertion line ---
        private void TreeView_PreviewDragOver(object sender, DragEventArgs e)
        {

            var targetItem = VisualUpwardSearch<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (targetItem == null)
            {
                ClearInsertionLine();
                return;
            }

            var dropTarget = targetItem.DataContext as NodesDefinition;
            if (dropTarget == null)
            {
                ClearInsertionLine();
                return;
            }

            // Déterminer si l’insertion est au-dessus ou en dessous du centre du TreeViewItem
            var position = e.GetPosition(targetItem);
            bool insertAbove = position.Y < targetItem.ActualHeight / 2;

            ShowInsertionLine(targetItem, insertAbove);

            // Stocker temporairement pour le Drop
            _dropTargetNode = dropTarget;
            _insertAbove = position.Y < targetItem.ActualHeight / 2;

            //Debug.WriteLine($"_insertAbove = {insertAbove}, position.Y = {position.Y}, ActualHeight = {targetItem.ActualHeight}, {dropTarget.NodeName}");


            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void ShowInsertionLine(TreeViewItem target, bool insertAbove)
        {
            if (target == null) return;

            ClearInsertionLine();

            _adornerLayer = AdornerLayer.GetAdornerLayer(target);
            if (_adornerLayer == null) return;

            _insertionAdorner = new InsertionLineAdorner(target, insertAbove);
            _adornerLayer.Add(_insertionAdorner);
        }

        private void ClearInsertionLine()
        {
            if (_adornerLayer != null && _insertionAdorner != null)
            {
                _adornerLayer.Remove(_insertionAdorner);
                _insertionAdorner = null;
                _adornerLayer = null;
            }
        }
        #endregion

        #region Multi selection

        private void UpdateItemSelection(TreeViewItem item, bool isSelected)
        {
            if (item == null)
                return;

            // Mise à jour de l'état du nœud
            if (item.Tag is NodesDefinition node)
                node.IsSelected = isSelected;

            // Mise à jour collection des items sélectionnés
            if (isSelected)
            {
                if (!_selectedItems.Contains(item))
                    _selectedItems.Add(item);
            }
            else
            {
                _selectedItems.Remove(item);
            }

            //DebugSelectedItems();

            UpdateItemVisualState(item, isSelected);
        }

        private void RestoreOriginalBackground(TreeViewItem item, Border border)
        {
            Brush backgroundBrush = Brushes.Transparent;

            var node = item.Tag as NodesDefinition;
            if (node == null)
            {
                border.Background = backgroundBrush;
                border.BorderBrush = backgroundBrush;
                return;
            }

            border.Background = backgroundBrush;
            //border.CornerRadius = new CornerRadius(5);
            //border.BorderBrush = node.IsFixed ? Brushes.Beige : Brushes.Beige;
        }

        private void ClearSelection()
        {
            foreach (var item in _selectedItems.ToList())
                UpdateItemSelection(item, false);
            _selectedItems.Clear();
        }

        #endregion

        #endregion

        #region Helpers

        // Helper pour remonter l'arborescence visuelle et vérifier si on clique sur un TextBox
        //Utiliser pour le Rename car incompatible avec les PreviewMouseMove
        private static bool IsClickOnTextBox(DependencyObject source)
        {
            while (source != null)
            {
                if (source is TextBox)
                    return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }
        private static bool IsIgnoredElement(DependencyObject source)
        {
            int i = 0;
            // Ignore les parties "non cliquables" pour le drag (ex: expander, scrollbar)
            while (source != null)
            {

                //Debug.WriteLine($"{i}");
                i++;
                if (source is ToggleButton || source is ScrollBar)
                    return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugSelectedItems()
        {
            Debug.WriteLine("------ Sélection actuelle ------");

            foreach (var tvi in _selectedItems)
            {

                if (tvi.Tag is NodesDefinition node)
                    Debug.WriteLine($"• {node.NodeName} ({node.Type})");
                else
                    Debug.WriteLine($"• TreeViewItem sans node (Tag null?)");
            }

        }

        private Border? GetBorder(TreeViewItem item)
        {
            return FindChildByName<Border>(item, "PART_SelectionBorder");
        }

        private T? FindChildByName<T>(DependencyObject? parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed && child is FrameworkElement fe && fe.Name == name)
                    return typed;

                var result = FindChildByName<T>(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void UpdateItemVisualState(TreeViewItem item, bool isSelected)
        {
            var border = GetBorder(item);
            if (border == null)
                return;

            if (isSelected)
            {
                // Couleur bleutée
                border.Background = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
                border.BorderThickness = new Thickness(0);
                border.CornerRadius = new CornerRadius(5);
            }
            else
            {
                RestoreOriginalBackground(item, border);
                border.BorderThickness = new Thickness(0); // important pour éviter des artefacts
            }
        }

        private List<NodesDefinition> GetSelectedNodes()
        {  
            return _selectedItems
                .Where(i => i.Tag is NodesDefinition)
                .Select(i => (NodesDefinition)i.Tag)
                .ToList();
        }

        // Sélectionne tous les TreeViewItem entre start et end
        private void SelectRange(TreeViewItem start, TreeViewItem end)
        {
            var allItems = GetAllTreeViewItems(); // méthode à implémenter pour récupérer tous les items visibles
            int startIndex = allItems.IndexOf(start);
            int endIndex = allItems.IndexOf(end);

            if (startIndex == -1 || endIndex == -1) return;

            int min = Math.Min(startIndex, endIndex);
            int max = Math.Max(startIndex, endIndex);

            for (int i = min; i <= max; i++)
            {
                UpdateItemSelection(allItems[i], true);
            }
        }

        /// <summary>
        /// Récupère tous les TreeViewItem visibles dans le TreeView, dans l’ordre d’affichage.
        /// </summary>
        private List<TreeViewItem> GetAllTreeViewItems()
        {
            var items = new List<TreeViewItem>();

            foreach (var root in TreeViewControlV3.Items)
            {
                if (TreeViewControlV3.ItemContainerGenerator.ContainerFromItem(root) is TreeViewItem rootItem)
                {
                    AddTreeViewItemAndChildren(rootItem, items);
                }
            }

            return items;
        }

        /// <summary>
        /// Ajoute récursivement le TreeViewItem et tous ses enfants dans la liste.
        /// </summary>
        private void AddTreeViewItemAndChildren(TreeViewItem item, List<TreeViewItem> list)
        {
            list.Add(item);

            // S'assurer que les enfants sont générés
            if (!item.IsExpanded)
            {
                item.ApplyTemplate();
                item.UpdateLayout();
            }

            foreach (var child in item.Items)
            {
                if (item.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem childItem)
                {
                    AddTreeViewItemAndChildren(childItem, list);
                }
            }
        }

        #endregion


    }
}

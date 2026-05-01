using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Utilities;
using Toltech.App.ViewModels;
using Westermo.GraphX.Common.Exceptions;
using static Toltech.App.FrontEnd.Controls.Dashboard.BarChartControl;
using static Toltech.App.Models.NodesDefinition;


namespace Toltech.App.Views.Controls.TreeView
{
    public class TreeViewAreaV3ViewModel : INotifyPropertyChanged // TODO passé en BaseVm
    {
        private readonly MainViewModel _mainVM;
        private Func<Task> _treeUpdateAction;

        private readonly DomainService _domainService;

        private readonly TreeNodeService _treeService;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Propriétés exposées au XAML

        private ObservableCollection<NodesDefinition> _rootNodes = new ObservableCollection<NodesDefinition>();
        public ObservableCollection<NodesDefinition> RootNodes
        {
            get => _rootNodes;
            set => SetField(ref _rootNodes, value, nameof(RootNodes));
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetField(ref _isLoading, value, nameof(IsLoading));
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetField(ref _statusMessage, value, nameof(StatusMessage));
        }

        private NodesDefinition _selectedNode;
        public NodesDefinition SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnPropertyChanged(nameof(SelectedNode));
                    //Debug.WriteLine($"[TreeViewVM] SelectedNode changed to: {_selectedNode?.NodeName} - ID : {_selectedNode?.Id}");

                    // Ici vous pouvez aussi déclencher un filtrage automatique
                    //UpdateVisibleRequirements();
                }
            }
        }

        public bool IsEditing { get; set; }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region ICommand
        public ICommand RefreshCommand { get; }
        public ICommand AddSubFolderCommand { get; }
        public ICommand DeleteFolderCommand { get; }
        public ICommand RenameFolderCommand { get; }
        public ICommand GroupSelectionIntoSubFolderCommand { get; }
        public ICommand DeleteNodePartCommand { get; }
        public ICommand RenameNodeCommand { get; }
        public ICommand ShowNodePropertiesCommand { get; }
        public ICommand QuickAnalyzeCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand DesactiveNodePartCommand { get; }

        public RelayCommand CreateRequirementFromTreeCommand { get; }
        public ICommand DeleteRequirementFromTreeCommand { get; }

        public RelayCommand CreatePartFromTreeCommand { get; }

        // Indicateur interne pour éviter reentrancy
        private bool _isLoadingTreeView = false;

        #endregion

        #region Constructeur
        public TreeViewAreaV3ViewModel(MainViewModel mainVM, TreeNodeService treeNodeService)
        {
            _treeService = treeNodeService;
            _mainVM = mainVM;

            _domainService = _mainVM.DomainService;

            #region ICommand Initializations

            RefreshCommand = new RelayCommand(async _ => await LoadTreeViewDataAsync(), _ => !_isLoadingTreeView);
            AddSubFolderCommand = new RelayCommand<NodesDefinition>(AddSubFolderAsync);
            DeleteFolderCommand = new RelayCommand<NodesDefinition>(DeleteFolderAsync);
            GroupSelectionIntoSubFolderCommand = new RelayCommand<List<NodesDefinition>>(GroupSelectionIntoSubFolderAsync);
            RenameNodeCommand = new RelayCommand<NodesDefinition>(RenameNode);
            ShowNodePropertiesCommand = new RelayCommand<NodesDefinition>(ShowNodeProperties);
            QuickAnalyzeCommand = new RelayCommand<NodesDefinition>(QuickAnalyze);
            ExportDataCommand = new RelayCommand<NodesDefinition>(ExportData);

            #region Datas Commands via TreeView

            CreatePartFromTreeCommand = new RelayCommand(_ =>
            {
                if (_mainVM.DataVM?.CreatePartCommand?.CanExecute(null) == true)
                    _mainVM.DataVM.CreatePartCommand.Execute(null);
            });


            DeleteNodePartCommand = new RelayCommand<NodesDefinition>(DeleteNodePartAsync);

            #endregion

            #region Requirements Commands via TreeView

            CreateRequirementFromTreeCommand = new RelayCommand(async _ =>
            {
                if (_mainVM?.RequirementVM != null)
                    await _mainVM.RequirementVM.CreateRequirementAsync();
            });


            DeleteRequirementFromTreeCommand = new RelayCommand<NodesDefinition>(DeleteNodeReqAsync);

            #endregion

            #region Part Commands via TreeView

            DesactiveNodePartCommand = new RelayCommand<NodesDefinition>(node =>
            {
                _ = ToggleActiveNodeAsync(node);
            });

            #endregion

            _treeUpdateAction = () => _ = LoadTreeViewDataAsync();
            EventsManager.TreeViewUpdated += _treeUpdateAction;
            EventsManager.ModelOpen += _treeUpdateAction;
            //LoadTreeViewDataAsync();
            #endregion

        }


        #endregion

        #region Loader

        /// <summary>
        /// Méthode principale pour charger les données du TreeView.
        /// Réutilise vos sous-fonctions existantes :
        /// - DatabaseService.ActiveInstance.CreateTableIfNotExistsAsync<NodesDefinition>()
        /// - EnsureDefaultFoldersAsync()
        /// - BuildPartHierarchy(allNodes)
        /// </summary>
        public async Task LoadTreeViewDataAsync()
        {
            Debug.WriteLine("[TreeViewVM] - LoadTreeViewDataAsync()");

            if (_isLoadingTreeView) return;
            if (_isLoadingTreeView) return;
            _isLoadingTreeView = true;
            IsLoading = true;
            StatusMessage = "Chargement...";

            var globalWatch = Stopwatch.StartNew();

            try
            {
                // --- Étape 4 : Charger tous les noeuds depuis la DB  
                var allNodes = await _treeService.GetAllNodesAsync();

                if (!allNodes.Any(n => n.Type == NodeType.ModelFolder))
                {
                    allNodes = (await _treeService.EnsureDefaultFoldersAsync()).ToList();
                }


                // --- Étape 6 : Construire la hiérarchie (réutiliser BuildPartHierarchy)
                var rootNodesEnumerable = _treeService.BuildPartHierarchy(allNodes);

                // --- Étape 7 : Mettre à jour la collection liée (RootNodes) pour que le TreeView se mette à jour automatiquement
                RootNodes = new ObservableCollection<NodesDefinition>(rootNodesEnumerable);

                // --- Étape 8 : Statut final
                StatusMessage = $"Chargé : {RootNodes.Count} racines. ({globalWatch.ElapsedMilliseconds} ms)";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Chargement annulé.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Erreur lors du chargement.";
                Debug.WriteLine($"❌ Erreur LoadTreeViewDataAsync : {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isLoadingTreeView = false;
                IsLoading = false;
            }
        }


        #endregion

        #region Context Menu - Elementary Functions

        #region Add / Delete Sub-Folder
        private async void AddSubFolderAsync(NodesDefinition clickedNode)
        {
            if (clickedNode == null)
                return;

            await CreateFolderParentId(clickedNode.Id);
        }

        private async void GroupSelectionIntoSubFolderAsync(List<NodesDefinition> selectedNodes)
        {
            if (selectedNodes == null || selectedNodes.Count == 0)
                return;

            await CreateSubFolderWithNodes(selectedNodes);
        }

        // Crée un nouveau dossier et y déplace les nodes fournis.
        private async Task CreateSubFolderWithNodes(List<NodesDefinition> nodesToGroup)
        {
            if (nodesToGroup == null || nodesToGroup.Count == 0)
                return;

            // Déterminer le parent commun du nouveau dossier.
            // Ici, on prend le parent du premier node sélectionné.
            var firstNode = nodesToGroup[0];

            if (firstNode == null)
                return;

            // Crée un nouveau dossier sous le parent
            var newFolder = await CreateFolderParentId(firstNode.ParentId);

            if (newFolder == null)
                return;

            // Déplace tous les nodes sélectionnés dans le nouveau dossier
            foreach (var node in nodesToGroup)
            {
                // Mettre à jour le ParentId et ParentNode
                node.ParentId = newFolder.Id;
            }
            await _treeService.UpdateRangeAsync(nodesToGroup);

            // Facultatif : expand le nouveau dossier dans l'arbre
            newFolder.IsExpanded = true;
        }

        private async Task<NodesDefinition> CreateFolderParentId(int? parentId)
        {
            var folder = new NodesDefinition
            {
                NodeName = "New Folder",
                IsFolder = true,
                ParentId = parentId,
                Type = NodeType.Folder
            };

            await _treeService.InsertAsync(folder);

            return folder;
        }

        #region Delete Folder 

        /// <summary>
        /// Supprime un folder donné et remonte ses enfants vers le parent.
        /// </summary>
        private async void DeleteFolderAsync(NodesDefinition folder)
        {
            if (folder == null || !folder.IsFolder)
                return;

            await _treeService.DeleteFolderAndPromoteChildrenAsync(folder);

            // Notifie la mise à jour de l'arbre
            //EventsManager.RaiseNodesUpdated();
        }


        #endregion


        #endregion

        #region Rename Inline
        public async Task RenameNodeAsync(NodesDefinition node, string newName)
        {
            if (node == null)
                return;

            await _treeService.RenameNodeAsync(node, newName);
        }

        #endregion


        private async Task DeleteNodePartAsync(NodesDefinition node)
        {
            await _treeService.DeleteNodeAsync(node);
        }
        private async Task DeleteNodeReqAsync(NodesDefinition node)
        {
            await _treeService.DeleteNodeAsync(node);
        }
    
       
        public void PropagateSelectionToDataVM(NodesDefinition? node)
        {
            if (node == null)
                return;

            // Ne propager que pour les DataNode (ou autre type voulu)
            if (node.Type == NodeType.DataNode)
            {
                // Appel à la fonction de la DatasVM
                _mainVM.DataVM.SelectDataByNodeId(node.LinkedOriginalId);
            }
        }


        private void RenameNode(NodesDefinition node)
        {
            Debug.WriteLine($"RenameNode called for {node?.NodeName}");
        }

        private void ShowNodeProperties(NodesDefinition node)
        {
            Debug.WriteLine($"ShowNodeProperties called for {node?.NodeName}");
        }

        private void QuickAnalyze(NodesDefinition node)
        {
            Debug.WriteLine($"QuickAnalyze called for {node?.NodeName}");
        }

        private void ExportData(NodesDefinition node)
        {
            Debug.WriteLine($"ExportData called for {node?.NodeName}");
        }

        #endregion

        #region Gestion du Drop 

        public async Task MoveNodes(List<NodesDefinition>? nodes, NodesDefinition? dropTarget, bool insertAbove)
        {
          await _treeService.MoveNodesAsync(nodes, dropTarget, insertAbove);
        }

        #endregion

        #region Expanded Collapsed
        private bool _isUpdatingExpansion = false;

        public async Task OnNodeExpandedAsync(NodesDefinition node)
        {
            if (_isUpdatingExpansion)
                return;

            _isUpdatingExpansion = true;
            try
            {
                node.IsExpanded = true;
                await _treeService.UpdateAsync(node);
            }
            finally
            {
                _isUpdatingExpansion = false;
            }
        }

        public async Task OnNodeCollapsedAsync(NodesDefinition node)
        {
            if (_isUpdatingExpansion)
                return;

            _isUpdatingExpansion = true;
            try
            {
                node.IsExpanded = false;
                await _treeService.UpdateAsync(node);
            }
            finally
            {
                _isUpdatingExpansion = false;
            }
        }


        #endregion


        private async Task ToggleActiveNodeAsync(NodesDefinition node)
        {
            await _treeService.ToggleNodeActiveAsync(node);
        }

        private async Task RepairTreeIfNeededAsync()
        {
            await _treeService.RepairTreeIfNeededAsync();
        }

    }
}

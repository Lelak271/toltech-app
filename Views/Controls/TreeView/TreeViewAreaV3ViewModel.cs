using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Utilities;
using Toltech.App.ViewModels;
using static Toltech.App.Models.NodesDefinition;
using static Toltech.App.Services.EventsManager;
using static Toltech.App.Utilities.TreeNodeService;


namespace Toltech.App.Views.Controls.TreeView
{
    public class TreeViewAreaV3ViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;

        private readonly TreeNodeService _treeService;
        private readonly NodeSyncService _nodeSyncService;


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
                    OnPropertyChanged();
                }
            }
        }

        public void SetSelectedNode(NodesDefinition node)
        {
            if (_selectedNode == node) return;
            _selectedNode = node;
            OnPropertyChanged(nameof(SelectedNode));
        }

        private List<NodesDefinition> _selectedNodes = new();
        public IReadOnlyList<NodesDefinition> SelectedNodes => _selectedNodes;

        /// <summary>
        /// Synchronise la sélection multiple depuis le code-behind.
        /// Appelé après chaque changement de sélection visuelle.
        /// </summary>
        public void UpdateSelectedNodes(List<NodesDefinition> nodes)
        {
            _selectedNodes = nodes ?? new List<NodesDefinition>();
            OnPropertyChanged(nameof(SelectedNodes));
        }



        private NodesDefinition _doubleClickedNode;
        public NodesDefinition DoubleClickedNode
        {
            get => _doubleClickedNode;
            private set
            {
                _doubleClickedNode = value;
                OnPropertyChanged();
            }
        }

        public void SetDoubleClickedNode(NodesDefinition node)
        {
            if (_doubleClickedNode == node) return;
            _doubleClickedNode = node;
            OnPropertyChanged(nameof(DoubleClickedNode));
        }
        public record DragDropResult(
            IReadOnlyCollection<NodesDefinition> SourceNodes,
            NodesDefinition TargetNode)
        {
            /// <summary>
            /// Nœuds source de type <see cref="NodeType.RequirementNode"/> uniquement.
            /// </summary>
            public IReadOnlyCollection<NodesDefinition> RequirementSourceNodes =>
                SourceNodes
                    .Where(n => n.Type == NodeType.RequirementNode)
                    .ToList();

        }

        private DragDropResult _lastDragDrop;
        public DragDropResult LastDragDrop
        {
            get => _lastDragDrop;
            private set
            {
                _lastDragDrop = value;
                OnPropertyChanged();
            }
        }

        public void OnNodeDropped(IReadOnlyCollection<NodesDefinition> source, NodesDefinition target)
        {
            LastDragDrop = new DragDropResult(source, target);
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

        public ICommand CreateRequirementFromTreeCommand { get; }
        public ICommand DeleteRequirementFromTreeCommand { get; }

        public ICommand CreatePartFromTreeCommand { get; }

        // Indicateur interne pour éviter reentrancy
        private bool _isLoadingTreeView = false;

        #endregion

        #region Constructeur
        public TreeViewAreaV3ViewModel(MainViewModel mainVM, TreeNodeService treeNodeService, NodeSyncService nodeSyncService)
        {
            _treeService = treeNodeService;
            _nodeSyncService = nodeSyncService;
            _mainVM = mainVM;

            #region ICommand Initializations

            RefreshCommand = new AsyncRelayCommand(LoadTreeViewDataAsync, () => !_isLoadingTreeView);
            AddSubFolderCommand = new RelayCommand<NodesDefinition>(AddSubFolderAsync);
            DeleteFolderCommand = new RelayCommand<NodesDefinition>(DeleteFolderAsync);
            GroupSelectionIntoSubFolderCommand = new RelayCommand<List<NodesDefinition>>(GroupSelectionIntoSubFolderAsync);
            RenameNodeCommand = new RelayCommand<NodesDefinition>(RenameNode);
            ShowNodePropertiesCommand = new RelayCommand<NodesDefinition>(ShowNodeProperties);
            QuickAnalyzeCommand = new RelayCommand<NodesDefinition>(QuickAnalyze);
            ExportDataCommand = new RelayCommand<NodesDefinition>(ExportData);

            #region Datas Commands via TreeView

            CreatePartFromTreeCommand = new RelayCommand(() =>
            {
                if (_mainVM.DataVM?.CreatePartCommand?.CanExecute(null) == true)
                    _mainVM.DataVM.CreatePartCommand.Execute(null);
            });


            DeleteNodePartCommand = new AsyncRelayCommand<NodesDefinition>(DeleteNodeAsync);

            #endregion

            #region Requirements Commands via TreeView

            CreateRequirementFromTreeCommand = new AsyncRelayCommand(async () =>
            {
                if (_mainVM?.RequirementVM != null)
                    await _mainVM.RequirementVM.CreateRequirementAsync();
            });


            DeleteRequirementFromTreeCommand = new AsyncRelayCommand<NodesDefinition>(DeleteNodeAsync);

            #endregion

            #region Part Commands via TreeView

            DesactiveNodePartCommand = new RelayCommand<NodesDefinition>(node =>
            {
                _ = ToggleActiveNodeAsync(node);
            });

            #endregion


            #endregion

            #region EventsManager subscription

            _ = LoadTreeViewDataAsync();
            EventsManager.ModelOpened += OnModelOpened;
            EventsManager.PartCrud += OnPartCrudAsync;
            EventsManager.ModelDataCrud += OnModelDataCrudAsync;
            EventsManager.RequirementCrud += OnRequirementCrudAsync;

            #endregion

        }

        #endregion


        #region Events

        /// <summary>
        /// Ecoute les évènements et met à jour le treeview en conséquence via le TreeNodeService
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnPartCrudAsync(PartCrudEvent e)
        {
            switch (e.Operation)
            {
                case CrudOperation.Added:
                    await _nodeSyncService.SyncAddedAsync(e.Entity);
                    break;

                case CrudOperation.Deleted:
                    // Unitaire
                    if (e.EntityId != null)
                        await _nodeSyncService.SyncPartDeletedAsync(e.EntityId);

                    // Masse
                    if (e.EntityIds?.Any() == true)
                        await _nodeSyncService.SyncPartDeletedAsync(entityIds: e.EntityIds);
                    break;

                case CrudOperation.Updated:
                    // Unitaire
                    if (e.Entity != null)
                        await _nodeSyncService.SyncUpdatedAsync(e.Entity);

                    // Masse
                    if (e.Entities?.Any() == true)
                        await _nodeSyncService.SyncUpdatedAsync(entities: e.Entities);
                    break;
            }

            await LoadTreeViewDataAsync();
        }
        private async Task OnModelDataCrudAsync(ModelDataCrudEvent e)
        {
            switch (e.Operation)
            {
                case CrudOperation.Added:
                    if (e.Entities?.Any() == true)
                        await _nodeSyncService.SyncAddedAsync(entities: e.Entities);
                    else if (e.Entity != null)
                        await _nodeSyncService.SyncAddedAsync(e.Entity);
                    break;

                case CrudOperation.Deleted:
                    if (e.EntityIds?.Any() == true)
                        await _nodeSyncService.SyncModelDataDeletedAsync(entityIds: e.EntityIds);
                    else if (e.EntityId != null)
                        await _nodeSyncService.SyncModelDataDeletedAsync(e.EntityId);
                    break;

                case CrudOperation.Updated:
                    if (e.Entities?.Any() == true)
                        await _nodeSyncService.SyncUpdatedAsync(entities: e.Entities);
                    else if (e.Entity != null)
                        await _nodeSyncService.SyncUpdatedAsync(e.Entity);
                    break;
            }

            await LoadTreeViewDataAsync();
        }
        private async Task OnRequirementCrudAsync(RequirementCrudEvent e)
        {
            switch (e.Operation)
            {
                case CrudOperation.Added:
                    if (e.Entities?.Any() == true)
                        await _nodeSyncService.SyncAddedAsync(entities: e.Entities);
                    else if (e.Entity != null)
                        await _nodeSyncService.SyncAddedAsync(e.Entity);
                    break;

                case CrudOperation.Deleted:
                    if (e.EntityIds?.Any() == true)
                        await _nodeSyncService.SyncRequirementDeletedAsync(entityIds: e.EntityIds);
                    else if (e.EntityId != null)
                        await _nodeSyncService.SyncRequirementDeletedAsync(e.EntityId);
                    break;

                case CrudOperation.Updated:
                    if (e.Entities?.Any() == true)
                        await _nodeSyncService.SyncUpdatedAsync(entities: e.Entities);
                    else if (e.Entity != null)
                        await _nodeSyncService.SyncUpdatedAsync(e.Entity);
                    break;
            }

            await LoadTreeViewDataAsync();
        }

        private async Task OnModelOpened(ModelOpenedEvent e)
        {
            await LoadTreeViewDataAsync();
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
            await LoadTreeViewDataAsync();
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
            await LoadTreeViewDataAsync();
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
            await LoadTreeViewDataAsync();
            return folder;
        }

        #region Delete  

        /// <summary>
        /// Supprime un folder donné et remonte ses enfants vers le parent.
        /// </summary>
        private async void DeleteFolderAsync(NodesDefinition folder)
        {
            if (folder == null || !folder.IsFolder)
                return;

            await _treeService.DeleteFolderAndPromoteChildrenAsync(folder);
            await LoadTreeViewDataAsync();
        }

        private async Task DeleteNodeAsync(NodesDefinition node)
        {
            await _treeService.DeleteNodeAsync(node);
            await LoadTreeViewDataAsync();

            await EventsManager.RaiseNodeChangedAsync(new NodeChangedEvent
            {
                Type = node.Type,
                Operation = CrudOperation.Deleted,
                LinkedOriginalId = node.LinkedOriginalId,
                Source = EventSource.Tree
            });

        }
       
        #endregion


        #endregion

        #region Rename Inline

        // todo nul juste faire update
        public async Task<bool> RenameNodeAsync(NodesDefinition node, string newName)
        {
            if (node == null )
                return false;

            if (!NameValidationHelper.TryValidateName(newName, out string errorMessage))
                return false;

            await _treeService.RenameNodeAsync(node, newName);

            // Publie le nœud — pas de hit DB, pas de logique métier
            await EventsManager.RaiseNodeChangedAsync(new NodeChangedEvent
            {
                Type = node.Type,
                Operation = CrudOperation.Updated,
                LinkedOriginalId = node.LinkedOriginalId,
                NewName = newName,
                Source = EventSource.Tree
            });
            return true;
        }

        /// <summary>
        /// Tente de renommer le nœud avec le nouveau nom.
        /// Met à jour IsEditing et NodeName si succès, annule sinon.
        /// </summary>
        public async Task<bool> CommitRenameAsync(NodesDefinition node, string newName)
        {
            if (node is null) return false;

            // Escape ou nom vide → annule sans appel DB
            if (string.IsNullOrWhiteSpace(newName) || newName == node.NodeName)
            {
                node.IsEditing = false;
                return false;
            }

            bool success = await RenameNodeAsync(node, newName);

            if (success)
            {
                node.NodeName = newName;
                node.IsEditing = false;
            }

            return success;
        }

        /// <summary>
        /// Annule le renommage et sort du mode édition.
        /// </summary>
        public void CancelRename(NodesDefinition node)
        {
            if (node is null) return;
            node.IsEditing = false;
        }

        #endregion



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
            bool canMove = await _treeService.MoveNodesAsync(nodes, dropTarget, insertAbove);

            if (canMove)
            {
                await LoadTreeViewDataAsync();

                await EventsManager.RaiseNodeChangedAsync(new NodeChangedEvent
                {
                    Type = dropTarget.Type,
                    Operation = CrudOperation.Move,
                    LinkedOriginalId = dropTarget.LinkedOriginalId,
                    NewName = dropTarget.NodeName,
                    Source = EventSource.Tree
                });

            }
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

        // TreeViewAreaV3ViewModel.cs
        public async Task HandleNodeDoubleClickAsync(NodesDefinition node)
        {
            SetSelectedNode(node);
            SetDoubleClickedNode(node);
        }

        /// <summary>
        /// Dans TreeViewViewModel — wrappe l'appel service
        /// </summary>
        public async Task<FolderRequirementsResult> GetFolderRequirementsAsync(NodesDefinition node)
            => await _treeService.GetFolderRequirementsAsync(node);
        public async Task<FolderRequirementsResult> GetRequirementsOfFolderAsync(NodesDefinition node)
            => await _treeService.GetRequirementsOfFolderAsync(node);




    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Toltech.App.Models;
using Toltech.App.Properties;
using Toltech.App.Services;
using Toltech.App.Services.Logging;
using Toltech.App.Services.Notification;
using Toltech.App.Utilities;
using Toltech.App.Views.Controls.TreeView;
using static Toltech.App.Models.NodesDefinition;
using static Toltech.App.Services.EventsManager;
using TtCore = Toltech.App.ViewModels;

namespace Toltech.App.ViewModels
{
    /// <summary>
    /// ViewModel dédié à la gestion des exigences ("Requirements").
    /// Encapsule la logique métier et les opérations CRUD associées.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ce ViewModel est responsable de la création, la suppression, la modification et
    /// le rechargement des exigences stockées en base SQLite via<see cref="DatabaseService"/>.
    /// </para>
    /// <para>
    /// Il est instancié et piloté par le <see cref="MainViewModel"/>,
    /// auquel il délègue l’affichage des notifications UI globales.
    /// </para>
    /// </remarks>
    public class RequirementsViewModel : BaseViewModel
    {
        #region Fields
        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;
        private readonly DomainService _domainService;
        public ObservableCollection<Part> Parts => MainVM.Parts;
        public TreeViewAreaV3ViewModel TreeVM { get; }
        private readonly INotificationService _notificationService;
        private readonly ILoggerService _logger;
        private readonly UiSettingsService _uiSettings;
        #endregion

        #region Collections
        public ObservableCollection<Requirements> Requirements { get; } = new();
        public ListCollectionView FilteredRequirements { get; }
        public ListCollectionView AllRequirements { get; }
        #endregion

        #region Commands
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveUniqueCommand { get; }
        public ICommand RemoveUniqueCommand { get; }
        public ICommand ClearPanelCommand { get; }
        public ICommand CreateRequirementCommand { get; }
        public ICommand DeleteRequirementCommand { get; }

        #endregion

        #region Properties
        public int NumberOfParts => MainVM.NumberOfParts;
        public int NumberOfReq => MainVM.NumberOfReq;

        private Requirements _selectedRequirement;
        public Requirements SelectedRequirement
        {
            get => _selectedRequirement;
            set
            {
                if (!Equals(_selectedRequirement, value))
                {
                    _selectedRequirement = value;
                    OnPropertyChanged(nameof(SelectedRequirement));
                    (RemoveUniqueCommand as TtCore.RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private NodesDefinition _currentFolderNode;
        public NodesDefinition CurrentFolderNode
        {
            get => _currentFolderNode;
            set
            {
                if (_currentFolderNode != value)
                {
                    _currentFolderNode = value;
                    OnPropertyChanged(nameof(CurrentFolderNode));
                }
            }
        }
        private string _currentFolder;
        public string CurrentFolder
        {
            get => _currentFolder;
            set
            {
                if (_currentFolder != value)
                {
                    _currentFolder = value;
                    OnPropertyChanged(nameof(CurrentFolder));
                }
            }
        }
      
        private int? _currentFolderId;
        public int? CurrentFolderId
        {
            get => _currentFolderId;
            set
            {
                if (_currentFolderId != value)
                {
                    _currentFolderId = value;
                    OnPropertyChanged(nameof(CurrentFolderId));
                }
            }
        }

        private bool _isCreating;
        public bool IsCreating
        {
            get => _isCreating;
            set
            {
                _isCreating = value;
                OnPropertyChanged();
            }
        }

        #region Eyes 
        private bool GetEye(string key) => _uiSettings.IsPanelExpanded(key);

        private void SetEye(string key, bool value, string rowHeightProperty = null, string eyeProperty = null)
        {
            if (_uiSettings.IsPanelExpanded(key) == value)
                return;

            _uiSettings.SetPanelExpanded(key, value);

            OnPropertyChanged(eyeProperty); // propriété Eye
            OnPropertyChanged(rowHeightProperty); // RowXHeight

            _ = _uiSettings.SaveAsync();
        }

        public bool IsEyeVisible1
        {
            get => GetEye("Req_Eye1");
            set => SetEye("Req_Eye1", value, nameof(Row1Height), nameof(IsEyeVisible1));
        }

        public bool IsEyeVisible2
        {
            get => GetEye("Req_Eye2");
            set => SetEye("Req_Eye2", value, nameof(Row2Height), nameof(IsEyeVisible2));
        }

        public bool IsEyeVisible3
        {
            get => GetEye("Req_Eye3");
            set => SetEye("Req_Eye3", value, nameof(Row3Height), nameof(IsEyeVisible3));
        }

        public bool IsEyeVisible4
        {
            get => GetEye("Req_Eye4");
            set => SetEye("Req_Eye4", value, nameof(Row4Height), nameof(IsEyeVisible4));
        }


        public GridLength Row1Height => IsEyeVisible1 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row2Height => IsEyeVisible2 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row3Height => IsEyeVisible3 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row4Height => IsEyeVisible4 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        #endregion

        #endregion

        #region Constructor
        public RequirementsViewModel(MainViewModel mainVM, DomainService domainService, TreeViewAreaV3ViewModel treeVM)
        {
            TreeVM = treeVM;

            _domainService = domainService;
            _mainVM = mainVM;
            _notificationService = App.NotificationService;

            _uiSettings = App.UiSettings;
            FilteredRequirements = new ListCollectionView(Requirements);
            FilteredRequirements.Filter = FilterRequirement;

            AllRequirements = new ListCollectionView(Requirements);

            #region Commandes
            LoadCommand = new TtCore.AsyncRelayCommand(() => LoadAsync());
            SaveCommand = new TtCore.AsyncRelayCommand(
                SaveAllReqAsync,
                () => Requirements.Any(r => r.IsDirty && !r.IsSaving)
            );

            // Commandes qui agissent sur un objet Requirements
            RemoveUniqueCommand = new TtCore.AsyncRelayCommand<Requirements>(RemoveUniqueAsync);
            SaveUniqueCommand = new TtCore.AsyncRelayCommand<Requirements>(SaveUniqueAsync);
            ClearPanelCommand = new TtCore.RelayCommand<Requirements>(ClearPanel);

            // Commandes sans paramètre
            CreateRequirementCommand = new TtCore.AsyncRelayCommand(CreateRequirementAsync, () => !IsCreating);
            DeleteRequirementCommand = new TtCore.AsyncRelayCommand(DeleteRequirementAsync);
            #endregion

            _mainVM.PropertyChanged += OnMainVMPropertyChanged;

            EventsManager.ModelOpened += OnModelOpenWrapper;

            EventsManager.NodeChanged += OnNodeChangedAsync;

            TreeVM.PropertyChanged += OnTreePropertyChanged;
        }


        #endregion

        #region Main Event Function

        // Chaque VM abonnée traduit selon son domaine
        private async Task OnNodeChangedAsync(NodeChangedEvent e)
        {
            switch (e.Type)
            {
                case NodeType.RequirementNode:
                    await OnRequirementCrudAsync(new RequirementCrudEvent
                    {
                        Operation = e.Operation,
                        Source = e.Source,
                        EntityId = e.LinkedOriginalId,
                        Entity = e.Operation == CrudOperation.Updated
                            ? new Requirements { Id_req = e.LinkedOriginalId, NameReq = e.NewName }
                            : null
                    });
                    break;


            }
        }

        private async Task OnRequirementCrudAsync(RequirementCrudEvent e)
        {
            switch (e.Operation)
            {
                case CrudOperation.Added:
                    await ReloadSafe();
                    break;

                case CrudOperation.Deleted:
                    var idsToInvalidate = e.EntityIds?.Any() == true
                        ? e.EntityIds
                        : e.EntityId > 0
                            ? new List<int> { e.EntityId }
                            : null;

                    if (idsToInvalidate is not null)
                        await RefreshFromNodeFolderAsync(_currentFolderNode);

                    ApplyFilterAndSort();
                    break;

                case CrudOperation.Updated:
                    var updatedReqs = e.Entities?.Any() == true
                        ? e.Entities
                        : e.Entity is not null
                            ? new List<Requirements> { e.Entity }
                            : null;

                    if (updatedReqs is null) break;

                    foreach (var updated in updatedReqs)
                    {
                        var existing = Requirements.FirstOrDefault(r => r.Id_req == updated.Id_req);
                        if (existing is null) continue;

                        if (updated.NameReq is not null)
                            existing.NameReq = updated.NameReq;
                    }

                    ApplyFilterAndSort();
                    break;
            }
        }


        /// <summary>
        /// Handles changes to the tree selection and applies filtering based on the selected requirement node. 
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing information about the property change.</param>
        private async void OnTreePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(TreeVM.DoubleClickedNode):
                        if (TreeVM.DoubleClickedNode?.Type == NodeType.RequirementNode)
                        {
                            await RefreshFromNodeReqAsync(TreeVM.DoubleClickedNode);
                        }
                        break;

                    case nameof(TreeVM.LastDragDrop):
                        var drop = TreeVM.LastDragDrop;
                        if (drop is null) return;

                        var drags = drop.RequirementSourceNodes;
                        if (!drags.Any()) return;

                        // Refresh uniquement si le dossier courant est concerné
                        bool targetIsCurrentFolder = drop.TargetNode?.ParentId == _currentFolderId;
                        bool dragFromCurrentFolder = drags.Any(n => n.ParentId == _currentFolderId);

                        await RefreshFromNodeFolderAsync(_currentFolderNode);

                        break;


                        //case nameof(TreeVM.LastNodeChanged):
                        //    await HandleNodeChangedAsync(TreeVM.LastNodeChanged);
                        //    break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ReqVM] OnTreePropertyChanged : {ex.Message}");
            }
        }
        private async Task RefreshFromNodeFolderAsync(NodesDefinition folderNode)
        {
            var result = await TreeVM.GetRequirementsOfFolderAsync(folderNode);
            if (result is null) return;

            _treeFilterIds = new HashSet<int>(result.RequirementIds);
            CurrentFolderNode = result.FolderNode;
            CurrentFolder = CurrentFolderNode.NodeName;
            CurrentFolderId = CurrentFolderNode.Id;
            ApplyFilterAndSort();
        }
        private async Task RefreshFromNodeReqAsync(NodesDefinition nodeReq)
        {
            var result = await TreeVM.GetFolderRequirementsAsync(nodeReq);
            if (result is null) return;

            _treeFilterIds = new HashSet<int>(result.RequirementIds);
            CurrentFolderNode = result.FolderNode;
            CurrentFolder = CurrentFolderNode.NodeName;
            CurrentFolderId = CurrentFolderNode.Id;
            ApplyFilterAndSort();
        }

        private async Task OnModelOpenWrapper(ModelOpenedEvent e)
        {
            RestoreCache();
            await ReloadSafe();
        }

        private void OnMainVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainVM.NumberOfParts) ||
                e.PropertyName == nameof(MainVM.NumberOfReq))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }

        #endregion

        #region Filter View

        private HashSet<int>? _treeFilterIds = new(); // entrée TreeView
        private bool HasTreeFilter => _treeFilterIds is not null;
        private bool ContainsReq(int id) => _treeFilterIds?.Contains(id) ?? false;
        public enum RequirementViewMode { All, TreeOnly, SearchOnly }

        public IReadOnlyList<RequirementViewMode> ViewModes { get; }
            = Enum.GetValues(typeof(RequirementViewMode))
                  .Cast<RequirementViewMode>()
                  .ToList();

        private RequirementViewMode _viewMode = RequirementViewMode.TreeOnly;
        public RequirementViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode == value) return;

                _viewMode = value;
                OnPropertyChanged();
                ApplyFilterAndSort();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;

                _searchText = value;
                OnPropertyChanged();
                ApplyFilterAndSort();
            }
        }

        private bool FilterRequirement(object obj)
            => obj is Requirements req && ComputeVisibility(req);

        private bool ComputeVisibility(Requirements req) => ViewMode switch
        {
            RequirementViewMode.All => true,

            RequirementViewMode.TreeOnly =>
                !HasTreeFilter || ContainsReq(req.Id_req),

            RequirementViewMode.SearchOnly =>
                string.IsNullOrWhiteSpace(_searchText)
                || (req.NameReq?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (req.Commentaire?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false),

            _ => true
        };

        private void ApplyFilterAndSort()
        {
            using (FilteredRequirements.DeferRefresh())
            {
                FilteredRequirements.Filter = FilterRequirement;
                FilteredRequirements.CustomSort = HasTreeFilter
                    ? new RequirementIdOrderComparer(_treeFilterIds!)
                    : null;
            }
        }

        #endregion

        #region Load UI

        private CancellationTokenSource? _reloadCts;
        private List<Requirements> _cache = new();

        private async Task ReloadSafe()
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(100, _reloadCts.Token);
                await LoadAsync(_reloadCts.Token);
            }
            catch (TaskCanceledException) { }
        }

        public async Task LoadAsync(CancellationToken token = default)
        {
            Debug.WriteLine("[RequirementsViewModel] - LoadAsync()");

            token.ThrowIfCancellationRequested();

            var loadResult = await _domainService.LoadAllRequirementsAsync();
            if (loadResult.IsFailure)
            {
                HandleError(loadResult);
                return;
            }
            token.ThrowIfCancellationRequested();

            _cache = loadResult.Value;

            token.ThrowIfCancellationRequested();

            _treeFilterIds = new HashSet<int>(); // reset filtre arbre

            await SyncCollectionAsync(_cache);

            ApplyFilterAndSort();
        }

        public async Task AddItemAsync(Requirements newItem)
        {
            _cache.Add(newItem);
            _treeFilterIds.Add(newItem.Id_req); // Ajout à la vue courante
            await SyncCollectionAsync(_cache);
            ApplyFilterAndSort();
        }

        public async Task RemoveItemAsync(Requirements item)
        {
            int previousIndex = _cache.IndexOf(item);
            if (previousIndex == -1) return;

            _cache.RemoveAt(previousIndex);
            await SyncCollectionAsync(_cache);
            ApplyFilterAndSort();
        }

        private async Task SyncCollectionAsync(List<Requirements> source)
        {
            var dupes = Requirements.GroupBy(r => r.Id_req).Where(g => g.Count() > 1);
            if (dupes.Any()) Debug.WriteLine($"[DUPE] {string.Join(", ", dupes.Select(g => g.Key))}");

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Mise à jour ou insertion
                for (int i = 0; i < source.Count; i++)
                {
                    var incoming = source[i];

                    // Chercher par Id, pas par référence
                    int current = -1;
                    for (int j = 0; j < Requirements.Count; j++)
                    {
                        if (Requirements[j].Id_req == incoming.Id_req)
                        {
                            current = j;
                            break;
                        }
                    }

                    if (current == -1)
                    {
                        // Nouvel élément → insérer
                        Requirements.Insert(i, incoming);
                    }
                    else
                    {
                        // Élément existant → mettre à jour ses propriétés
                        // pour garder le même objet (et donc les bindings)
                        Requirements[current].LoadFromDb(incoming);

                        if (current != i)
                            Requirements.Move(current, i);
                    }
                }

                // Supprimer les éléments absents de source
                var sourceIds = new HashSet<int>(source.Select(r => r.Id_req));
                for (int i = Requirements.Count - 1; i >= 0; i--)
                    if (!sourceIds.Contains(Requirements[i].Id_req))
                        Requirements.RemoveAt(i);
            });

        }

        public void RestoreCache()
        {
            _cache.Clear();
        }
        public bool HasUnsavedChanges()
            => _cache.Any(r => r.IsDirty);

        #endregion

        #region CRUD Helpers
        public async Task DeleteRequirementAsync()
        {
            MessageBox.Show("Fonction à créer");

        }

        public async Task SaveAllReqAsync()
        {
            Debug.WriteLine("RequirementsViewModel - SaveAsync()");
            var toSave = Requirements.Where(r => r.IsDirty).ToList();
            if (toSave.Count == 0) return;

            var saveResResult = await _domainService.SaveRequirementsAsync(toSave);
            if (saveResResult.IsFailure)
            {
                HandleError(saveResResult);
                return;
            }
            await SyncCollectionAsync(_cache);

            _ = _notificationService.ShowNotifAsync("Données sauvegardées pour les exigences.");

            await EventsManager.RaiseRequirementCrudAsync(new RequirementCrudEvent
            {
                Operation = CrudOperation.Updated,
                Entities = toSave,
            });

        }

        public async Task DeleteRequirementByIdAsync(int? idReq)
        {

            var reqToDelete = await _domainService.GetReqByIdAsync(idReq);
            if (reqToDelete.IsFailure)
            {
                HandleError(reqToDelete);
                return;
            }

            if (!_dialog.Confirm($"Voulez-vous supprimer l'exigence {reqToDelete.Value.NameReq}"))
                return;

            var deleteResult = await _domainService.DeleteRequirementByIdAsync(idReq);
            if (deleteResult.IsFailure)
            {
                HandleError(deleteResult);
                return;
            }
            await RemoveItemAsync(Requirements.FirstOrDefault(r => r.Id_req == idReq));

            await EventsManager.RaiseRequirementCrudAsync(new RequirementCrudEvent
            {
                Operation = CrudOperation.Deleted,
                EntityId = reqToDelete.Value.Id_req,
            });

        }

        // 
        public async Task CreateRequirementAsync()
        {
            Debug.WriteLine("[RequirementsViewModel] - CreateRequirementAsync()");

            if (IsCreating)
                return;

            try
            {
                IsCreating = true;

                var placeholder = new Requirements
                {
                    NameReq = "Req_temp",
                    IsActive = true
                };

                await AddItemAsync(placeholder);

                var uiModel = await _domainService.CreateRequirementAsync();

                if (uiModel.IsFailure)
                {
                    HandleError(uiModel);
                    return;
                }

                _treeFilterIds.Remove(0);

                placeholder.LoadFromDb(uiModel.Value);

                _treeFilterIds.Add(placeholder.Id_req);

                ApplyFilterAndSort();

                await EventsManager.RaiseRequirementCrudAsync(
                    new RequirementCrudEvent
                    {
                        Operation = CrudOperation.Added,
                        Entity = uiModel.Value,
                    });
            }
            finally
            {
                IsCreating = false;
            }
        }

        #region Panel Button Function
        public async Task SaveUniqueAsync(Requirements req)
        {
            Debug.WriteLine("RequirementsViewModel - SaveUniqueAsync(Requirements req)");

            var saveResult = await _domainService.SaveRequirementsAsync(new List<Requirements> { req });
            if (saveResult.IsFailure)
            {
                HandleError(saveResult);
                return;
            }
            _ = _notificationService.ShowNotifAsync($"Données sauvegardées pour l'exigence {req.NameReq}.");

            await EventsManager.RaiseRequirementCrudAsync(new RequirementCrudEvent
            {
                Operation = CrudOperation.Updated,
                Entity = req,
            });

        }

        private async Task RemoveUniqueAsync(Requirements reqToDelete)
        {
            bool confirm = _dialog.Confirm(
                $"Voulez-vous supprimer l'exigence '{reqToDelete.NameReq}' ?");

            if (!confirm)
                return;

            await RemoveItemAsync(reqToDelete);

            var removeResult = await _domainService.DeleteRequirementAsync(reqToDelete);
            if (removeResult.IsFailure)
            {
                await AddItemAsync(reqToDelete);
                HandleError(removeResult);
                return;
            }

            await EventsManager.RaiseRequirementCrudAsync(new RequirementCrudEvent
            {
                Operation = CrudOperation.Deleted,
                EntityId = reqToDelete.Id_req,
            });

        }

        private void ClearPanel(Requirements req)
        {
            Debug.WriteLine("RequirementsViewModel - ClearPanel(Requirements req)");

            if (req == null) return;
            req.NameReq = string.Empty;
            req.PartReq1Id = 0;
            req.PartReq2Id = 0;
            req.NameTolOri = string.Empty;
            req.NameTolExtre = string.Empty;
            req.Description1 = string.Empty;
            req.Description2 = string.Empty;
            req.tol2 = 0;
            req.CoordX = 0;
            req.CoordY = 0;
            req.CoordZ = 0;
            req.CoordU = 0;
            req.CoordV = 1;
            req.CoordW = 0;
            req.Commentaire = string.Empty;
        }

        #endregion

        #endregion

        #region CheckBox Handling
        //private void SubscribeToRequirement(Requirements req)
        //{
        //    req.PropertyChanged += (s, e) =>
        //    {
        //        if (e.PropertyName == nameof(req.CheckBox1) || e.PropertyName == nameof(req.CheckBox2))
        //        {
        //            var r = s as Requirements;
        //            if (r == null) return;
        //            _ = HandleCheckBoxChangedAsync(r);
        //        }
        //    };
        //}

        //private async Task HandleCheckBoxChangedAsync(Requirements r)
        //{
        //    if (r.CheckBox1)
        //    {
        //        var tolerance1 = await DatabaseService.ActiveInstance.GetTolerancesByIdAsync(r.Id_tol1);
        //        if (tolerance1 != null)
        //            r.SetCalculatedTol1(tolerance1.tolInt);
        //    }
        //    else
        //    {
        //        r.SetCalculatedTol1(r.tol1);
        //    }

        //    if (r.CheckBox2)
        //    {
        //        var tolerance2 = await DatabaseService.ActiveInstance.GetTolerancesByIdAsync(r.Id_tol2);
        //        if (tolerance2 != null)
        //            r.SetCalculatedTol2(tolerance2.tolInt);
        //    }
        //    else
        //    {
        //        r.SetCalculatedTol2(r.tol2);
        //    }
        //}
        #endregion


    }
}

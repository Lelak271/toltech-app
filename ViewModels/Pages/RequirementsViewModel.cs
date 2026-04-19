using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TOLTECH_APPLICATION.FrontEnd.Interfaces;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Properties;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Services.Dialog;
using TOLTECH_APPLICATION.Services.Logging;
using TOLTECH_APPLICATION.Utilities;
using TOLTECH_APPLICATION.Views.Controls.TreeView;
using TtCore = TOLTECH_APPLICATION.ViewModels;

namespace TOLTECH_APPLICATION.ViewModels
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
        private DomainService _domainService;


        public ObservableCollection<Part> Parts => MainVM.Parts;
        public TreeViewAreaV3ViewModel TreeVM { get; }

        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialog;
        private readonly ILoggerService _logger;

        private readonly UiSettingsService _uiSettings;
        #endregion

        #region Collections
        public ObservableCollection<Requirements> Requirements { get; } = new ObservableCollection<Requirements>();
        public ListCollectionView FilteredRequirements { get; }

        private HashSet<int> _visibleRequirementIds = new();
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

        #region Eyes 
        private bool GetEye(string key) => _uiSettings.IsPanelExpanded(key);

        private void SetEye(string key, bool value, string rowHeightProperty = null)
        {
            if (_uiSettings.IsPanelExpanded(key) == value)
                return;

            _uiSettings.SetPanelExpanded(key, value);

            OnPropertyChanged(); // propriété Eye
            OnPropertyChanged(rowHeightProperty); // RowXHeight

            _ = _uiSettings.SaveAsync();
        }

        public bool IsEyeVisible1
        {
            get => GetEye("Req_Eye1");
            set => SetEye("Req_Eye1", value, nameof(Row1Height));
        }

        public bool IsEyeVisible2
        {
            get => GetEye("Req_Eye2");
            set => SetEye("Req_Eye2", value, nameof(Row2Height));
        }

        public bool IsEyeVisible3
        {
            get => GetEye("Req_Eye3");
            set => SetEye("Req_Eye3", value, nameof(Row3Height));
        }

        public bool IsEyeVisible4
        {
            get => GetEye("Req_Eye4");
            set => SetEye("Req_Eye4", value, nameof(Row4Height));
        }


        public GridLength Row1Height => IsEyeVisible1 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row2Height => IsEyeVisible2 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row3Height => IsEyeVisible3 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row4Height => IsEyeVisible4 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        #endregion

        #endregion

        #region Constructor
        public RequirementsViewModel(MainViewModel mainVM, TreeViewAreaV3ViewModel treeVM)
        {
            _mainVM = mainVM;
            _domainService = mainVM.DomainService;   
            _dialog = App.DialogService;
            _logger = App.Logger;
            _notificationService = App.NotificationService;

            _uiSettings = App.UiSettings;
            FilteredRequirements = (ListCollectionView)CollectionViewSource.GetDefaultView(Requirements);
            //UpdateRequirementsOrder();
            FilteredRequirements.Filter = FilterRequirement;

            #region Commandes
            // Commandes avec paramètres async ou sans paramètres
            LoadCommand = new TtCore.RelayCommand(async _ => await LoadAsync(), _ => true);
            SaveCommand = new TtCore.RelayCommand(async _ => await SaveAllReqAsync(), _ => Requirements.Any());

            // Commandes qui agissent sur un objet Requirements (cast à l'intérieur)
            RemoveUniqueCommand = new TtCore.RelayCommand(async param =>
            {
                if (param is Requirements req)
                    await RemoveUniqueAsync(req);
            }, param => param is Requirements);

            SaveUniqueCommand = new TtCore.RelayCommand(async param =>
            {
                if (param is Requirements req)
                    await SaveUniqueAsync(req);
            }, param => param is Requirements);

            ClearPanelCommand = new TtCore.RelayCommand(param =>
            {
                if (param is Requirements req)
                    ClearPanel(req);
            }, param => param is Requirements);

            // Commandes sans paramètre
            //CreateRequirementCommand = new TtCore.RelayCommand(async _ => await CreateRequirementAsync());
            CreateRequirementCommand = new TtCore.RelayCommand(async _ => await CreateRequirementAsync());
            DeleteRequirementCommand = new TtCore.RelayCommand(async _ => await DeleteRequirementAsync());
            #endregion

            TreeVM = treeVM;
            AttachTreeVM();

        }
        #endregion

        #region Main Event Function

        /// <summary>
        /// Permet le raffraichissment du TreeView lors d'un Drag&Drop
        /// </summary>
        private void AttachTreeVM()
        {
            // Event pour le changement  de Folder et Drag & Drop
            EventsManager.RequirementSelectChanged += OnRequirementSelectChangedAsync;
        }
        #endregion

        #region Filter View

        private HashSet<int> _treeFilterIds = new(); // entrée TreeView

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
                !_treeFilterIds.Any() || _treeFilterIds.Contains(req.Id_req),

            RequirementViewMode.SearchOnly =>
                string.IsNullOrWhiteSpace(_searchText)
                || (req.NameReq?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (req.Commentaire?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false),

            _ => true
        };

        private void ApplyFilterAndSort(bool forceRefresh = false)
        {
            using (FilteredRequirements.DeferRefresh())
            {
                FilteredRequirements.Filter = FilterRequirement;

                FilteredRequirements.CustomSort = _treeFilterIds.Any()
                    ? new RequirementIdOrderComparer(_treeFilterIds)
                    : null;
            }
        }

        private async Task OnRequirementSelectChangedAsync(EventsManager.RequirementEvent e)
        {
            if (e == null)
                return;

            if (Requirements.Count == 0)
                await ReloadSafe();

            _treeFilterIds = e.RequirementIds is { Count: > 0 }
                ? new HashSet<int>(e.RequirementIds)
                : new HashSet<int>();

            CurrentFolder = e.NameParentFolder;

            if (_viewMode != RequirementViewMode.TreeOnly)
            {
                _viewMode = RequirementViewMode.TreeOnly;
                OnPropertyChanged(nameof(ViewMode));
            }

            ApplyFilterAndSort();
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

            var requirements = await _domainService.LoadAllRequirementsAsync();
            token.ThrowIfCancellationRequested();

            _cache = requirements;

            token.ThrowIfCancellationRequested();

            _treeFilterIds = new HashSet<int>(); // reset filtre arbre

            await SyncCollectionAsync(_cache);

            ApplyFilterAndSort(forceRefresh: true);

            (SaveCommand as TtCore.RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveUniqueCommand as TtCore.RelayCommand)?.RaiseCanExecuteChanged();
        }

        public async Task AddItemAsync(Requirements newItem)
        {
            _cache.Add(newItem);
            _treeFilterIds.Add(newItem.Id_req); // Ajout à la vue courante
            await SyncCollectionAsync(_cache);
            ApplyFilterAndSort(forceRefresh: true);
        }

        public async Task RemoveItemAsync(Requirements item)
        {
            int previousIndex = _cache.IndexOf(item);
            if (previousIndex == -1) return;

            _cache.RemoveAt(previousIndex);
            await SyncCollectionAsync(_cache);
            ApplyFilterAndSort(forceRefresh: true);
        }

        private async Task SyncCollectionAsync(List<Requirements> source)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                for (int i = 0; i < source.Count; i++)
                {
                    var item = source[i];
                    int current = Requirements.IndexOf(item);

                    if (current == -1)
                        Requirements.Insert(i, item);
                    else if (current != i)
                        Requirements.Move(current, i);
                }

                var sourceSet = new HashSet<Requirements>(source);

                for (int i = Requirements.Count - 1; i >= 0; i--)
                    if (!sourceSet.Contains(Requirements[i]))
                        Requirements.RemoveAt(i);
            });
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
            try
            {
                var toSave = Requirements.Where(r => r.IsDirty).ToList();
                if (toSave.Count == 0) return;

                await _domainService.SaveRequirementsAsync(toSave);
                _notificationService.ShowNotifAsync("Données sauvegardées pour les exigences.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur SaveAsync: {ex}");
                throw;
            }
            finally
            {
                (SaveCommand as TtCore.RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public async Task DeleteRequirementByIdAsync(int? idReq)
        {

            string nameReq = await _domainService.GetRequirementNameByIdAsync(idReq);

            if (!_dialog.Confirm($"Voulez-vous supprimer l'exigence {nameReq}"))
                return;

            await _domainService.DeleteRequirementByIdAsync(idReq);
        }

        // TBD utilité
        public async Task CreateRequirementAsync()
        {
            Debug.WriteLine("[RequirementsViewModel] - CreateRequirementAsync()");

            var uiModel = await _domainService.CreateRequirementAsync();

            if (uiModel == null)
                return;

            await AddItemAsync(uiModel);
        }

        #region Panel Button Function
        public async Task SaveUniqueAsync(Requirements req)
        {
            Debug.WriteLine("RequirementsViewModel - SaveUniqueAsync(Requirements req)");
            try
            {
                await _domainService.SaveRequirementsAsync(new List<Requirements> { req });
                _notificationService.ShowNotifAsync("Données sauvegardées pour l'exigence.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur SaveUniqueAsync: {ex}");
                throw;
            }
        }

        private async Task RemoveUniqueAsync(Requirements req)
        {
            bool confirm = _dialog.Confirm(
                $"Voulez-vous supprimer l'exigence '{req.NameReq}' ?");

            if (!confirm)
                return;

            try
            {
                bool success = await _domainService.RemoveRequirementAsync(req);

                if (!success)
                    return;

                await RemoveItemAsync(req);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Erreur lors de la suppression de l'exigence {req.NameReq} ({req.Id_req})",
                    nameof(RequirementsViewModel),
                    ex);

                throw;
            }
            finally
            {
                (SaveCommand as TtCore.RelayCommand)?.RaiseCanExecuteChanged();
                (RemoveUniqueCommand as TtCore.RelayCommand)?.RaiseCanExecuteChanged();
            }
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

        #region Helpers

        #endregion

        #endregion


        public async Task ReverseActiveReqByIdAsync(int? idReq)
        {
            await _domainService.ReverseActiveReqByIdAsync(idReq);
        }

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

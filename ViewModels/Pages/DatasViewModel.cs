using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Input;
using TOLTECH_APPLICATION.FrontEnd.Controls;
using TOLTECH_APPLICATION.FrontEnd.Interfaces;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Properties;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Services.Dialog;
using TOLTECH_APPLICATION.Services.Logging;
using TOLTECH_APPLICATION.ToltechCalculation.Helpers;
using TOLTECH_APPLICATION.Utilities;
using TOLTECH_APPLICATION.Views.Controls.TreeView;
using static TOLTECH_APPLICATION.FrontEnd.Controls.TemplateCreateWindow;
using TtCore = TOLTECH_APPLICATION.ViewModels;

// TODO 
// Rajouter refresh lors de suppresion via la fentre DB part si cela supprimme la piece en cours UI
namespace TOLTECH_APPLICATION.ViewModels
{
    public class DatasViewModel : BaseViewModel
    {
        #region Fields

        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;

        public ObservableCollection<Part> Parts => MainVM.Parts;
        public Part ActivePart => Parts?.FirstOrDefault(p => p.Id == SelectedPartId);

        public TreeViewAreaV3ViewModel TreeVM { get; }

        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialog;
        private readonly ILoggerService _logger;

        private readonly DomainService _domainService;

        private readonly UiSettingsService _uiSettings;

        #endregion

        #region Collections
        public ObservableCollection<ModelData> Datas { get; } = new ObservableCollection<ModelData>();

        #endregion

        #region Commands
        public ICommand LoadCommand { get; }
        public ICommand CreatePartCommand { get; }
        public ICommand CreateDataCommand { get; }
        public ICommand ShowWindowDeletePartCommand { get; }
        public ICommand DeletePartCommand { get; }
        public ICommand CheckIsoPartCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand FocusDataByPartIdCommand { get; }


        // Command Lateral Buttons
        public ICommand DeletePanelCommand { get; }
        public ICommand ClearPanelCommand { get; }
        public ICommand SavePanelCommand { get; }
        #endregion

        #region Properties
        #region Eyes 
        private bool GetEye(string key) => _uiSettings.IsPanelExpanded(key);

        private void SetEye(string key, bool value, string rowHeightProperty = null)
        {
            if (_uiSettings.IsPanelExpanded(key) == value)
                return;

            _uiSettings.SetPanelExpanded(key, value);

            OnPropertyChanged();

            if (rowHeightProperty != null)
                OnPropertyChanged(rowHeightProperty);

            _ = _uiSettings.SaveAsync();
        }

        public bool IsEyeVisible1
        {
            get => GetEye("Data_Eye1");
            set => SetEye("Data_Eye1", value, nameof(Row1Height));
        }

        public bool IsEyeVisible2
        {
            get => GetEye("Data_Eye2");
            set => SetEye("Data_Eye2", value, nameof(Row2Height));
        }

        public bool IsEyeVisible3
        {
            get => GetEye("Data_Eye3");
            set => SetEye("Data_Eye3", value, nameof(Row3Height));
        }

        public bool IsEyeVisible4
        {
            get => GetEye("Data_Eye4");
            set => SetEye("Data_Eye4", value, nameof(Row4Height));
        }

        public bool IsEyeVisible5
        {
            get => GetEye("Data_Eye5");
            set => SetEye("Data_Eye5", value, nameof(Row5Height));
        }


        public GridLength Row1Height => IsEyeVisible1 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row2Height => IsEyeVisible2 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row3Height => IsEyeVisible3 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row4Height => IsEyeVisible4 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        public GridLength Row5Height => IsEyeVisible5 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        #endregion

        public int NbLiaisons => Datas?.Count ?? 0;

        // Stockage interne du Part fixé
        // Champ interne stockant le Part fixé
        private Part? _fixedPart;

        public Part? FixedPart
        {
            get => _fixedPart;
            set
            {
                if (_fixedPart == value)
                    return;

                _fixedPart = value;

                // Synchronisation modèle
                UpdateIsFixedFlags(_fixedPart);

                OnPropertyChanged(nameof(FixedPart));
                OnPropertyChanged(nameof(FixedPartId));

                if (_fixedPart != null)
                    _ = UpdateFixedPartAsync(_fixedPart);
            }
        }


        // Propriété pour SelectedValue binding dans la ComboBox
        public int FixedPartId
        {
            get => FixedPart?.Id ?? 0;
            set
            {
                if (FixedPart?.Id == value)
                    return;

                var part = Parts?.FirstOrDefault(p => p.Id == value);
                if (part != null)
                    FixedPart = part;
            }
        }

        #region Focus Items Data

        public event Action<ModelData>? RequestFocusItem;
        private void OnSelectedDataChanged()
        {
            if (SelectedData != null)
                RequestFocusItem?.Invoke(SelectedData);
        }

        /// <summary>
        /// Fonction publique pour demander le focus depuis un autre VM
        /// </summary>
        public void FocusItem(ModelData? data)
        {
            if (data == null)
                return;

            // Met à jour SelectedData pour propager l'événement
            SelectedData = data;

            // Déclenche l'événement explicitement
            RequestFocusItem?.Invoke(data);
        }
        public void SelectDataByNodeId(int linkedOriginalId)
        {
            var data = Datas.FirstOrDefault(d => d.Id == linkedOriginalId);
            if (data != null)
            {
                SelectedData = data;
                RequestFocusItem?.Invoke(data); // événement pour la vue
            }
        }

        // Stockage de la donnée sélectionnée pour focus
        private ModelData? _selectedData;
        public ModelData? SelectedData
        {
            get => _selectedData;
            set
            {
                if (SetProperty(ref _selectedData, value))
                {
                    OnSelectedDataChanged();
                    OnPropertyChanged(nameof(SelectedData)); // notifier à chaque changement
                }
            }
        }

        #endregion

        #region Select Active Part

        // Stockage de la Part sélectionnée
        private int? _selectedPartId;
        public int? SelectedPartId
        {
            get => _selectedPartId;
            set
            {
                if (SetProperty(ref _selectedPartId, value)) // déclenche OnPropertyChanged
                {
                    OnSelectedPartChanged();
                }

            }
        }

        // Méthode pour réagir au changement
        private async void OnSelectedPartChanged()
        {
            Debug.WriteLine($"Select part Id : {SelectedPartId}");
            OnPropertyChanged(nameof(ActivePart));
            await ReloadSafe(SelectedPartId);
        }

        #endregion

        #endregion

        #region Constructor
        public DatasViewModel(MainViewModel mainVM, TreeViewAreaV3ViewModel treeVM)
        {
            _mainVM = mainVM;
            _domainService=mainVM.DomainService;

            // TODO bof 
            if (_mainVM?.Parts != null)
            {
                _mainVM.Parts.CollectionChanged -= OnPartsCollectionChanged;
                _mainVM.Parts.CollectionChanged += OnPartsCollectionChanged;
            }

            _dialog = App.DialogService;
            _notificationService = App.NotificationService;
            _logger = App.Logger;

            _uiSettings = App.UiSettings;
            #region Command

            // Commandes avec paramètres async ou sans paramètres
            LoadCommand = new TtCore.RelayCommand(async _ => await ReloadSafe(), _ => true);

            CreatePartCommand = new TtCore.RelayCommand(async _ => await CreatePartAndDatas());
            //CreateDataCommand = new TtCore.RelayCommand(async _ => await CreateData());
            CreateDataCommand = new TtCore.RelayCommand(async param =>
            {
                if (param is int partId)
                {
                    await CreateData(partId);
                }
            });
            DeletePartCommand = new TtCore.RelayCommand(async _ => await DeletePartActive());
            ShowWindowDeletePartCommand = new TtCore.RelayCommand(async _ => await ShowWindowDeletePart());
            CheckIsoPartCommand = new TtCore.RelayCommand(async _ => await CheckIsoPart());
            SaveAllCommand = new TtCore.RelayCommand(async _ => await SaveAllActiveModelDataAsync());

            DeletePanelCommand = new TtCore.RelayCommand(async param =>
            {
                if (param is ModelData data)
                    await DeletePanelAsync(data);

            }, param => param is ModelData);

            ClearPanelCommand = new TtCore.RelayCommand(param =>
            {
                if (param is ModelData data)
                    ClearPanel(data);

            }, param => param is ModelData);

            SavePanelCommand = new TtCore.RelayCommand(async param =>
            {
                if (param is ModelData data)
                    await SavePanelAsync(data);

            }, param => param is ModelData);

            #endregion

            TreeVM = treeVM;

            #region Event Manager

            EventsManager.TreeViewDataNodeDrag += OnTreeChanged;
            EventsManager.PartSelectedChanged += OnPartSelectedChanged;
            EventsManager.ModelOpen += OnModelOpenWrapper;

            #endregion
        }

        #endregion

        #region Main Event Function
        ///Summary  
        /// Fonction pour définir la valeur actuelle de la CB après ouverture d'un modéle et changement de la liste des Parts
        ///Peut etre amelioré => âs mieux pour le moment 
        ///Summary  
        private async Task DefinedCBFixedPart()
        {
            var fixedPart = await _domainService.GetFixedPartAsync();

            if (fixedPart == null || Parts == null)
                return;

            var partInCollection = Parts.FirstOrDefault(p => p.Id == fixedPart.Id);

            if (partInCollection != null)
                FixedPart = partInCollection;
        }

        private async Task OnModelOpenWrapper()
        {
            RestoreCache();
            await ReloadSafe();
            await DefinedCBFixedPart();
        }

        private async Task OnTreeChanged()
        {
            await ReloadSafe();
        }

        private async Task OnPartSelectedChanged(int? idPart)
        {
            if (idPart != null)
                SelectedPartId = idPart;
        }
        private async void OnPartsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await DefinedCBFixedPart();
        }

        private void UpdateIsFixedFlags(Part? fixedPart)
        {
            if (Parts == null)
                return;

            foreach (var part in Parts)
                part.IsFixed = part == fixedPart;
        }

        #endregion

        #region Load UI

        private CancellationTokenSource _reloadCts;
        private async Task ReloadSafe(int? idPart = 0)
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(100, _reloadCts.Token);
                await LoadAsync( _reloadCts.Token, idPart);
            }
            catch (TaskCanceledException) { }
        }

        // Cache mémoire : PartId → liste des ModelData
        private readonly Dictionary<int, List<ModelData>> _cache = new();
        private readonly LinkedList<int> _lruOrder = new(); // ordre d'accès
        private const int MaxCachedParts = 10; // limite

        // ─── Chargement d'une Part (DB uniquement si pas déjà en cache) ───────
        public async Task LoadAsync(CancellationToken token, int? idPart = 0)
        {
            token.ThrowIfCancellationRequested();

            Debug.WriteLine("[DatasViewModel] - LoadAsync()");

            if (SelectedPartId == null)
            {
                Datas.Clear();
                return;
            }

            if (idPart == 0) idPart = SelectedPartId;

            int partId = idPart.Value;

            // Si déjà en cache → pas de DB, on affiche directement
            if (_cache.ContainsKey(partId))
            {
                TouchLru(partId);
                await ReorderFromDb(partId);
                FocusItem(SelectedData);
                return;
            }
            token.ThrowIfCancellationRequested();

            // Première visite de cette Part → chargement DB
            var dataList = await _domainService.LoadPartDataAsync(partId);
            token.ThrowIfCancellationRequested();

            if (!dataList.Any())
            {
                SelectedPartId = null;
                _cache[partId] = new List<ModelData>(); // cache vide pour éviter re-query
                Datas.Clear();
                return;
            }

            var sortedData = await _domainService.LoadSortedDataAsync(dataList, partId);
            token.ThrowIfCancellationRequested();

            var items = sortedData
                .Select(data =>
                {
                    var uiData = new ModelData();
                    uiData.LoadFromDb(data);
                    return uiData;
                })
                .ToList();
            token.ThrowIfCancellationRequested();

            _cache[partId] = items;
            TouchLru(partId);

            token.ThrowIfCancellationRequested();

            await ApplySortAndFilter(partId);
            FocusItem(SelectedData);

        }

        // ─── Réordonnancement en mémoire pour une Part ────────────────────────
        public async Task ReorderFromDb(int partId)
        {
            if (!_cache.ContainsKey(partId))
                return;

            _cache[partId] = (await _domainService.SortDatasAsync(_cache[partId], partId))
                .ToList();

            if (partId == SelectedPartId)
                await ApplySortAndFilter(partId);
        }

        // ─── Ajout d'un item dans une Part ────────────────────────────────────
        public async Task AddItem(ModelData newItem, int partId)
        {
            if (!_cache.ContainsKey(partId))
                _cache[partId] = new List<ModelData>();

            _cache[partId].Add(newItem);

            if (partId == SelectedPartId)
                await ReorderFromDb(partId);
        }

        // ─── Suppression d'un item ────────────────────────────────────────────
        public async Task RemoveItem(ModelData item, int partId)
        {
            if (!_cache.ContainsKey(partId)) return;

            _cache[partId].Remove(item);

            if (partId == SelectedPartId)
                await ApplySortAndFilter(partId);
        }

        // ─── Invalider le cache d'une Part (force reload DB au prochain Load) ─
        public void InvalidateCache(int partId)
        {
            _cache.Remove(partId);
            _lruOrder.Remove(partId);
        }
        public void RestoreCache()
        {
            _cache.Clear();
            _lruOrder.Clear();
        }

        // ─── Accesseurs utiles ────────────────────────────────────────────────
        public IReadOnlyList<ModelData>? GetCachedItems(int partId)
            => _cache.TryGetValue(partId, out var items) ? items : null;

        public bool HasUnsavedChanges(int partId)
            => _cache.TryGetValue(partId, out var items) && items.Any(d => d.IsDirty);

        public bool HasUnsavedChangesAny()
            => _cache.Values.Any(items => items.Any(d => d.IsDirty));

        // ─── Vue : synchronise Datas avec le cache de la Part affichée ────────
        private async Task ApplySortAndFilter(int partId)
        {
            Debug.WriteLine("ApplySortAndFilter");
            var source = _cache.TryGetValue(partId, out var items)
                ? items
                : new List<ModelData>();

           await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                for (int i = 0; i < source.Count; i++)
                {
                    var item = source[i];
                    int currentIndex = Datas.IndexOf(item);

                    if (currentIndex == -1)
                        Datas.Insert(i, item);
                    else if (currentIndex != i)
                        Datas.Move(currentIndex, i);
                }

                var sourceSet = new HashSet<ModelData>(source);

                for (int i = Datas.Count - 1; i >= 0; i--)
                    if (!sourceSet.Contains(Datas[i]))
                        Datas.RemoveAt(i);
            });
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="partId"></param>
        private void TouchLru(int partId)
        {
            _lruOrder.Remove(partId);
            _lruOrder.AddFirst(partId); // déplace en tête = plus récent

            // Éviction si limite dépassée
            while (_lruOrder.Count > MaxCachedParts)
            {
                var oldest = _lruOrder.Last.Value;

                // Ne pas évacter une Part avec des dirty items
                if (HasUnsavedChanges(oldest))
                {
                    // cherche le plus vieux sans dirty
                    var candidate = _lruOrder
                        .Reverse()
                        .FirstOrDefault(id => !HasUnsavedChanges(id));

                    if (candidate == default) break; // rien à évincer, on accepte le dépassement

                    _cache.Remove(candidate);
                    _lruOrder.Remove(candidate);
                }
                else
                {
                    _cache.Remove(oldest);
                    _lruOrder.RemoveLast();
                }
            }
        }

        #endregion

        #region Fonctions pour les boutons latéraux des PanelsModeler

        private void ClearPanel(ModelData data)
        {
            Debug.WriteLine("[DatasViewModel] - ClearPanel(ModelData data)");

            if (data == null) return;

            data.Model = string.Empty;
            //data.Extremite = string.Empty;
            /// test data.Origine = string.Empty;
            data.OriginePartId = 0;

            data.TolExtr = 0;
            data.TolInt = 0;
            data.TolOri = 0;

            data.CoordX = 0;
            data.CoordY = 0;
            data.CoordZ = 0;
            data.CoordU = 0;
            data.CoordV = 1;
            data.CoordW = 0;

            data.DescriptionTolExtre = string.Empty;
            data.DescriptionTolInt = string.Empty;
            data.DescriptionTolOri = string.Empty;

            data.Commentaire = string.Empty;

            data.NameTolExtre = string.Empty;
            data.NameTolInt = string.Empty;
            data.NameTolOri = string.Empty;

            data.IdTolExtre = 0;
            data.IdTolInt = 0;
            data.IdTolOri = 0;

            data.CheckBoxExtre = false;
            data.CheckBoxInt = false;
            data.CheckBoxOri = false;
        }

        private async Task DeletePanelAsync(ModelData data)
        {
            int partId = data.ExtremitePartId.Value;

            Debug.WriteLine("[DatasViewModel] - DeletePanelAsync(ModelData data)");

            if (data == null || data.Id <= 0)
                return;

            bool confirm = _dialog.Confirm($"Voulez-vous vraiment supprimer {data.Model} ?");

            if (!confirm)
                return;

            // 1. UI optimistic remove
            await RemoveItem(data, partId);

            try
            {
                // 2. logique métier centralisée
                bool success = await _domainService.DeleteDataAsync(data);

                if (!success)
                {
                    // rollback UI
                    await AddItem(data, partId);

                    _ = _notificationService.ShowNotifAsync(
                        $"Erreur lors de la suppression {data.Model}.",
                        true);
                }
            }
            catch (Exception ex)
            {
                // rollback UI
                await AddItem(data, partId);

                _logger.LogWarning("Erreur lors de la suppression", "todo");
            }
        }

        // Méthode privée commune
        private async Task SaveModelDataInternalAsync(List<ModelData> toSave)
        {
            await _domainService.SaveModelDataAsync(toSave);
        }

        // Save un seul item
        private async Task SavePanelAsync(ModelData data)
        {
            Debug.WriteLine("[DatasViewModel] - SavePanelAsync(ModelData data)");
            if (data.IsDirty != true)
                return;

            if (data == null || data.Id <= 0)
            {
                _dialog.Warning("L'identifiant est invalide ou vide.");
                return;
            }

            try
            {
                await SaveModelDataInternalAsync(new List<ModelData> { data });
                _= _notificationService.ShowNotifAsync($"Données sauvegardées pour la ponctuelle {data.Model}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur SavePanelAsync: {ex}");
                throw;
            }
        }

        // Save tous les dirty
        public async Task SaveAllActiveModelDataAsync()
        {
            Debug.WriteLine("[DatasViewModel] - SaveAllActiveModelDataAsync()");
            try
            {
                if (!ModelValidationHelper.CheckModelActif())
                    return;

                var toSave = Datas?.Where(d => d.IsDirty).ToList();
                if (toSave == null || toSave.Count == 0) return;

                await SaveModelDataInternalAsync(toSave);
                _notificationService.ShowNotifAsync("Données sauvegardées pour les ponctuelles.");
            }
            catch (Exception ex)
            {
                await _notificationService.ShowNotifAsync(
                    $"Erreur lors de la sauvegarde : {ex.Message}",
                    isError: true
                );
            }
        }

        #endregion

        #region CRUD Helper
        public async Task CreatePartAndDatas()
        {
            string nomPiece = "";

            var dlg = new TemplateCreateWindow(
                TemplateCreateWindowType.Part,
                null);

            if (dlg.ShowDialog() == true)
            {
                nomPiece = dlg.EnteredName;
            }

            if (string.IsNullOrWhiteSpace(nomPiece))
                return;

            await _domainService.CreatePartAndDatasAsync(nomPiece);
        }

        public async Task CreateData(int idPartActif)
        {
            if (!ModelValidationHelper.CheckModelActif(true))
                return;

            // 1. UI optimistic
            var placeholder = new ModelData
            {
                Model = "...",
                Active = true
            };

            await AddItem(placeholder, idPartActif);

            // 2. appel métier
            var result = await _domainService.CreateDataAsync(idPartActif);

            // 3. gestion résultat UI
            if (result == null)
            {
                await RemoveItem(placeholder, idPartActif);
                return;
            }

            // 4. hydration
            placeholder.LoadFromDb(result);
        }

        // Supprimer pièce du modèle TODO
        public async Task ShowWindowDeletePart()
        {
            if (!ModelValidationHelper.CheckModelActif(true))
                return;
            //var deleteWindow = new DeletePieceWindow();
            Debug.WriteLine("[DatasViewModel] - FAKE FUNCTION ShowWindowDeletePart( TODO )");
            //deleteWindow.ShowDialog(); // Fenêtre modale
        }
        public async Task DeletePartActive()
        {
            if (!SelectedPartId.HasValue)
            {
                Debug.WriteLine("Aucune Part sélectionnée pour la suppression.");
                return;
            }

            int partId = SelectedPartId.Value;

            // 1. récupérer le nom via domain (lecture autorisée)
            string? partName = await _domainService.GetPartNameByIdAsync(partId);

            if (string.IsNullOrWhiteSpace(partName))
                return;

            // 2. décision utilisateur (UI layer)
            bool confirmed = _dialog.Confirm(
                $"Voulez-vous supprimer la pièce '{partName}'?");

            if (!confirmed)
                return;

            // 3. exécution métier
            bool success = await _domainService.DeletePartByIdAsync(partId);

            if (!success)
                return;

            // 4. update UI state
            if (SelectedPartId == partId)
            {
                SelectedPartId = null;
            }
        }

        public async Task DeletePartById(int idPart)
        {
            bool success = await _domainService.DeletePartByIdAsync(idPart);

            if (!success)
                return;

            if (SelectedPartId == idPart)
            {
                SelectedPartId = null;
            }
        }


        // Appel de la IsoLocal
        private async Task CheckIsoPart()
        {
            if (!SelectedPartId.HasValue)
            {
                _dialog.Error("Pas de pièce sélectionnée");
                return;
            }

            await _domainService.CheckIsoPartAsync(SelectedPartId);
        }

        private async Task UpdateFixedPartAsync(Part part)
        {
            await _domainService.UpdateFixedPartAsync(part);
        }
        #endregion


    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Toltech.App.FrontEnd.Controls;
using Toltech.App.Models;
using Toltech.App.Properties;
using Toltech.App.Services;
using Toltech.App.Services.Notification;
using Toltech.App.Utilities;
using Toltech.App.Views.Controls.TreeView;
using static Toltech.App.FrontEnd.Controls.TemplateCreateWindow;
using static Toltech.App.Models.NodesDefinition;
using static Toltech.App.Services.EventsManager;
using TtCore = Toltech.App.ViewModels;

// TODO 
// Rajouter refresh lors de suppresion via la fentre DB part si cela supprimme la piece en cours UI
namespace Toltech.App.ViewModels
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

        public int NumberOfParts => MainVM.NumberOfParts;
        public int NumberOfReq => MainVM.NumberOfReq;
        public int NbLiaisons => Datas?.Count ?? 0;

        // Champ interne stockant le Part fixé
        private Part? _fixedPart;

        public Part? FixedPart
        {
            get => _mainVM.PartVM?.FixedPart;
            set
            {
                if (_mainVM.PartVM == null || _mainVM.PartVM.FixedPart == value) return;
                _mainVM.PartVM.FixedPart = value;
                UpdateIsFixedFlags(value);
                OnPropertyChanged(nameof(FixedPart));
                //OnPropertyChanged(nameof(FixedPartId));

                UpdateFixedPartAsync(FixedPart);
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
                    UpdateSelectedPartName();
                }

            }
        }

        private string _selectedPartName;
        public string SelectedPartName
        {
            get => _selectedPartName;
            set => SetProperty(ref _selectedPartName, value);
        }

        /// <summary>
        /// Fonction pour refresh l'interface si a lieu un changement de nom d'une Part
        /// </summary>
        private void UpdateSelectedPartName()
        {
            if (!SelectedPartId.HasValue)
            {
                SelectedPartName = string.Empty;
                return;
            }
            var part = Parts.FirstOrDefault(p => p.Id == SelectedPartId.Value);
            SelectedPartName = part?.NamePart ?? string.Empty;
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
            _domainService = mainVM.DomainService;

            _notificationService = App.NotificationService;

            _uiSettings = App.UiSettings;

            _mainVM.PropertyChanged += OnMainVMPropertyChanged;

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

            //EventsManager.TreeViewDataNodeDrag += OnTreeChanged;
            EventsManager.PartSelectedChanged += OnPartSelectedChanged;
            EventsManager.ModelOpen += OnModelOpenWrapper;
            EventsManager.NodeChanged += async e =>
            {
                OnNodeChangedAsync(e);
            };


            #endregion
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

        #region Main Event Function

        private Task OnNodeChangedAsync(NodeChangedEvent e)
        {
            switch (e.Type)
            {
                case NodeType.PartNode:
                    return OnPartCrudAsync(new PartCrudEvent
                    {
                        Operation = e.Operation,
                        Source = EventSource.Tree,
                        EntityId = e.LinkedOriginalId,
                        Entity = e.Operation == CrudOperation.Updated
                                    ? new Part { Id = e.LinkedOriginalId, NamePart = e.NewName }
                                    : null
                    });

                //case NodeType.DataNode:
                //    return OnModelDataCrudAsync(new ModelDataCrudEvent
                //    {
                //        Operation = e.Operation,
                //        Source = EventSource.Tree,
                //        EntityId = e.LinkedOriginalId,
                //        Entity = e.Operation == CrudOperation.Updated
                //                    ? new ModelData { Id = e.LinkedOriginalId, Model = e.NewName }
                //                    : null
                //    });

                default:
                    return Task.CompletedTask;
            }
        }

        private async Task OnPartCrudAsync(PartCrudEvent e)
        {
            //DefinedCBFixedPart();

            switch (e.Operation)
            {
                case CrudOperation.Added:
                    await ReloadSafe();
                    break;

                case CrudOperation.Deleted:
                    await ReloadSafe();
                    break;

                case CrudOperation.Updated:
                    var updatedParts = e.Entities?.Any() == true
                 ? e.Entities
                 : e.Entity != null ? new List<Part> { e.Entity } : null;

                    if (updatedParts?.Any(p => p.Id == SelectedPartId) == true)
                        UpdateSelectedPartName();
                    break;
            }

            //// Uniquement pour rafraîchir le nom affiché si la part sélectionnée a changé
            //if (e.Operation != CrudOperation.Updated) return Task.CompletedTask;

            //var updatedParts = e.Entities?.Any() == true
            //    ? e.Entities
            //    : e.Entity != null ? new List<Part> { e.Entity } : null;

            //if (updatedParts?.Any(p => p.Id == SelectedPartId) == true)
            //    UpdateSelectedPartName();

            //return Task.CompletedTask;
        }

        ///Summary  
        /// Fonction pour définir la valeur actuelle de la CB après ouverture d'un modéle et changement de la liste des Parts
        ///Peut etre amelioré => pas mieux pour le moment 
        ///Summary  
        private async Task DefinedCBFixedPart()
        {
            var fixedPart = await _domainService.GetFixedPartAsync();

            if (fixedPart.IsFailure || Parts == null || fixedPart.Value == null)
                return;

            var partInCollection = Parts.FirstOrDefault(p => p.Id == fixedPart.Value.Id);

            if (partInCollection != null)
                FixedPart = partInCollection;
        }

        private async Task OnModelOpenWrapper()
        {
            RestoreCache();
            await ReloadSafe();
            await DefinedCBFixedPart();
        }

        private async Task OnPartSelectedChanged(int? idPart)
        {
            //await DefinedCBFixedPart();
            if (idPart != null)
                SelectedPartId = idPart;
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
            // Capture de l'ancienne instance
            var previousCts = _reloadCts;

            // Création de la nouvelle instance
            _reloadCts = new CancellationTokenSource();

            // Annulation + libération de l'ancienne
            previousCts?.Cancel();
            previousCts?.Dispose();

            try
            {
                await Task.Delay(100, _reloadCts.Token);
                await LoadAsync(_reloadCts.Token, idPart);
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
            if (dataList.IsFailure || dataList.Value == null)
            {
                HandleError(dataList);
                return;
            }
            token.ThrowIfCancellationRequested();

            if (!dataList.Value.Any())
            {
                SelectedPartId = null;
                _cache[partId] = new List<ModelData>(); // cache vide pour éviter re-query
                Datas.Clear();
                return;
            }

            var sortedData = await _domainService.LoadSortedDataAsync(dataList.Value, partId);
            if (sortedData.IsFailure)
            {
                HandleError(sortedData);
                return;
            }
            token.ThrowIfCancellationRequested();

            var items = sortedData.Value
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
            var sortedDataResult = await _domainService.SortDatasAsync(_cache[partId], partId);
            if (sortedDataResult.IsFailure)
            {
                HandleError(sortedDataResult);
                return;
            }
            _cache[partId] = (sortedDataResult.Value)
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

            // 2. logique métier centralisée
            var deleteResult = await _domainService.DeleteDataAsync(data);
            if (deleteResult.IsFailure)
            {
                // rollback UI
                await AddItem(data, partId);

                _ = _notificationService.ShowNotifAsync(
                    $"Erreur lors de la suppression {data.Model}.",
                    true);
            }

            await EventsManager.RaiseModelDataCrudAsync(new ModelDataCrudEvent
            {
                Operation = CrudOperation.Deleted,
                EntityId = data.Id,
            });
        }

        // Méthode privée commune
        private async Task SaveModelDataInternalAsync(List<ModelData> toSave)
        {
            var saveResult = await _domainService.SaveModelDataAsync(toSave);
            if (saveResult.IsFailure)
            {
                HandleError(saveResult.Error); return;
            }

            await EventsManager.RaiseModelDataCrudAsync(new ModelDataCrudEvent
            {
                Operation = CrudOperation.Updated,
                Entities = toSave,
            });

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

            await SaveModelDataInternalAsync(new List<ModelData> { data });
            _ = _notificationService.ShowNotifAsync($"Données sauvegardées pour la ponctuelle {data.Model}.");


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

            var createResult = await _domainService.CreatePartAndDatasAsync(nomPiece);
            if (createResult.IsFailure)
            {
                HandleError(createResult);
                return;
            }

            await EventsManager.RaisePartCrudAsync(new PartCrudEvent
            {
                Operation = CrudOperation.Added,
                Entity = createResult.Value.Part
            });

            await EventsManager.RaiseModelDataCrudAsync(new ModelDataCrudEvent
            {
                Operation = CrudOperation.Added,
                Entities = createResult.Value.Datas  // liste complète des datas créées
            });

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
            if (result.IsFailure)
            {
                await RemoveItem(placeholder, idPartActif);
                HandleError(result);
                return;
            }

            // 4. hydration
            placeholder.LoadFromDb(result.Value);

            await EventsManager.RaiseModelDataCrudAsync(new ModelDataCrudEvent
            {
                Operation = CrudOperation.Added,
                Entity = result.Value,
                ParentId = idPartActif
            });

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
            var partName = await _domainService.GetPartNameByIdAsync(partId);
            if (partName.IsFailure)
            {
                HandleError(partName);
                return;
            }

            // 2. décision utilisateur (UI layer)
            bool confirmed = _dialog.Confirm(
                $"Voulez-vous supprimer la pièce '{partName.Value}'?");

            if (!confirmed)
                return;

            // 3. exécution métier
            var success = await _domainService.DeletePartWithDatasByIdAsync(partId);
            if (success.IsFailure)
            {
                HandleError(success);
                return;
            }

            // 4. update UI state
            if (SelectedPartId == partId)
            {
                SelectedPartId = null;
            }

            await EventsManager.RaisePartCrudAsync(new PartCrudEvent
            {
                Operation = CrudOperation.Deleted,
                EntityId = partId
            });

            await EventsManager.RaiseModelDataCrudAsync(new ModelDataCrudEvent
            {
                Operation = CrudOperation.Deleted,
                EntityIds = success.Value.Datas?.Select(d => d.Id).ToList()  // liste complète des datas supprimées
            });

        }

        public async Task DeletePartById(int idPart)
        {
            var success = await _domainService.DeletePartWithDatasByIdAsync(idPart);
            if (success.IsFailure)
            {
                HandleError(success);
                return;
            }

            if (SelectedPartId == idPart)
            {
                SelectedPartId = null;
            }

            await EventsManager.RaisePartCrudAsync(new PartCrudEvent
            {
                Operation = CrudOperation.Deleted,
                EntityId = idPart
            });

        }


        // Appel de la IsoLocal
        private async Task CheckIsoPart()
        {
            if (!SelectedPartId.HasValue)
            {
                _dialog.Error("Pas de pièce sélectionnée");
                return;
            }

            var checkResult = await _domainService.CheckIsoPartAsync(SelectedPartId);
            // 1. Erreur technique — la fonction a échoué
            if (checkResult.IsFailure)
            {
                HandleError(checkResult);
                return;
            }

            // 2. Erreur métier — la fonction a marché mais la pièce est invalide
            if (checkResult.Value.HasErrors)
            {
                var message = string.Join("\n\n", checkResult.Value.Errors);
                _dialog.Warning(message, Loc("Title_ValidationError"));
                return;
            }

            // 3. Succès — pièce valide
            _dialog.Info(checkResult.Value.SuccessMessage, Loc("Title_ValidationSuccess"));
        }

        private async Task UpdateFixedPartAsync(Part part)
        {
            var results = await _domainService.UpdateFixedPartAsync(part);
            if (results.IsFailure)
            {
                HandleError(results);
            }

            await EventsManager.RaisePartCrudAsync(new PartCrudEvent
            {
                Operation = CrudOperation.Updated,
                Entities = results.Value,
                Source = EventSource.Data
            });
        }

        #endregion


    }
}

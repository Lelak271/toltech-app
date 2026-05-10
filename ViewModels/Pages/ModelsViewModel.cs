using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.Win32;// Nécessaire pour OpenFileDialog
using Toltech.App.FrontEnd.Controls;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Services.Notification;
using Toltech.App.Utilities;
using static Toltech.App.FrontEnd.Controls.TemplateCreateWindow;
using static Toltech.App.Services.EventsManager;
using TtCore = Toltech.App.ViewModels;

namespace Toltech.App.ViewModels
{
    public class ModelsViewModel : BaseViewModel
    {
        #region Fields

        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM; // For Binding 

        private readonly INotificationService _notificationService;

        private readonly DomainService _domainService;

        private readonly Func<Task> _reloadAction;

        private RegisterModelWindow _registerModelWindow;

        #endregion

        #region Collections

        public ObservableCollection<ModelMeta> Models { get; } = new ObservableCollection<ModelMeta>();
        public ListCollectionView FilteredModels { get; }

        #endregion

        #region Commands

        public ICommand ToggleEditCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand OpenFromFileCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand DeleteActiveModelCommand { get; }
        public ICommand DuplicateActiveModelCommand { get; }
        public ICommand OpenRegisterWindowCommand { get; }

        #endregion

        #region Properties

        private PanelModelMeta? _currentEditablePanel;

        public PanelModelMeta? CurrentEditablePanel
        {
            get => _currentEditablePanel;
            set
            {
                if (_currentEditablePanel == value)
                    return;

                _currentEditablePanel?.SetEditable(false);
                _currentEditablePanel = value;
                _currentEditablePanel?.SetEditable(true);

                OnPropertyChanged(nameof(CurrentEditablePanel));
            }
        }


        private ModelMeta? _selectedModel;
        public ModelMeta? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value, nameof(SelectedModel)))
                {
                    OnPropertyChanged(nameof(ModelName));
                }
            }
        }

        private string? _pathModel;
        public string? PathModel
        {
            get => _pathModel;
            set
            {
                if (_pathModel != value)
                {
                    _pathModel = value;
                    OnPropertyChanged(nameof(PathModel));
                }
            }
        }

        private string? _modelName;
        public string? ModelName
        {
            get => _modelName;
            set
            {
                if (_modelName != value)
                {
                    _modelName = value;
                    OnPropertyChanged(nameof(ModelName));
                }
            }
        }

        #endregion

        #region Events
        private async Task OnModelOpened(ModelOpenedEvent e)
        {
            var model = _cache.FirstOrDefault(m => m.FilePathModel == e.Path);

            if (model == null)
            {
                await LoadAsync();
                model = _cache.FirstOrDefault(m => m.FilePathModel == e.Path);
            }

            if (model == null)
                return;

            SelectedModel = model;
            NumberOfParts = model.PartCount;
            NumberOfReq = model.RequirementCount;

            PathModel = model.FilePathModel;
            ModelName = model.NameData;
        }
        #endregion

        #region Constructor

        public ModelsViewModel(MainViewModel mainVM)
        {
            var vm = this;
            _mainVM = mainVM;
            _domainService = mainVM.DomainService;
             
            _notificationService = App.NotificationService;

            FilteredModels = new ListCollectionView(Models);
            FilteredModels.Filter = FilterModel;
            FilteredModels.SortDescriptions.Add(
                new SortDescription(nameof(ModelMeta.CreatedAtmodel), ListSortDirection.Descending));

             _ = ReloadSafe();

            _mainVM.MetaModelSyncService.MetaChanged += OnMetaChanged;

            #region EventManager

            //EventsManager.ModelOpen += _reloadAction;
            EventsManager.ModelOpened += OnModelOpened;
            #endregion

            #region Command
            ToggleEditCommand = new TtCore.RelayCommand<PanelModelMeta>(ToggleEdit);
            OpenCommand = new TtCore.RelayCommand<ModelMeta>(async (model) => await Open(model));
            OpenFromFileCommand = new TtCore.RelayCommand(async _ => await OpenFromFile());
            DuplicateCommand = new TtCore.RelayCommand<ModelMeta>(Duplicate);
            DeleteCommand = new TtCore.RelayCommand<ModelMeta>(Delete);

            CreateCommand = new TtCore.RelayCommand(async _ => await Create());
            DeleteActiveModelCommand = new TtCore.RelayCommand(async _ => await Delete());
            DuplicateActiveModelCommand = new TtCore.RelayCommand(async _ => await Duplicate());
            OpenRegisterWindowCommand = new TtCore.RelayCommand(async _ => OpenRegisterWindow(mainVM));
            #endregion
        }

        private void OpenRegisterWindow(MainViewModel mainVM)
        {
            _registerModelWindow = new RegisterModelWindow(mainVM);
            _registerModelWindow.ShowDialog();
        }

        /// <summary>
        /// Réagit aux changements de propriétés du MainViewModel.
        /// Permet de propager les notifications vers les propriétés locales dépendantes,
        /// afin de maintenir la synchronisation entre la VM centrale et cette ViewModel.
        /// </summary>
        #endregion


        #region Load UI

        private CancellationTokenSource? _reloadCts;
        private List<ModelMeta> _cache = new();

        private async Task ReloadSafe()
        {
            var newCts = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref _reloadCts, newCts);

            previous?.Cancel();
            previous?.Dispose();

            try
            {
                await Task.Delay(100, newCts.Token);
                await LoadAsync(newCts.Token);
            }
            catch (TaskCanceledException) { }
        }

        private async Task LoadAsync(CancellationToken token = default)
        {
            Debug.WriteLine("[ModelsViewModel] - LoadAsync()");

            token.ThrowIfCancellationRequested();

            var tempModels = await _domainService.LoadModelsAsync();

            if (tempModels.IsFailure)
            {
                _dialog.Error(tempModels.Error, "Chargement échoué");
                return;
            }

            token.ThrowIfCancellationRequested();

            // Stockage dans le cache
            _cache = tempModels.Value;

            // Sync optimisée vers UI
            await SyncCollectionAsync(_cache);

            // Rafraîchit la vue filtrée
            FilteredModels.Refresh();
        }

        private async Task SyncCollectionAsync(List<ModelMeta> source)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var sourceIds = source.Select(m => m.IdModel).ToHashSet();

                // Suppression des obsolètes
                for (int i = Models.Count - 1; i >= 0; i--)
                {
                    if (!sourceIds.Contains(Models[i].IdModel))
                        Models.RemoveAt(i);
                }

                // Ajout des nouveaux
                var existingIds = Models.Select(m => m.IdModel).ToHashSet();
                foreach (var item in source)
                {
                    if (!existingIds.Contains(item.IdModel))
                        Models.Add(item);
                }
            });
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
                FilteredModels.Refresh();
            }
        }


        private bool FilterModel(object obj)
        {
            if (obj is not ModelMeta model)
                return false;
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;
            return model.NameData?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true;
        }


        #endregion


        #region CRUD Helpers
        private async Task SaveModelAsync(ModelMeta meta)
        {
            var saveResult = await _domainService.SaveModelAsync(meta);
            if (saveResult.IsFailure)
            {
                HandleError(saveResult);
            }
        }

        private async Task ToggleEdit(PanelModelMeta panel)
        {
            if (panel == null)
                return;

            CurrentEditablePanel = CurrentEditablePanel == panel ? null : panel;

            
            if (CurrentEditablePanel == null && panel.DataContext is ModelMeta meta)
            {
                var saveResult = await _domainService.SaveModelAsync(meta);
                if (saveResult.IsFailure)
                {
                    HandleError(saveResult);
                }
            }

        }

        private async Task Open(ModelMeta model)
        {
            if (model == null)
                return;

            string path = model.FilePathModel;

            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.Exists(ModelManager.AppDataPath)
                    ? ModelManager.AppDataPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Fichier Toltech (*.tolx)|*.tolx",
                Title = "Ouvrir un modèle Toltech",
                DefaultExt = ".tolx",
                CheckFileExists = true,
                CheckPathExists = true
            };

            string selectedFile;

            if (File.Exists(path))
            {
                selectedFile = path;
            }
            else
            {
                if (openFileDialog.ShowDialog() != true)
                    return;

                selectedFile = openFileDialog.FileName;
            }

            var openResult = await _domainService.OpenModelAsync(selectedFile);
            if (openResult.IsFailure)
            {
                HandleError(openResult);
                return;
            }

            // => Registrer dans la bibliothéque

            var exists = await _domainService.IsExistModelRegisterAsync(selectedFile);
            if (exists.IsSuccess)
            {
                return;
            }

            bool confirmRegister = _dialog.Ask(
                "Ce modèle n'est pas présent. Voulez-vous l'ajouter ?",
                "Information");

            if (confirmRegister)
            {
                var registerResult = await _domainService.RegisterModelAsync(model.NameData, selectedFile);
                if (registerResult.IsFailure)
                {
                    HandleError(registerResult);
                }
            }
        }

        private async Task Delete(ModelMeta model)
        {
            if (model == null)
                return;

            var deleteResult = await _domainService.DeleteModelAsync(model.FilePathModel);

            if (deleteResult.IsFailure)
            {
                HandleError(deleteResult);
            }
        }

        private async Task Duplicate(ModelMeta model)
        {
            if (model == null)
                return;

            string path = model.FilePathModel;
            await DuplicateWithPath(path);
        }

        private async Task Create()
        {
            string modelName = string.Empty;

            var dlg = new TemplateCreateWindow(
                TemplateCreateWindowType.Model,
                null);

            if (dlg.ShowDialog() == true)
            {
                modelName = dlg.EnteredName?.Trim();
            }

            if (string.IsNullOrEmpty(modelName))
                return;

            var createResult = await _domainService.CreateModelAsync(modelName);

            if (createResult.IsFailure)
            {
                _dialog.Warning(createResult.Error,
                    "Création impossible");
            }
        }

        private async Task OpenFromFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Directory.Exists(ModelManager.AppDataPath)
                    ? ModelManager.AppDataPath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Fichier Toltech (*.tolx)|*.tolx",
                Title = "Ouvrir un modèle Toltech",
                DefaultExt = ".tolx",
                CheckFileExists = true,
                CheckPathExists = true
            };

            bool? confirm = openFileDialog.ShowDialog();

            if (confirm == true)
            {
                string selectedFile = openFileDialog.FileName;

                var openResult = await _domainService.OpenModelAsync(selectedFile);
                if (openResult.IsFailure)
                {
                    HandleError(openResult);
                }
            }
        }

        private async Task Delete(string path = "")
        {
            if (!ModelValidationHelper.CheckModelActif(false))
                return;

            path = ModelManager.ModelActif;
            string name = Path.GetFileNameWithoutExtension(path);

            // Demande de confirmation utilisateur
            var confirm = _dialog.Ask($"Voulez-vous vraiment supprimer le modèle actif '{name}' ?");

            if (!confirm)
                return;

            var deleteResult = await _domainService.DeleteModelAsync(path);
            if (deleteResult.IsFailure)
            {
                HandleError(deleteResult);
            }
        }

        private async Task Duplicate()
        {
            string path = ModelManager.ModelActif;
            if (path == null)
                return;

            await DuplicateWithPath(path);
        }

        private async Task DuplicateWithPath(string path)
        {
            string selectedPath = _dialog.OpenFolder("Select folder");

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(selectedPath))
                return;

            var duplicateResult = await _domainService.DuplicateModelAsync(path, selectedPath);

            if (duplicateResult.IsFailure)
                HandleError(duplicateResult);
        }

        #endregion



        #region Private helpers



        #endregion

        #region Number of Parts & Requirements

        private async void OnMetaChanged(ModelMetaChangedEvent e)
        {
            var model = Models.FirstOrDefault(m => m.IdModel == e.ModelId);
            if (model == null)
                return;

            if (e.PartCount.HasValue)
            {
                model.PartCount = e.PartCount.Value;
                NumberOfParts = e.PartCount.Value;
            }

            if (e.ReqCount.HasValue)
            {
                model.RequirementCount = e.ReqCount.Value;
                NumberOfReq = e.ReqCount.Value;
            }
        }

        private int _numberOfParts;
        public int NumberOfParts
        {
            get => _numberOfParts;
            set
            {
                if (_numberOfParts != value)
                {
                    _numberOfParts = value;
                    OnPropertyChanged(nameof(NumberOfParts));
                }
            }
        }

        private int _numberOfReq;
        public int NumberOfReq
        {
            get => _numberOfReq;
            set
            {
                if (_numberOfReq != value)
                {
                    _numberOfReq = value;
                    OnPropertyChanged(nameof(NumberOfReq));
                }
            }
        }

        #endregion

    }
}

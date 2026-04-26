using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;// Nécessaire pour OpenFileDialog
using Toltech.App.FrontEnd.Controls;
using Toltech.App.Services.Notification;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Services.Logging;
using Toltech.App.Utilities;
using static Toltech.App.FrontEnd.Controls.TemplateCreateWindow;
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

        public string? PathModel => _mainVM.ModelActif;


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
            }
        }

        #endregion

        #region Constructor

        public ModelsViewModel(MainViewModel mainVM)
        {
            var vm = this;
            _mainVM = mainVM;
            _domainService = mainVM.DomainService;

            _mainVM.PropertyChanged += MainVM_PropertyChanged;

            _notificationService = App.NotificationService;

            FilteredModels = new ListCollectionView(Models);
            FilteredModels.Filter = FilterModel;

            _reloadAction = () => _ = ReloadSafe();

            #region EventManager

            EventsManager.ModelOpen += _reloadAction;

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
        private void MainVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_mainVM.ModelActif))
            {
                OnPropertyChanged(nameof(PathModel));
            }
        }

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

            // ✔ Stockage dans le cache
            _cache = tempModels.Value;

            // ✔ Sync optimisée vers UI
            await SyncCollectionAsync(_cache);

            // ✔ Rafraîchit la vue filtrée
            ApplyFilter();
        }
        private async Task SyncCollectionAsync(List<ModelMeta> source)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                for (int i = 0; i < source.Count; i++)
                {
                    var item = source[i];
                    int current = Models.IndexOf(item);

                    if (current == -1)
                        Models.Insert(i, item);
                    else if (current != i)
                        Models.Move(current, i);
                }

                var sourceSet = new HashSet<ModelMeta>(source);

                for (int i = Models.Count - 1; i >= 0; i--)
                    if (!sourceSet.Contains(Models[i]))
                        Models.RemoveAt(i);
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
                ApplyFilter();
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

        private void ApplyFilter()
        {
            using (FilteredModels.DeferRefresh())
            {
                FilteredModels.Filter = FilterModel;
            }
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
                var saveResult =await _domainService.SaveModelAsync(meta);
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
                var registerResult = await _domainService.RegisterModelAsync(selectedFile);
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
                // TODO: Refactorer vers un Result<T> générique afin de standardiser la gestion des erreurs et des succès retournés par le DomainService, 
                // et permettre une communication explicite des causes d’échec (validation, doublon, IO, DB, etc.) sans utiliser de bools.
                _dialog.Warning("Un modèle portant ce nom existe déjà.",
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

            if (confirm==true)
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

        private async Task RenameModelFileIfNeededAsync(
            ModelMeta dbModel,
            ModelMeta currentModel)
        {
            await _domainService.RenameModelFileIfNeededAsync(dbModel, currentModel);
        }

        #endregion



    }
}

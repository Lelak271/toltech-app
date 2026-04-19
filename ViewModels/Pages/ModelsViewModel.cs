using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;// Nécessaire pour OpenFileDialog
using TOLTECH_APPLICATION.FrontEnd.Controls;
using TOLTECH_APPLICATION.FrontEnd.Interfaces;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Services.Dialog;
using TOLTECH_APPLICATION.Services.Logging;
using TOLTECH_APPLICATION.Utilities;
using static TOLTECH_APPLICATION.FrontEnd.Controls.TemplateCreateWindow;
using TtCore = TOLTECH_APPLICATION.ViewModels;

namespace TOLTECH_APPLICATION.ViewModels
{
    public class ModelsViewModel : BaseViewModel
    {
        #region Fields

        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM; // For Binding 

        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialog;
        private readonly ILoggerService _logger;

        private readonly DomainService _domainService;

        private readonly Func<Task> _reloadAction;

        private RegisterModelWindow _registerModelWindow;

        #endregion

        #region Collections

        public ObservableCollection<ModelMeta> Models { get; }
            = new ObservableCollection<ModelMeta>();

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

            _dialog = App.DialogService;
            _notificationService = App.NotificationService;
            _logger = App.Logger;

            _reloadAction = () => _ = LoadAsync();

            #region EventManager
            //EventsManager.ModelDelete += _reloadAction;
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
            OpenRegisterWindowCommand = new TtCore.RelayCommand(async _ => OpenRegisterWindow());
            #endregion
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


        private async Task LoadAsync()
        {
            Debug.WriteLine("[ModelsViewModel] - LoadAsync()");

            try
            {
                var tempModels = await _domainService.LoadModelsAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Models.Clear();

                    foreach (var model in tempModels)
                        Models.Add(model);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ModelsViewModel] - Erreur LoadAsync : {ex}");
            }
        }

        private async Task SaveModelAsync(ModelMeta meta)
        {
            await _domainService.SaveModelAsync(meta);
        }

        private async Task ToggleEdit(PanelModelMeta panel)
        {
            if (panel == null)
                return;

            CurrentEditablePanel = CurrentEditablePanel == panel ? null : panel;

            if (CurrentEditablePanel == null && panel.DataContext is ModelMeta meta)
            {
                await SaveModelAsync(meta);
            }

        }

        private async Task Open(ModelMeta model)
        {
            try
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

                string selectedFile = null;

                if (!File.Exists(path))
                {
                    var result = openFileDialog.ShowDialog();

                    if (result == true)
                        selectedFile = openFileDialog.FileName;
                    else
                        return;
                }
                else
                {
                    selectedFile = path;
                }

                await _domainService.OpenModelAsync(selectedFile);
            }
            catch (Exception ex)
            {
                _logger.LogError("Open failed", "", ex);
            }
        }

        private async Task Delete(ModelMeta model)
        {
            try
            {
                if (model == null)
                    return;

                await _domainService.DeleteModelAsync(model.FilePathModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Delete failed", "", ex);
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

            bool success = await _domainService.CreateModelAsync(modelName);

            if (!success)
            {
                // TODO: Refactorer vers un Result<T> générique afin de standardiser la gestion des erreurs et des succès retournés par le DomainService, 
                // et permettre une communication explicite des causes d’échec (validation, doublon, IO, DB, etc.) sans utiliser de bools.
                _dialog.Warning("Un modèle portant ce nom existe déjà.",
                    "Création impossible");

                return;
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

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string selectedFile = openFileDialog.FileName;

                // 👉 centralisation complète
                await _domainService.OpenModelAsync(selectedFile);
            }
        }

        private async Task Delete(string path = "")
        {
            path = ModelManager.ModelActif;
            string name = Path.GetFileNameWithoutExtension(path);

            // Demande de confirmation utilisateur
            var result = _dialog.Ask(
                $"Voulez-vous vraiment supprimer le modèle actif '{name}' ?");

            try
            {
                if (path == null || path == "")
                    return;

                await _domainService.DeleteModelAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogError("Delete failed", "", ex);
            }
        }

        private async Task Duplicate()
        {
            string path = ModelManager.ModelActif;
            if (path == null)
            {
                return;
            }
            await DuplicateWithPath(path);
        }

        private async Task DuplicateWithPath(string path)
        {
            try
            {
                string selectedPath = _dialog.OpenFolder("Select folder");

                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(selectedPath))
                    return;

                await _domainService.DuplicateModelAsync(path, selectedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError("Duplicate failed", "", ex);
            }
        }


        #region Private helpers

        private async Task RenameModelFileIfNeededAsync(
            ModelMeta dbModel,
            ModelMeta currentModel)
        {
            await _domainService.RenameModelFileIfNeededAsync(dbModel, currentModel);
        }

        #endregion

        private void OpenRegisterWindow()
        {
            _registerModelWindow = new RegisterModelWindow(this);
            _registerModelWindow.ShowDialog();
        }

        /// <summary>
        /// Fonction pour enregistrer un modèle dans la base de données meta.
        /// Crée un lien via le IdModel si il n'existe pas.
        /// </summary>
        /// <param name="modelPath"></param>
        /// <returns></returns>
        public async Task RegisterModelAsync(string modelPath = null)
        {
            Debug.WriteLine("[ModelsViewModel] - RegisterModelAsync");

            if (!ModelValidationHelper.CheckModelActif(true))
                return;

            bool success = await _domainService.RegisterModelAsync(modelPath);

            if (!success)
            {
                _dialog.Error("Enregistrement du modèle impossible.");
                return;
            }
        }

    }
}

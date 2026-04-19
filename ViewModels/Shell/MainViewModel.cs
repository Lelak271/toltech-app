using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.ToltechCalculation.Helpers;
using Toltech.App.ToltechCalculation.Resux;
using Toltech.App.Views.Controls.TreeView;
using Toltech.App.Visualisateur;
using  Toltech.ComputeEngine.Contracts;

namespace Toltech.App.ViewModels
{
    // <summary>
    /// ViewModel principal de l’application TOLTECH.
    /// Gère la navigation entre les pages, la centralisation des services,
    /// et la communication entre les différents sous-ViewModels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Le <see cref="MainViewModel"/> constitue le point d’entrée de la logique applicative :
    /// — il instancie les pages WPF et leurs ViewModels associés,
    /// - il gère l’état global et la navigation via<see cref="SelectedPage"/>,
    /// — il centralise les services partagés (base de données, notifications, sérialisation),
    /// et il fournit un accès hiérarchique aux sous-ViewModels(ex. <see cref = "RequirementsViewModel" />).
    /// </para>
    /// <para>
    /// Ce modèle respecte le pattern MVVM tout en permettant une communication descendante contrôlée
    /// entre le ViewModel principal et ses sous-modules via des références passées au constructeur.
    /// </para>
    /// </remarks>
    public partial class MainViewModel : INotifyPropertyChanged
    {
        #region Fields & Services

        private ResuxSerializer _resuxSerializer;

        public HomePageViewModel HomeVM { get; private set; }
        public DatasViewModel DataVM { get; private set; }
        public RequirementsViewModel RequirementVM { get; private set; }
        public PartDBViewModel PartVM { get; private set; }
        public ModelsViewModel ModelsVM { get; private set; }
        public ResultsViewModel ResultsVM { get; private set; }
        public TreeViewAreaV3ViewModel TreeViewViewModel { get; private set; }
        public LogViewerViewModel StatusBarVM { get; private set; }

        public DbModelService DbModelService { get; }
        public DomainService DomainService { get; }
        public DatabaseService DatabaseService { get; }

        public IComputeEngine ComputeEngine { get; }
        public ComputeValidationService ComputeValidationService { get; }


        #endregion

        #region Pages Instances & Navigation

        public VSTWindow PageVST { get; private set; }

        /// <summary>
        /// Page séléctionné des TabItems
        /// Séléction via ViewModel
        /// </summary>
        public object SelectedPage
        {
            get
            {
                switch (SelectedTabIndex)
                {
                    case 0:
                        return HomeVM;
                    case 1:
                        return ModelsVM;
                    case 2:
                        return DataVM;
                    case 3:
                        return RequirementVM;
                    case 4:
                        return ResultsVM;
                    case 5:
                        return ResultsVM;
                    case 6:
                        return ResultsVM;

                    default:
                        return ResultsVM;
                }
            }
        }


        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetField(ref _selectedTabIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedPage));

                    // Déclenche le chargement de la page associée
                    //_ = TryLoadAsync(SelectedPage);
                }
            }
        }

        #endregion

        #region Construction / Initialisation

        private object _currentNotification;
        public object CurrentNotification
        {
            get => _currentNotification;
            set => SetField(ref _currentNotification, value);
        }

        public MainViewModel(IComputeEngine computeEngine)
        {
            DatabaseService = new DatabaseService(""); // Instance Unique
            DbModelService = new DbModelService(); // Instance Unique

            ComputeEngine = computeEngine; // Instance Unique
            ComputeValidationService = new ComputeValidationService(ComputeEngine, DatabaseService); // Instance QUASI Unique => Page Resultats

            DomainService = new DomainService(
                DatabaseService,
                DbModelService,
                ComputeValidationService,
                App.Logger
                ); // Instance Unique

            LoadVM();
            LoadPages(); // TODO  obsole soon

            SubscribeToModelManagerEvents();
            NumbersPartReqChanged();

            _resuxSerializer = new ResuxSerializer();

            LoadToDoItems();

            StatusBarVM = new LogViewerViewModel(App.Logger);

        }

        private void LoadVM()
        {
            TreeViewViewModel = new TreeViewAreaV3ViewModel(this);
            RequirementVM = new RequirementsViewModel(this, TreeViewViewModel);
            PartVM = new PartDBViewModel(this);
            DataVM = new DatasViewModel(this, TreeViewViewModel);
            HomeVM = new HomePageViewModel();
            ModelsVM = new ModelsViewModel(this);
            ResultsVM = new ResultsViewModel(this);

        }
        private void LoadPages()
        {
            Debug.WriteLine("[MainViewModel] - LoadPages() ancienne fonction enelver la gestion de V3D");
            PageVST = new VSTWindow();
        }


        #endregion

        #region Modèle actif / Pièce active

        private string _modelActif;
        public string ModelActif
        {
            get => string.IsNullOrEmpty(_modelActif) ? "Aucun" : _modelActif;
            set
            {
                if (_modelActif != value)
                {
                    _modelActif = value;
                    OnPropertyChanged(nameof(ModelActif));
                    OnPropertyChanged(nameof(ModelName));
                }
            }
        }

        public string ModelName => string.IsNullOrEmpty(_modelActif)
                                   ? "*"
                                   : Path.GetFileNameWithoutExtension(_modelActif);



        // Pour faciliter le binding dans l'UI directement sur la liste de parts
        public ObservableCollection<Part> Parts => PartVM.Parts;


        #endregion

        #region Fichier Resx

        private string _filePathResx;
        public string FilePathResx
        {
            get => string.IsNullOrEmpty(_filePathResx) ? "Fichier .Resx" : _filePathResx;
            set
            {
                if (_filePathResx != value)
                {
                    _filePathResx = value;
                    OnPropertyChanged(nameof(FilePathResx));
                    OnPropertyChanged(nameof(FileResxName));
                }
            }
        }

        public string FileResxName
        {
            get
            {
                if (string.IsNullOrEmpty(_filePathResx) || !File.Exists(_filePathResx))
                    return "(aucun)";

                try
                {
                    // Récupération des métadonnées
                    var meta = _resuxSerializer.ExtractMetadataFromFile(_filePathResx);
                    return string.IsNullOrEmpty(meta.Projet) ? Path.GetFileNameWithoutExtension(_filePathResx)
                                                                  : meta.Projet;
                }
                catch
                {
                    // En cas d'erreur de lecture, retourner le nom du fichier
                    return Path.GetFileNameWithoutExtension(_filePathResx);
                }
            }
        }

        #endregion

        #region Nombre de Parts & Requests + Liste de pièces

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

        #region ModelManager Events

        private void SubscribeToModelManagerEvents()
        {
            ModelManager.OnModelChanged += model =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ModelActif = model as string;
                });
            };

            ModelManager.FilePathResxChanged += filresx =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilePathResx = filresx as string;
                });
            };
        }

        #endregion


        // TODO revoir cette partie
        #region Events Manager - Number Part & Req

        private async Task NumbersPartReqChanged()
        {
            Debug.WriteLine("[MainViewModel] - NumbersPartReqChanged()");
            EventsManager.ModelOpen -= UpdateCountPartReqWrapper;
            EventsManager.ModelOpen += UpdateCountPartReqWrapper;
            EventsManager.RequirementAddedOrDelete -= UpdateCountPartReqWrapper;
            EventsManager.RequirementAddedOrDelete += UpdateCountPartReqWrapper;
            EventsManager.PartAddedOrDelete -= UpdateCountPartReqWrapper;
            EventsManager.PartAddedOrDelete += UpdateCountPartReqWrapper;
        }

        // Wrapper pour gérer async correctement dans un événement
        private async Task UpdateCountPartReqWrapper()
        {
            await UpdateCountsAsync();
        }

        // TODO a revoir 
        public async Task UpdateCountsAsync()
        {
            Debug.WriteLine("[MainViewModel] - UpdateCountsAsync()");
            try
            {
                int numberOfParts = await DatabaseService.ActiveInstance.GetPartsCountAsync();
                int numberOfReq = await DatabaseService.ActiveInstance.GetNumberReqAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    NumberOfParts = numberOfParts;
                    NumberOfReq = numberOfReq;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateCountsAsync Exception : {ex.Message}");
            }
        }

        #endregion

        #region Provisoire ToDoItem

        /// Classe représentant un élément de type ToDo.
        public class ToDoItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public bool IsDone { get; set; }
        }


        public ObservableCollection<ToDoItem> ToDoItems { get; set; } = new();
        public void LoadToDoItems()
        {
            ToDoItems.Add(new ToDoItem { Title = "Ajouter glisser-déposer pour panels", IsDone = false });
            ToDoItems.Add(new ToDoItem { Title = "Intégrer TreeView interactif", IsDone = false });
            ToDoItems.Add(new ToDoItem { Title = "Binding dynamique des modèles", IsDone = false });
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region RelayCommand

        // TODO Possibilité d'enlever les fonctions non utiles 

        #region ModelsVM

        public ICommand CreateModelCommand => ModelsVM.CreateCommand;
        public ICommand DeleteModelCommand => ModelsVM.DeleteCommand;
        public ICommand DuplicateModelCommand => ModelsVM.DuplicateCommand;
        public ICommand OpenModelCommand => ModelsVM.OpenCommand;
        public ICommand OpenModelFileCommand => ModelsVM.OpenFromFileCommand;
        public ICommand ToggleEditCommand => ModelsVM.ToggleEditCommand;
        public ICommand DeleteActiveModelCommand => ModelsVM.DeleteActiveModelCommand;
        public ICommand DuplicateActiveModelCommand => ModelsVM.DuplicateActiveModelCommand;
        public ICommand OpenRegisterWindowCommand => ModelsVM.OpenRegisterWindowCommand;


        #endregion

        #region DatasVM

        public ICommand LoadDatasCommand => DataVM.LoadCommand;
        public ICommand CreatePartCommand => DataVM.CreatePartCommand;
        public ICommand CreateDataCommand => DataVM.CreateDataCommand;
        public ICommand ShowWindowDeletePartCommand => DataVM.ShowWindowDeletePartCommand;
        public ICommand DeletePartCommand => DataVM.DeletePartCommand;
        public ICommand CheckIsoPartCommand => DataVM.CheckIsoPartCommand;
        public ICommand SaveAllCommand => DataVM.SaveAllCommand;
        public ICommand FocusDataByPartIdCommand => DataVM.FocusDataByPartIdCommand;

        #endregion

        #region ReqsVM
        public ICommand LoadReqsCommand => RequirementVM.LoadCommand;
        public ICommand SaveReqsCommand => RequirementVM.SaveCommand;
        public ICommand SaveUniqueCommand => RequirementVM.SaveUniqueCommand;
        public ICommand RemoveUniqueCommand => RequirementVM.RemoveUniqueCommand;
        public ICommand ClearPanelCommand => RequirementVM.ClearPanelCommand;
        public ICommand CreateRequirementCommand => RequirementVM.CreateRequirementCommand;
        public ICommand DeleteRequirementCommand => RequirementVM.DeleteRequirementCommand;

        #endregion

        #region PartsVM
        public ICommand LoadPartCommand => PartVM.LoadCommand;
        //public ICommand CreatePartCommand => PartVM.CreateCommand;
        public ICommand SaveCommand => PartVM.SaveCommand;
        //public ICommand DeletePartCommand => PartVM.DeleteCommand;
        public ICommand TestCommand => PartVM.TestCommand;
        public ICommand InsertImageCommand => PartVM.InsertImageCommand;

        #endregion
        #endregion


    }

}

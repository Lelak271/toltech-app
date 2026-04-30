using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Services.Dialog;
using Toltech.App.Services.Notification;
using Toltech.App.Utilities;
using Westermo.GraphX.Common.Exceptions;

namespace Toltech.App.ViewModels
{
    public class PartDBViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialog;

        private DomainService _domainService;
        private static bool _subscribed = false;
        public PartDBViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _domainService = mainVM.DomainService;

            _dialog = App.DialogService;
            _notificationService = App.NotificationService;

            Parts = new ObservableCollection<Part>();
            SelectedParts = new ObservableCollection<Part>();

            #region Commandes
            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            CreateCommand = new RelayCommand(async _ => await CreateAsync());
            //CreateCommand = new RelayCommand(async _ => await AddLine());
            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync());
            InsertImageCommand = new RelayCommand(async _ => await InsertImageAsync(), _ => SelectedParts != null);

            #endregion

            #region Events
            if (!_subscribed)
            {
                // Gestion centrale du Chargement des Parts du modèle actif
                EventsManager.ModelOpen += WrapperLoadAsync;
                EventsManager.PartAddedOrDelete += WrapperLoadAsync;
                _subscribed = true;
            }
            #endregion

            _ = LoadAsync();
        }

        // ------------------------------------------------------------
        #region Collections
        // ------------------------------------------------------------

        public ObservableCollection<Part> Parts { get; set; } = new ObservableCollection<Part>();
        private ObservableCollection<Part> _selectedParts = new ObservableCollection<Part>();
        public ObservableCollection<Part> SelectedParts
        {
            get => _selectedParts;
            set => SetProperty(ref _selectedParts, value); // Permet au binding TwoWay de fonctionner
        }
        #endregion

        #region Commands

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand TestCommand { get; }
        public ICommand InsertImageCommand { get; }

        #endregion

        #region Méthodes

        private async Task LoadAsync()
        {
            if (!ModelValidationHelper.CheckModelActif(false))
                return;

            Parts.Clear();

            var partsResult = await _domainService.GetAllPartsAsync();
            if (partsResult.IsFailure) return;

            foreach (var part in partsResult.Value.OrderBy(p => p.NamePart))
            {
                Parts.Add(part);
                //System.Diagnostics.Debug.WriteLine( $"[PartVM] Ajout - Id={part.Id}, Nom='{part.NamePart}'");
            }

        }
        #endregion

        // Wrapper void 
        private async Task WrapperLoadAsync()
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PartVM] Erreur lors du rechargement : {ex}");
            }
        }

        private async Task CreateAsync()
        {
            string namePart = "Nouvelle pièce";
            var newPart = new Part
            {
                NamePart = namePart,
                MasseVol = 0.0,
                Comment = string.Empty,
                IsActive = true
            };

            await _domainService.InsertPartAsync(newPart);
            _ = _notificationService.ShowNotifAsync($"Pièce ajoutée {namePart}.");
            //Parts.Add(newPart);
        }

        private async Task SaveAsync()
        {
            var toSave = Parts.Where(p => p.IsDirty).ToList();
            if (toSave.Count == 0)
                return;

            var result = await _domainService.UpdatePartsAsync(toSave);

            if (!result.IsSuccess)
            {
                HandleError(result);
                return;
            }

            _ = _notificationService.ShowNotifAsync("Données sauvegardées.");
        }


        public async Task DeleteByIdAsync(int idPart)
        {
            var namePartResult = await _domainService.GetPartNameByIdAsync(idPart);

            if (!namePartResult.IsSuccess)
                return;

            var namePart = namePartResult.Value;

            if (!_dialog.Confirm($"Voulez-vous supprimer la pièce {namePart}"))
                return;
            if (idPart == 0)
                return;
            await _domainService.DeletePartWithDatasByIdAsync(idPart);
        }
        private async Task DeleteAsync()
        {
            if (SelectedParts.Count == 0)
                return;

            var result = MessageBox.Show(
                $"Supprimer {SelectedParts.Count} pièce(s) ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var partsToDelete = SelectedParts.ToList();

            var deleteResult = await _domainService.DeletePartsAsync(partsToDelete);

            if (!deleteResult.IsSuccess)
            {
                _ = _notificationService.ShowNotifAsync(deleteResult.Error);
                return;
            }

            foreach (var part in partsToDelete)
            {
                Parts.Remove(part);
            }

            _ = _notificationService.ShowNotifAsync($"{partsToDelete.Count} pièce(s) supprimée(s).");

            SelectedParts.Clear();
        }
        private async Task ReverseActivePartByIdAsync(Part part)
        {
            await _domainService.SetActivePart_PartAsync(part);
        }
        public async Task ReverseActivePartByIdAsync(int idPart)
        {
            var part = await _domainService.GetPartByIdAsync(idPart);
            await _domainService.SetActivePart_PartAsync(part.Value);
        }


        private async Task InsertImageAsync()
        {
            // On prend la première ligne sélectionnée uniquement
            var selectedPart = SelectedParts.FirstOrDefault();
            if (selectedPart == null)
                return;

            // Ouvrir un dialog pour choisir l'image
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc("Choose_an_image"),
                Filter = $"{Loc("Picture")}|*.png;*.jpg;*.jpeg;*.bmp",
                Multiselect = false, 
            };

            bool? result = openFileDialog.ShowDialog();
            if (result != true)
                return;

            string filePath = openFileDialog.FileName;

            if (!File.Exists(filePath))
                return;

            // Lire le fichier en byte[]
            byte[] imageBytes = await File.ReadAllBytesAsync(filePath);

            // Mettre à jour la propriété ImagePart de la ligne sélectionnée
            selectedPart.ImagePart = imageBytes;

            // Sauvegarde en base
            await _domainService.UpdatePartsAsync(selectedPart);

            // Notifier l'UI si nécessaire
            OnPropertyChanged(nameof(SelectedParts));
        }





    }
}

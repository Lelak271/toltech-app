using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Toltech.App.ViewModels;
using Toltech.App.Services.Notification;
using Toltech.App.Models;
using Toltech.App.Resources;
using Toltech.App.Services;
using CST = Toltech.App.Services;
using Toltech.App.Utilities;
using Toltech.App.Services.Dialog;

namespace Toltech.App.ViewModels
{
    public class PartDBViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialog;

        private static bool _subscribed = false;
        public PartDBViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _dialog = App.DialogService;
            _notificationService = App.NotificationService;

            Parts = new ObservableCollection<Part>();
            SelectedParts = new ObservableCollection<Part>();

            LoadCommand = new RelayCommand(async _ => await LoadAsync());
            CreateCommand = new RelayCommand(async _ => await CreateAsync());
            //CreateCommand = new RelayCommand(async _ => await AddLine());
            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync());
            TestCommand = new RelayCommand(async _ => await SetImageInFirstPartAsync());
            InsertImageCommand = new RelayCommand(async _ => await InsertImageAsync(), _ => SelectedParts != null);

            if (!_subscribed)
            {
                // Gestion centrale du Chargement des Parts du modèle actif
                EventsManager.ModelOpen += WrapperLoadAsync;
                EventsManager.PartAddedOrDelete += WrapperLoadAsync;
                _subscribed = true;
            }
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

            var parts = await  DatabaseService.ActiveInstance.GetAllPartsAsync();
            if (parts == null) return;

            foreach (var part in parts.OrderBy(p => p.NamePart))
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

            await DatabaseService.ActiveInstance.InsertPartAsync(newPart);
            _notificationService.ShowNotifAsync($"Pièce ajoutée {namePart}.");
            //Parts.Add(newPart);
        }

        private async Task SaveAsync()
        {
            var dbParts = await DatabaseService.ActiveInstance.GetAllPartsAsync();

            foreach (var uiPart in Parts.ToList()) // Parts = ObservableCollection<Part> de l'UI
            {
                // Cherche l'équivalent en DB (par Id)
                var dbPart = dbParts.FirstOrDefault(p => p.Id == uiPart.Id);

                if (dbPart == null)
                {
                    // Nouvelle ligne → insertion
                    await DatabaseService.ActiveInstance.InsertPartAsync(uiPart);
                }
                else
                {
                    // Comparaison des champs importants pour détecter un changement
                    bool nameChanged = !string.Equals(uiPart.NamePart, dbPart.NamePart, StringComparison.Ordinal);
                    bool masseChanged = Math.Abs(uiPart.MasseVol - dbPart.MasseVol) > Constants.EPSILON;
                    bool commentChanged = uiPart.Comment != dbPart.Comment;
                    bool imageChanged = !AreImagesEqual(uiPart.ImagePart, dbPart.ImagePart);
                    bool isActiveChanged = uiPart.IsActive != dbPart.IsActive;

                    if (nameChanged || masseChanged || commentChanged || imageChanged || isActiveChanged)
                    {
                        // Si le nom a changé, on peut vérifier l'unicité
                        if (nameChanged)
                        {
                            bool exists = await DatabaseService.ActiveInstance.IsNamePartExisteAsync(uiPart.NamePart);
                            if (exists)
                            {
                            }
                            await DatabaseService.ActiveInstance.UpdatePartNameAsync(dbPart.Id, uiPart.NamePart);
                        }

                        // Mise à jour
                        await DatabaseService.ActiveInstance.UpdatePartAsync(uiPart);
                    }
                }
            }
            _notificationService.ShowNotifAsync($"Données sauvegardées.");
        }

        // Comparaison simple des tableaux byte[] pour les images
        private bool AreImagesEqual(byte[] img1, byte[] img2)
        {
            if (ReferenceEquals(img1, img2)) return true;
            if (img1 == null || img2 == null) return false;
            if (img1.Length != img2.Length) return false;
            for (int i = 0; i < img1.Length; i++)
                if (img1[i] != img2[i]) return false;
            return true;
        }

        public async Task DeleteByIdAsync(int idPart)
        {
           string namePart= await DatabaseService.ActiveInstance.GetPartNameByID(idPart);

            if (!_dialog.Confirm($"Voulez-vous supprimer la pièce {namePart}"))
              return;
            if (idPart == 0)
                return;
            await DatabaseService.ActiveInstance.DeletePartAsync(idPart);
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

            foreach (var part in SelectedParts.ToList())
            {
                 await DatabaseService.ActiveInstance.DeletePartAsync(part);
                 Parts.Remove(part);
            }

            SelectedParts.Clear();
        }
        private async Task ReverseActivePartByIdAsync(Part part)
        {
            await DatabaseService.ActiveInstance.SetActivePart_PartAsync(part);
        }
        public async Task ReverseActivePartByIdAsync(int idPart)
        {
            var part = await DatabaseService.ActiveInstance.GetPartByIdAsync(idPart);
            await DatabaseService.ActiveInstance.SetActivePart_PartAsync(part);
        }



        public static async Task SetImageInFirstPartAsync()
        {
            string filePath = @"C:\Users\louis\OneDrive\Images\Screenshots\Capture d'écran 2025-05-24 004423.png";

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Le fichier image n'existe pas.", filePath);

            // Lire l'image en byte[]
            byte[] imageBytes = await File.ReadAllBytesAsync(filePath);

            var db = DatabaseService.ActiveInstance;

            // Récupérer toutes les parts
            var parts = await db.GetAllPartsAsync();

            if (parts == null || !parts.Any())
            {
                // Table vide → créer la première ligne
                var newPart = new Part
                {
                    NamePart = "Exemple",
                    MasseVol = 0.0,
                    Comment = "Image test",
                    ImagePart = imageBytes
                };

                await db.InsertPartAsync(newPart);
            }
            else
            {
                // Mettre à jour la première ligne existante
                var firstPart = parts.First();
                firstPart.ImagePart = imageBytes;
                await db.UpdatePartAsync(firstPart);
            }
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
                Title = "Choisir une image",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp",
                Multiselect = false
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
            await DatabaseService.ActiveInstance.UpdatePartAsync(selectedPart);

            // Notifier l'UI si nécessaire
            OnPropertyChanged(nameof(SelectedParts));
        }





    }
}

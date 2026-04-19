using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using TOLTECH_APPLICATION.ViewModels;
using TOLTECH_APPLICATION.Resources;
using TOLTECH_APPLICATION.Services.Dialog;

namespace TOLTECH_APPLICATION.FrontEnd.Controls
{
    public partial class RegisterModelWindow : Window
    {
        #region Fields & Properties
        //private readonly MainViewModel _mainVM;
        private readonly IDialogService _dialog;
        private readonly ModelsViewModel _modelsVM;

        public ObservableCollection<DriveItem> DriveItems { get; set; } = new ObservableCollection<DriveItem>();
        #endregion

        #region Constructor
        public RegisterModelWindow(ModelsViewModel modelVM)
        {
            //_mainVM = modelVM.MainVM;
            _modelsVM = modelVM;
            _dialog = App.DialogService;

            DataContext = modelVM;
            InitializeComponent();

            LoadDrivesAndKnownFolders();
            ListDrives.ItemsSource = DriveItems;
        }
        #endregion

        private async void RegisterActiveModel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Enregistre le modèle actif via la VM
                await _modelsVM.RegisterModelAsync();

                // Optionnel : désactiver le bouton et mettre un tooltip
                // BtnRegisterActiveModel.IsEnabled = false;
                // BtnRegisterActiveModel.ToolTip = (string)Application.Current.FindResource("ModelAlreadyRegistered");
            }
            catch (Exception ex)
            {
                // Affiche une erreur si l'enregistrement échoue
                _dialog.Error($"Impossible d'enregistrer le modèle actif : {ex.Message}", "Erreur");
            }
        }

        private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ouvre le dialogue pour sélectionner un fichier
                string? selectedFile = _dialog.OpenFile(
                    "Fichiers Tolx (*.tolx)|*.tolx",
                    "Sélectionnez un fichier .tolx"
                );

                // Si l'utilisateur annule
                if (string.IsNullOrWhiteSpace(selectedFile))
                    return;

                // Affiche le fichier sélectionné
                _dialog.Info($"Fichier sélectionné : {selectedFile}", "Info");

                // Enregistre le modèle sélectionné via la VM
                await _modelsVM.RegisterModelAsync(selectedFile);
            }
            catch (Exception ex)
            {
                _dialog.Error($"Impossible d'enregistrer le fichier sélectionné : {ex.Message}", "Erreur");
            }
        }



        #region Drive / Folder Loading
        private void LoadDrivesAndKnownFolders()
        {
            DriveItems.Clear();

            // 1) Lecteurs
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;

                DriveItems.Add(new DriveItem
                {
                    Name = $"{drive.Name} ({drive.DriveType})",
                    Path = drive.Name,
                    IconGlyph = "💽"
                });
            }

            // 2) Dossiers système de base
            AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Bureau", "🖥");
            AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Documents", "📄");
            AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Images", "🖼");
            AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Musique", "🎵");
            AddFolderIfExists(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Vidéos", "🎬");
            AddFolderIfExists(GetKnownFolderPath(KnownFolder.Downloads), "Téléchargements", "⬇️");

            // 3) Raccourcis épinglés Quick Access de l'utilisateur
            foreach (var path in GetPinnedFolders())
            {
                AddFolderIfExists(path.Path, path.Name, path.IconGlyph);
            }
        }

        private void AddFolderIfExists(string path, string displayName, string glyph)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

                DriveItems.Add(new DriveItem
                {
                    Name = displayName,
                    Path = path,
                    IconGlyph = glyph
                });
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Récupère les dossiers épinglés Quick Access de l'utilisateur
        /// </summary>
        private static DriveItem[] GetPinnedFolders()
        {
            // TODO Trop compliqué pour le moment , à revoir plus tard si besoin
            return Array.Empty<DriveItem>();
        }

        private static string GetKnownFolderPath(KnownFolder folder)
        {
            IntPtr outPath;
            int hr = SHGetKnownFolderPath(folder.Id(), 0, IntPtr.Zero, out outPath);
            if (hr >= 0)
            {
                string path = Marshal.PtrToStringUni(outPath);
                Marshal.FreeCoTaskMem(outPath);
                return path ?? "";
            }
            return "";
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
        #endregion

        #region Selection / OpenFile
        private void ListDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListDrives.SelectedItem is not DriveItem selected) return;

            string? selectedFile = _dialog.OpenFile(
                "Fichiers Tolx (*.tolx)|*.tolx",
                "Sélectionnez un fichier .tolx",
                selected.Path
            );

            if (string.IsNullOrWhiteSpace(selectedFile)) return;

            _dialog.Info($"Fichier sélectionné : {selectedFile}", "Info");
            _modelsVM.RegisterModelAsync(selectedFile);
        }
        #endregion
    }

    #region Models
    public class DriveItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string IconGlyph { get; set; } = "";
    }

    public enum KnownFolder
    {
        Downloads
    }

    public static class KnownFolderExtensions
    {
        public static Guid Id(this KnownFolder folder) => folder switch
        {
            KnownFolder.Downloads => new Guid("374DE290-123F-4565-9164-39C4925E467B"),
            _ => throw new ArgumentOutOfRangeException(nameof(folder))
        };
    }
    #endregion
}

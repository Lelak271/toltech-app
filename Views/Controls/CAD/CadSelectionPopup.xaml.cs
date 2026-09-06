using System.ComponentModel;
using System.Windows;
using Toltech.Cad.Abstractions;
using Toltech.Cad.Model;
namespace Toltech.App.Views.Controls.CAD
{
    /// <summary>
    /// Logique d'interaction pour CadSelectionPopup.xaml
    /// </summary>
    public partial class CadPointSelectionWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _cadLogoPath = "/Asset/Logos/FreeCAD.png";
        public string CadLogoPath
        {
            get => _cadLogoPath;
            set
            {
                if (_cadLogoPath == value)
                    return;

                _cadLogoPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CadLogoPath)));
            }
        }
        private string _cadDisplayName = "CAO";

        public string CadDisplayName
        {
            get => _cadDisplayName;
            set
            {
                if (_cadDisplayName == value)
                    return;

                _cadDisplayName = value;
                 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CadLogoPath)));
            }
        }

        private string _hintMessage = string.Empty;

        public string HintMessage
        {
            get => _hintMessage;

            private set
            {
                if (_hintMessage == value)
                    return;

                _hintMessage = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HintMessage)));
            }
        }

        private readonly ICadSelectionService _cadSelectionService;
        public CadPointSelectionWindow(ICadSelectionService cadSelectionService)
        {
            InitializeComponent();
            DataContext = this;
            _cadSelectionService = cadSelectionService;
            _cadSelectionService.HintReceived +=    OnHintReceived;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lit l'état actuel du service CAO au moment de l'ouverture
            // et détermine le logo à afficher en conséquence.
            UpdateCadLogo();
            CadDisplayName = _cadSelectionService.ActiveSoftware.ToString();

        }

        private void UpdateCadLogo()
        {
            CadLogoPath = _cadSelectionService.ActiveSoftware switch
            {
                CadSoftware.FreeCAD => "/Asset/Logos/FreeCAD.png",
                CadSoftware.SolidWorks => "/Asset/Logos/SOLIDWORKS.png",
                CadSoftware.None => "/Asset/Logos/NoCad.png",
                _ => "/Asset/Logos/NoCad.png"
            };

        }

        /// <summary>
        /// Reçoit un message d'information provenant du logiciel de CAO.
        /// </summary>
        private void OnHintReceived(
            object? sender,
            string message)
        {
            // L'événement peut provenir du thread HTTP.
            // On revient donc sur le thread WPF.
            Dispatcher.Invoke(() =>
            {
                HintMessage = message;
            });
        }

        private async  void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // Demande au logiciel de CAO
                // d'arrêter le mode de sélection.
                await _cadSelectionService.StopSelectionAsync();
                _cadSelectionService.HintReceived -=  OnHintReceived;
            }
            catch (Exception error)
            {
                // L'arrêt de la sélection ne doit pas
                // empêcher la fermeture du popup.
                System.Diagnostics.Debug.WriteLine(
                    $"Erreur lors de l'arrêt de la sélection CAO : {error}");
            }
            finally
            {
                // Ferme ensuite la fenêtre.
                Close();
            }
        }

    }
}

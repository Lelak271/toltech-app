using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DocumentFormat.OpenXml.ExtendedProperties;

namespace Toltech.App.FrontEnd.Interfaces
{
    public partial class NotificationControl : UserControl
    {
        private bool _isClosing = false;
        private TaskCompletionSource<bool> _closeTcs;


        public NotificationControl()
        {
            InitializeComponent();
            // Pour s'assurer que ActualWidth est correctement défini avant d'animer
            this.Loaded += NotificationControl_Loaded;
        }

        public NotificationControl(string message) : this()
        {
            MessageText.Text = message;
        }

        private void NotificationControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Optionnel : désabonnement pour éviter appels multiples
            this.Loaded -= NotificationControl_Loaded;
        }

        public async Task ShowAsync(bool isError = false)
        {
            int durationMs = 3000;

            if (isError)
            {
                SetBackgroundColors(
                    "#D70022",
                    "#FFFFFF",
                    "#FFB3B3",
                    "#FF7F7F",
                    "#FFFFFF",
                    "#FF4500"
                );
            }

            // Fade-in
            Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(300);

            // Animation de progression
            var fillAnimation = new DoubleAnimation
            {
                From = 0,
                To = ActualWidth,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            LoadingProgress.BeginAnimation(WidthProperty, fillAnimation);

            // Attente de la durée d’affichage
            await Task.Delay(durationMs);

            // Si déjà fermé par clic, ne rien faire
            if (_isClosing)
                return;

            await CloseAsync();
        }


        public void SetBackgroundColors(string headerColorHex,         // Couleur du header
                                        string headerTextColorHex,     // Couleur du texte du header
                                        string firstLoadingColorHex,   // Couleur de fond FirstLoadingProgress
                                        string loadingColorHex,        // Couleur de la barre LoadingProgress
                                        string messageTextColorHex,    // Couleur du texte du message
                                        string progressBarColorHex     // Couleur de la ProgressBar
                                    )
        {
            var brushHeader = (SolidColorBrush)new BrushConverter().ConvertFrom(headerColorHex);
            var brushHeaderText = (SolidColorBrush)new BrushConverter().ConvertFrom(headerTextColorHex);
            var brushFirstLoading = (SolidColorBrush)new BrushConverter().ConvertFrom(firstLoadingColorHex);
            var brushLoading = (SolidColorBrush)new BrushConverter().ConvertFrom(loadingColorHex);
            var brushMessageText = (SolidColorBrush)new BrushConverter().ConvertFrom(messageTextColorHex);
            var brushProgressBar = (SolidColorBrush)new BrushConverter().ConvertFrom(progressBarColorHex);

            // Header
            HeaderNotif.Background = brushHeader;
            if (HeaderNotif.Child is TextBlock headerText)
                headerText.Foreground = brushHeaderText;

            // Fond principal
            FirstLoadingProgress.Background = brushFirstLoading;

            // Barre de progression chargée
            LoadingProgress.Background = brushLoading;

            // Message
            MessageText.Foreground = brushMessageText;

            // ProgressBar
            ProgressBar.Foreground = brushProgressBar;
        }

        private async void OnNotificationClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await CloseAsync();
        }

        private async Task CloseAsync()
        {
            if (_isClosing)
                return;

            _isClosing = true;

            // Stoppe les animations en cours
            BeginAnimation(OpacityProperty, null);
            LoadingProgress.BeginAnimation(WidthProperty, null);

            var fadeOut = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(250));
            _closeTcs = new TaskCompletionSource<bool>();

            fadeOut.Completed += (s, e) =>
            {
                _closeTcs.TrySetResult(true);
            };

            BeginAnimation(OpacityProperty, fadeOut);
            await _closeTcs.Task;
        }



    }
}

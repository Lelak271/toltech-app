using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Toltech.App.FrontEnd.Controls;
using Toltech.App.ViewModels;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Utilities;

namespace Toltech.App.Views
{
    public partial class PageData : UserControl
    {
        #region Champs privés

        private bool _isInternalChange = false;
        private bool _hasChanges = false;
        public List<PanelData> modelDataControl = new List<PanelData>();
        private DatasViewModel ViewModel => DataContext as DatasViewModel;

        #endregion

        #region Constructeur
        public PageData()
        {
            InitializeComponent();
        }
        #endregion

        private async void Page3Load(object sender, RoutedEventArgs e)
        {
            //await LoadDataVM();
            //FocusSelectedItem(); // TODO a remettre dans le UX

        }



        /// <summary>
        /// TO DO 
        /// </summary>
        private void FocusSelectedItem()
        {
            var vm = DataContext as DatasViewModel;

            vm.RequestFocusItem += data =>
            {
                if (data == null) return;

                // Dispatcher pour attendre que le ListBox ait généré ses containers
                ListBoxDatas.Dispatcher.InvokeAsync(() =>
                {
                    ListBoxDatas.ScrollIntoView(data);

                    if (ListBoxDatas.ItemContainerGenerator.ContainerFromItem(data) is ListBoxItem item)
                        item.Focus();
                }, DispatcherPriority.Loaded);
            };

        }


        #region Fonction UI

        #region Fonctions liées aux Buttons du HeaderPage3

        // Appel de la fonction Save Data
        // TBD mettre dans la VM 
        private async void MultiEdit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ModelManager.ModelActif))
            {
                MessageBox.Show("Aucun modèle actif sélectionné.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //MessageBox.Show($"Fonction pas encore integrée", "MultiEdition", MessageBoxButton.OK, MessageBoxImage.Information);

            if (Application.Current.Dispatcher.CheckAccess())
            {
                MessageBox.Show($"Texte depuis UI");
            }
            else
            {
                MessageBox.Show("Texte non  UI");
            }
        }

        #endregion



        #region Gestion Button Show Hide

        // Etat du header
        private bool _headerVisible = true;

        // Pour savoir si la souris est sur le header
        private bool _isMouseOverHeader = false;
        private void FloatingHeader_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isMouseOverHeader = true;

            // Ne relance pas l'animation si le header est déjà visible
            if (!_headerVisible)
            {
                ShowHeader();
                _headerVisible = true;
            }

            // Arrêter le timer si la souris est dessus
            _hideTimer.Stop();
        }

        private void FloatingHeader_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //_isMouseOverHeader = false;

            //// Si ScrollViewer tout en haut, header toujours visible
            //if (ScrollPage3.VerticalOffset <= 0)
            //{
            //    if (!_headerVisible) // ne relance pas si déjà visible
            //    {
            //        ShowHeader();
            //        _headerVisible = true;
            //    }
            //    _hideTimer.Stop(); // pas de disparition automatique
            //}
            //else
            //{
            //    // Ne lancer le timer que si le header est visible et que le timer n’est pas déjà en cours
            //    if (_headerVisible && !_hideTimer.IsEnabled)
            //    {
            //        _hideTimer.Start();
            //    }
            //}
        }

        private void ShowHeader()
        {
            var sb = new Storyboard();

            // Opacité
            var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            Storyboard.SetTarget(opacityAnim, FloatingHeader);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            sb.Children.Add(opacityAnim);

            // Translation (header descend depuis le haut)
            var translateAnim = new DoubleAnimation(-50, 0, TimeSpan.FromMilliseconds(300));
            Storyboard.SetTarget(translateAnim, HeaderTransform);
            Storyboard.SetTargetProperty(translateAnim, new PropertyPath("Y"));
            sb.Children.Add(translateAnim);

            sb.Begin();
        }

        private void HideHeader()
        {
            var sb = new Storyboard();

            var opacityAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            Storyboard.SetTarget(opacityAnim, FloatingHeader);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            sb.Children.Add(opacityAnim);

            var translateAnim = new DoubleAnimation(0, -50, TimeSpan.FromMilliseconds(300));
            Storyboard.SetTarget(translateAnim, HeaderTransform);
            Storyboard.SetTargetProperty(translateAnim, new PropertyPath("Y"));
            sb.Children.Add(translateAnim);

            sb.Begin();
        }

        private DispatcherTimer _hideTimer = new DispatcherTimer();
        private void Timer()
        {
            _hideTimer.Interval = TimeSpan.FromSeconds(3);
            _hideTimer.Tick += (s, e) =>
            {
                if (!_isMouseOverHeader)
                {
                    HideHeader();
                    _headerVisible = false;
                    _hideTimer.Stop();
                }
            };

        }


        #endregion

        #endregion

    }
}

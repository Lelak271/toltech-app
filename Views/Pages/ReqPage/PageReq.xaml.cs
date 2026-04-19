using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Toltech.App.FrontEnd.Controls;
using Toltech.App.ViewModels;
using Toltech.App.Services;


namespace Toltech.App.Views
{
    public partial class PageReq : UserControl
    {
        #region Champs privés
        public List<PanelReqs> ReqsDataControl = new List<PanelReqs>();
        private string _lastModelActifId = null; // Champ persistant
        private bool _isInternalChange = false;
        private bool _hasChanges = false;

        private ScrollViewer _internalScrollViewer;
        #endregion

        #region Constructeur
        public PageReq()
        {
            InitializeComponent();

            ModelManager.OnPartChanged += async (sender) =>
            {
                if (_hasChanges)
                {
                    //await ConfirmAndSaveCurrentPartAsync();  // pas sur de l'utilité
                    _hasChanges = false;
                }
            };

        }
        #endregion

        #region Méthodes d'initialisation

        // Gestionnaire d'événement pour le chargement de la page
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Récupère le ViewModel depuis le DataContext
            //if (DataContext is RequirementsViewModel viewModel)
            //{
            //    await viewModel.LoadAsync();
            //}

            //var currentModelId = ModelManager.ModelActif;

            //if (string.IsNullOrEmpty(currentModelId))
            //{
            //    return;
            //}

            //bool isModelChanged = _lastModelActifId != currentModelId; // True si le modèle actif a changé
            //bool isPanelReqsEmpty = true;
            //var vm = DataContext as RequirementsViewModel;
            //if (vm == null || vm.Requirements == null)
            //    return;


            //if (isModelChanged || isPanelReqsEmpty)
            //{
            //    _lastModelActifId = currentModelId;

            //}

            //// Recherche du scrollInterne de la listbox pour appliquer la logique de ScrollChanged
            //_internalScrollViewer = FindVisualChild<ScrollViewer>(ListBoxReqs);

            //if (_internalScrollViewer != null)
            //{
            //    _internalScrollViewer.ScrollChanged += ScrollPageReq_ScrollChanged;
            //}

        }


        #endregion

        #region TreeView UI - Changement utilisateur
        /// Affiche une boîte de dialogue pour confirmer la sauvegarde de la pièce en cours avant de poursuivre l'action sur une autre pièce.
        public async Task<bool> ConfirmAndSaveCurrentPartAsync()
        {
            MessageBoxResult result = MessageBox.Show(
                "Souhaitez-vous sauvegarder les données de la pièce en cours avant de continuer ?",
                "Confirmation de sauvegarde",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _hasChanges = false; // Reinitialisation
                return true; // continuer l’action appelante
            }
            else if (result == MessageBoxResult.No)
            {
                return true; // continuer sans sauvegarde
                _hasChanges = false; // Reinitialisation
            }
            else // Cancel
            {
                return false; // annuler l’action appelante
            }
        }

        // Fonction qui définie _hasChanges = true SSI c'est une modification de l'utilisateur 
        private void AttachChangeHandlersReq(PanelReqs control)
        {
            foreach (var child in FindVisualChildren<TextBox>(control))
            {
                child.TextChanged += (s, e) =>
                {
                    if (!_isInternalChange)
                        _hasChanges = true;
                };
            }

            foreach (var child in FindVisualChildren<ComboBox>(control))
            {
                child.SelectionChanged += (s, e) =>
                {
                    if (!_isInternalChange)
                        _hasChanges = true;
                };
            }

            foreach (var child in FindVisualChildren<CheckBox>(control))
            {
                child.Checked += (s, e) =>
                {
                    if (!_isInternalChange)
                        _hasChanges = true;
                };
                child.Unchecked += (s, e) =>
                {
                    if (!_isInternalChange)
                        _hasChanges = true;
                };
            }
        }

        /// Recherche récursivement tous les éléments enfants visuels d’un type donné dans l’arborescence WPF.
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        #endregion


        #region Fonction UI


        #region Gestion Button Show Hide

        private double _previousOffset = 0;
        private const double ScrollThreshold = 80; // Seuil en pixels pour déclencher le hide/show

        // Etat du header
        private bool _headerVisible = true;

        private void ScrollPageReq_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double delta = e.VerticalOffset - _previousOffset;

            // Si on est tout en haut, on force l'affichage
            if (e.VerticalOffset <= 0)
            {
                if (!_headerVisible) // ne relance pas si déjà visible
                {
                    ShowHeader();
                    _headerVisible = true;
                }
                _hideTimer.Stop(); // pas de disparition automatique
            }
            else
            {
                if (delta > ScrollThreshold && _headerVisible)
                {
                    HideHeader();
                    _headerVisible = false;
                }
                else if (delta < -ScrollThreshold && !_headerVisible)
                {
                    ShowHeader();
                    _headerVisible = true;

                    // Lancer le timer pour disparition automatique
                    _hideTimer.Stop();
                    _hideTimer.Start();
                }
            }

            // Mettre à jour uniquement si delta dépasse le seuil
            if (Math.Abs(delta) > ScrollThreshold)
                _previousOffset = e.VerticalOffset;
        }


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
            _isMouseOverHeader = false;

            ////Si ScrollViewer tout en haut, header toujours visible
            //if (ScrollPageReq.VerticalOffset <= 0)
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

        #endregion

        #endregion


        // Méthode appelée lors du click sur "Édition multiple"
        private void MultiEdit_Click(object sender, RoutedEventArgs e)
        {
            // TODO : implémenter l'édition multiple
        }




    }
}

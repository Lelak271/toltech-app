using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TOLTECH_APPLICATION.FrontEnd.Controls;
using TOLTECH_APPLICATION.ViewModels;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Utilities;

namespace TOLTECH_APPLICATION.Views
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

            ModelManager.OnPartChanged += async (sender) =>
            {
                if (_hasChanges)
                {
                    //await ConfirmAndSaveCurrentPartAsync(); TBD
                    _hasChanges = false;
                }
                //await LoadDataVM();
            };

            Timer();
        }
        #endregion

        private async void Page3Load(object sender, RoutedEventArgs e)
        {
            //await LoadDataVM();
            //FocusSelectedItem(); // TODO a remettre dans le UX

        }

        #region Sauvegarde des données
        // Sauvegarde generale les données du modèle actif
        // TODO mettre dans la VM
        public async Task SaveActiveModelDataAsync()
        {
            try
            {
                if (!ModelValidationHelper.CheckModelActif())
                    return;

                List<ModelData> modelDataToSave = new List<ModelData>();

                foreach (var control in modelDataControl)
                {
                    int id = int.TryParse(control.IdText.Text, out int parsedId) ? parsedId : 0;

                    // Récupération depuis la base de données de l'enregistrement existant
                    var existing = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(id);

                    if (string.IsNullOrEmpty(control.IdText.Text))
                        continue;
                    _isInternalChange = true;

                    // ===== récupération des Part =====
                    //var extremitePart = ViewModel.MainVM.Parts.FirstOrDefault(p => p.Id == control.ExtremitePartId);
                    var originePart = control.Part2Text.SelectedItem as Part;

                    modelDataToSave.Add(new ModelData
                    {
                        Id = id,
                        Model = control.NamePoText.Text,

                        // Relations via Id
                        //ExtremitePartId = extremitePart.Id,
                        OriginePartId = originePart.Id,

                        //Extremite = control.Part1Text.Text,
                        //Origine = control.Part2Text.Text,

                        TolExtr = control.CheckBoxPart1.IsChecked == true ? existing?.TolExtr ?? 0 : ConversionHelper.TryParseInvariant(control.Tol1Text.Text),
                        TolInt = control.CheckBoxTolInt.IsChecked == true ? existing?.TolInt ?? 0 : ConversionHelper.TryParseInvariant(control.TolIntText.Text),
                        TolOri = control.CheckBoxPart2.IsChecked == true ? existing?.TolOri ?? 0 : ConversionHelper.TryParseInvariant(control.Tol2Text.Text),

                        CoordX = ConversionHelper.TryParseInvariant(control.PointXText.Text),
                        CoordY = ConversionHelper.TryParseInvariant(control.PointYText.Text),
                        CoordZ = ConversionHelper.TryParseInvariant(control.PointZText.Text),
                        CoordU = ConversionHelper.TryParseInvariant(control.DirectionXText.Text),
                        CoordV = ConversionHelper.TryParseInvariant(control.DirectionYText.Text),
                        CoordW = ConversionHelper.TryParseInvariant(control.DirectionZText.Text),


                        DescriptionTolOri = control.CheckBoxPart2.IsChecked == true ? existing?.DescriptionTolOri : control.descriptionPart2.Text,
                        DescriptionTolInt = control.CheckBoxTolInt.IsChecked == true ? existing?.DescriptionTolInt : control.descriptionPartInt.Text,
                        DescriptionTolExtre = control.CheckBoxPart1.IsChecked == true ? existing?.DescriptionTolExtre : control.descriptionPart1.Text,

                        Commentaire = control.CommentText.Text,

                        NameTolOri = control.CheckBoxPart2.IsChecked == true ? existing?.NameTolOri : control.NameTol2Part2.Text,
                        NameTolInt = control.CheckBoxTolInt.IsChecked == true ? existing?.NameTolInt : control.NameTolInt.Text,
                        NameTolExtre = control.CheckBoxPart1.IsChecked == true ? existing?.NameTolExtre : control.NameTol1Part1.Text,

                        CheckBoxOri = control.CheckBoxPart2.IsChecked ?? false,
                        CheckBoxInt = control.CheckBoxTolInt.IsChecked ?? false,
                        CheckBoxExtre = control.CheckBoxPart1.IsChecked ?? false,

                        IdTolExtre = int.TryParse(control.IdTol1.Text, out int idtol1) ? idtol1 : 0,
                        IdTolInt = int.TryParse(control.IdTolInt.Text, out int idtolint) ? idtolint : 0,
                        IdTolOri = int.TryParse(control.IdTol2.Text, out int idtol2) ? idtol2 : 0

                    });
                }
                ;
                _isInternalChange = false;


                foreach (var data in modelDataToSave)
                {
                    await DatabaseService.ActiveInstance.UpdateModelDataAsync(data);
                }
                _hasChanges = false;
                //MessageBox.Show("Données sauvegardées avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur s'est produite lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 
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

        #endregion


        #region TreeView UI - Changement utilisateur
        // TO DO remettre a jour cette fonctionnalité
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
                await SaveActiveModelDataAsync(); // 
                _hasChanges = false; // Reinitialisation
                return true; // continuer l’action appelante
            }
            else if (result == MessageBoxResult.No)
            {
                _hasChanges = false; // Reinitialisation
                return true; // continuer sans sauvegarde
            }
            else // Cancel
            {
                return false; // annuler l’action appelante
            }
        }

        // Fonction qui définie _hasChanges = true SSI c'est une modification de l'utilisateur 
        private void AttachChangeHandlers(PanelData control)
        {
            foreach (var child in UIHelper.FindVisualChildren<TextBox>(control))
            {
                child.TextChanged += (s, e) =>
                {
                    if (!_isInternalChange)
                        _hasChanges = true;
                };
            }

            foreach (var child in UIHelper.FindVisualChildren<ComboBox>(control))
            {
                child.SelectionChanged += (s, e) =>
                {
                    if (!_isInternalChange)
                        _hasChanges = true;
                };
            }

            foreach (var child in UIHelper.FindVisualChildren<CheckBox>(control))
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
        //private static IEnumerable<T> UIHelper.FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        //{
        //    if (depObj != null)
        //    {
        //        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        //        {
        //            DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
        //            if (child is T t)
        //            {
        //                yield return t;
        //            }

        //            foreach (T childOfChild in UIHelper.FindVisualChildren<T>(child))
        //            {
        //                yield return childOfChild;
        //            }
        //        }
        //    }
        //}
        #endregion

        #region Fonctions effacer UI et effacer total
        //  Effacer toutes les valeurs de l'UI PanelsModeler
        private void ClearAllUIPanels(List<PanelData> allPanels)
        {
            foreach (var panel in allPanels)
            {
                ErasePanelValues(panel, false);  // Effacer les valeurs pour chaque panneau
            }
        }

        //  Effacer TOUTES les valeurs de PanelsModeler
        public void ClearAllPanels(List<PanelData> allPanels)
        {
            foreach (var panel in allPanels)
            {
                ErasePanelValues(panel, true); // Effacer les valeurs pour chaque panneau
            }
        }

        //Suppresion des valeurs pour l'utilisateur - Attention PART1 et ID ne doivent pas l'etre
        //TRUE => eraseall , FALSE=> eralseUI value (not id)
        private async Task ErasePanelValues(PanelData panel, bool eraseAll)
        {
            _isInternalChange = true;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (eraseAll)
                {
                    panel.NamePoText.Clear();
                    panel.Part1Text.Text = string.Empty;
                    panel.IdText.Clear();
                    panel.IdTol1.Clear();
                    panel.IdTolInt.Clear();
                    panel.IdTol2.Clear();
                }

                panel.Tol1Text.Clear();
                panel.Part2Text.SelectedIndex = -1;
                panel.Part2Text.Text = string.Empty;
                panel.Tol2Text.Clear();
                panel.TolIntText.Clear();
                panel.PointXText.Clear();
                panel.PointYText.Clear();
                panel.PointZText.Clear();
                panel.DirectionXText.Clear();
                panel.DirectionYText.Clear();
                panel.DirectionZText.Clear();
                panel.descriptionPart1.Clear();
                panel.descriptionPart2.Clear();
                panel.descriptionPartInt.Clear();
                panel.NameTol1Part1.Clear();
                panel.NameTolInt.Clear();
                panel.NameTol2Part2.Clear();
            });
            _isInternalChange = false;
        }


        //Bouton UI pour effacer les valeurs de tous les panels modeler
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Effacer les valeurs de tous les panels
            ClearAllUIPanels(modelDataControl); // modelDataControl est une liste de panelmodeler

        }

        #endregion

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

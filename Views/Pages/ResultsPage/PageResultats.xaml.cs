using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Toltech.ComputeEngine.Contracts;
using TOLTECH_APPLICATION.FrontEnd.Controls;
using TOLTECH_APPLICATION.FrontEnd.Interfaces;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Services.Dialog;
using TOLTECH_APPLICATION.ToltechCalculation.Helpers;
using TOLTECH_APPLICATION.ToltechCalculation.Resux;
using TOLTECH_APPLICATION.ViewModels;

namespace TOLTECH_APPLICATION.Views
{
    public partial class PageResultats : UserControl
    {
        #region Champs privés
        private readonly ComputeValidationService _computeValidationService;
        private IComputeEngine _computeEngine;
        private ResuxSerializer _resuxSerializer;
        private INotificationService _notificationService;
        private IDialogService _dialog;

        private ResultsViewModel ResultsVM; // TODO pas joli l'appel
        private MainViewModel mainVM; // TODO pas joli l'appel
        public PageResultats()
        {
            InitializeComponent();
            AfficherExigencesDansListBox();

            // On attend que DataContext soit défini
            // Dès que le DataContext est assigné par le DataTemplate
            this.DataContextChanged += (s, e) =>
            {
                if (DataContext is ResultsViewModel vm)
                {
                    _dialog = App.DialogService;
                    _notificationService = App.NotificationService;
                }
            };

            if (DataContext is ResultsViewModel vm)
            {
                ResultsVM = vm;
            }

            _computeEngine = App.MainVM.ComputeEngine; // TODO enlever couplage en passant par la VM avec injection de Icompute
            _computeValidationService = App.MainVM.ComputeValidationService; 

            _resuxSerializer = new ResuxSerializer();
            this.Loaded += PageResultas_Load;

            BarChartControl1.ParentPage = this;
            CrossTableControl1.ParentPage = this;
            BchartReqsControl.ParentPage = this;

        }
        #endregion

        #region Méthodes d'initialisation
        private async void PageResultas_Load(object sender, RoutedEventArgs e)
        {
            AfficherExigencesDansListBox();
            CbAllReqs.IsChecked = false;
        }

        #endregion

        #region Zone de calculs - Méthode Cascade Matricielle
        // Récuperation des req ID et Name
        private async Task AfficherExigencesDansListBox()
        {
            if (ModelManager.ModelActif == null) return;
            try
            {
                var exigences = await DatabaseService.ActiveInstance.GetAllRequirementsAsync();
                RequirementsList.SelectedItems.Clear();

                if (exigences != null && exigences.Any())
                {
                    RequirementsList.ItemsSource = exigences;
                    RequirementsList.DisplayMemberPath = "NameReq"; // Affiche le champ "Model" dans la ListBox
                    RequirementsList.SelectedItems.Clear();
                }
                else
                {
                    RequirementsList.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des exigences : {ex.Message}",
                                "Erreur",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        #endregion

        #region Zone de calculs -Méthode OneMatrix

        private async Task LaunchStudyV2()
        {
            var stopwatch = Stopwatch.StartNew();

            var modelData = await DatabaseService.ActiveInstance.GetAllModelDataAsync();
            var computeModelData = ComputeMapper.ToComputeModelData(modelData);

            var exigencesSelectionnees = RequirementsList.SelectedItems.Cast<Requirements>().ToList();
            var computeRequirements = ComputeMapper.ToComputeRequirements(exigencesSelectionnees);


            if (!await _computeValidationService.ValidationCalculsAsync(computeModelData, computeRequirements))
                return;

            var loader = new LoadingWindow();
            loader.Show();

            IProgress<string> progress = new Progress<string>(message =>
            {
                loader.UpdateMessage(message);
            });

            int total = exigencesSelectionnees.Count;
            int index = 0;
            ComputeResult AllResults = new ComputeResult();

            try
            {
                Debug.WriteLine("Initialisation de MainComputeService...");

                var fixPart = await DatabaseService.ActiveInstance.GetFixedPartAsync();
                var request = new ComputeRequest
                {
                    ModelData = modelData.Select(m => new ComputeModelData
                    {
                        Id = m.Id,
                        OriginePartId = m.OriginePartId,
                        ExtremitePartId = m.ExtremitePartId,
                        Active = m.Active,

                        CoordX = m.CoordX,
                        CoordY = m.CoordY,
                        CoordZ = m.CoordZ,

                        CoordU = m.CoordU,
                        CoordV = m.CoordV,
                        CoordW = m.CoordW,

                        TolOri = m.TolOri,
                        TolInt = m.TolInt,
                        TolExtr = m.TolExtr,

                        IdTolOri = m.IdTolOri,
                        IdTolInt = m.IdTolInt,
                        IdTolExtre = m.IdTolExtre,

                        Model = m.Model
                    }).ToList(),

                    Requirements = exigencesSelectionnees.Select(r => new ComputeRequirement
                    {
                        Id_req = r.Id_req,
                        PartReq1Id = r.PartReq1Id,
                        PartReq2Id = r.PartReq2Id,
                        CoordU = r.CoordU,
                        CoordV = r.CoordV,
                        CoordW = r.CoordW
                    }).ToList(),

                    IdFixPart = fixPart.Id
                };

                AllResults = await _computeEngine.ComputeAsync(request);

                await _resuxSerializer.WriteResultsToFileV2Async(AllResults.Results, exigencesSelectionnees);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    _notificationService.ShowNotifAsync("Fin des résultats", false);

                    await BarChartControl1.LoadRequirementsToComboBox(ModelManager.FilePathResx);
                    await CrossTableControl1.GenerateAndDisplayCrossTable(ModelManager.FilePathResx);
                    BchartReqsControl.LoadBartChartReqs();

                });
                loader.Close();
                Debug.WriteLine("Fin de l'étude des Exigences !");

                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                //MessageBox.Show($"Temps de calcul total : {elapsedMs} ms");

            }
            catch (Exception ex)
            {
                loader.Close();
            }
        }

        private async void LauchComputeV2_Click(object sender, RoutedEventArgs e)
        {
            var exigencesSelectionnees = RequirementsList.SelectedItems.Cast<Requirements>().ToList();
            if (!exigencesSelectionnees.Any())
            {
                MessageBox.Show("Veuillez sélectionner au moins une exigence.", "Toltech information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await LaunchStudyV2();
        }
        #endregion

        #region Chargement de l'interface via RESUX et fonction IMP/EXP FICHIER

        // Fonction UI pour importer RESUX
        private async void ImportResx_Click(object sender, RoutedEventArgs e)
        {
            await ImportResxBack();
            //await BarChartControl1.LoadRequirementsToComboBox(ModelManager.FilePathResx);
            await CrossTableControl1.GenerateAndDisplayCrossTable(ModelManager.FilePathResx);
            BchartReqsControl.LoadBartChartReqs();
        }

        //Fonction INTERNE pour importer RESUX
        private async Task ImportResxBack()
        {
            string _selectedFilePath;
            string resultsPath = ModelManager.GetResultsPath();

            var dlg = new OpenFileDialog
            {
                Title = "Sélectionnez un fichier de résultats",
                Filter = "Fichiers texte (*.resux)|*.resux|Tous les fichiers (*.*)|*.*",
                InitialDirectory = resultsPath
            };

            if (dlg.ShowDialog() == true)
            {
                _selectedFilePath = dlg.FileName;
                ModelManager.FilePathResx = _selectedFilePath;
            }
        }

        //Exporter un fichier resultats
        private void ExportResx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Récupération du chemin source du fichier à exporter
                string sourceFilePath = ModelManager.FilePathResx;
                if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    MessageBox.Show("Le fichier source est introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Dossier par défaut pour la sauvegarde
                string initialDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TolTech\\Results"
                );
                Directory.CreateDirectory(initialDirectory);

                string nameModel = System.IO.Path.GetFileNameWithoutExtension(ModelManager.ModelActif);
                string extension = ".resux";

                // Boîte de dialogue pour choisir l’emplacement de sauvegarde
                var dlg = new SaveFileDialog
                {
                    Title = "Enregistrer les résultats sous",
                    Filter = $"{extension.ToUpper()} files (*{extension})|*{extension}|Tous les fichiers (*.*)|*.*",
                    InitialDirectory = initialDirectory,
                    FileName = $"Resultats_{nameModel}{extension}",
                    AddExtension = false,
                    OverwritePrompt = true
                };

                bool? result = dlg.ShowDialog();
                if (result == true)
                {
                    string destinationFilePath = dlg.FileName;

                    // Copie du fichier source vers la destination sélectionnée
                    File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
                    MessageBox.Show("Exportation réussie.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exportation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Fonctions UI 

        private Dictionary<string, GridLength> originalRowHeights = new();
        private HashSet<string> minimizedRows = new();

        private void ToggleRowHeight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string rowName)
                return;

            if (FindName(rowName) is not RowDefinition targetRow)
                return;

            // Vérifier si la row est minimisée (40px)
            bool isMinimized = targetRow.Height.IsAbsolute && targetRow.Height.Value <= 40;

            if (isMinimized)
            {
                // Expand = 1*
                targetRow.Height = new GridLength(1, GridUnitType.Star);
                SetButtonIcon(button, "\uE77A"); // expand
            }
            else
            {
                // Minimize = 40px
                targetRow.Height = new GridLength(40, GridUnitType.Pixel);
                SetButtonIcon(button, "\uE718"); // collapse
            }
        }

        private void SetButtonIcon(Button button, string icon)
        {
            button.Content = icon;
            //button.FontFamily = new FontFamily("Segoe MDL2 Assets"); // TODO vérifier si nécessaire
        }

        private void MenuActivateV1_Click(object sender, RoutedEventArgs e)
        {
            // On met à jour le texte du bouton
            BtnCompute.Content = "Lancer V1";

            // On change dynamiquement l'événement associé
            BtnCompute.Click -= LauchComputeV2_Click; // retire V2 si présent
        }

        // Active le mode V2
        private void MenuActivateV2_Click(object sender, RoutedEventArgs e)
        {
            BtnCompute.Content = "Lancer V2";

            BtnCompute.Click -= LauchComputeV2_Click;
            BtnCompute.Click += LauchComputeV2_Click;
        }

        // Largeur minimale du panneau droit quand collapsé
        private readonly GridLength _collapsedRightWidth = new GridLength(50);
        // Largeur du panneau droit quand expandé
        private readonly GridLength _expandedRightWidth = new GridLength(1.2, GridUnitType.Star);

        // Largeur de la colonne gauche
        private readonly GridLength _leftExpandedWidth = new GridLength(3, GridUnitType.Star);
        private readonly GridLength _leftCollapsedWidth = new GridLength(1, GridUnitType.Star);

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            RightColumn.Width = _collapsedRightWidth;   // réduire panneau droit
            LeftColumn.Width = _leftCollapsedWidth;     // agrandir colonne gauche proportionnellement
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            RightColumn.Width = _expandedRightWidth;    // largeur normale panneau droit
            LeftColumn.Width = _leftExpandedWidth;      // largeur normale colonne gauche
        }


        private bool _isUpdatingSelection = false;
        private void CbAllReqs_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            _isUpdatingSelection = true;
            if (CbAllReqs.IsChecked == true)
            {
                // Sélectionner tous les éléments
                RequirementsList.SelectAll();
            }
            else
            {
                // Désélectionner tous les éléments
                RequirementsList.UnselectAll();
            }
            _isUpdatingSelection = false;
        }

        private void RequirementsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return; // Ignore si on met à jour depuis la CheckBox

            // Dès qu'un item est sélectionné/désélectionné manuellement, décocher CbAllReqs
            if (CbAllReqs.IsChecked == true)
            {
                CbAllReqs.IsChecked = false;
            }
        }


        #endregion
    }
}

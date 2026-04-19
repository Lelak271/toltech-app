using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TOLTECH_APPLICATION.Front;
using TOLTECH_APPLICATION.ViewModels;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;


namespace TOLTECH_APPLICATION.FrontEnd.Controls
{
    public partial class PanelReqs : UserControl
    {
        private string currentContextTarget = string.Empty; // "Part1" ou "Part2"

        #region Constructeur
        public PanelReqs()
        {
            InitializeComponent();
            //Debug.WriteLine("PANEL CREATED -> " + Guid.NewGuid());

            #region Click droit

            ContextMenu lienDbContextMenu = new ContextMenu();

            MenuItem lienDbMenuItem = new MenuItem
            {
                Header = "Lien DB"
            };
            lienDbMenuItem.Click += LienDbMenuItem_Click;

            lienDbContextMenu.Items.Add(lienDbMenuItem);

            // Affecter le même menu à la zone Pièce 1
            NameTol1Part1.ContextMenu = lienDbContextMenu;
            descriptionPart1.ContextMenu = lienDbContextMenu;
            TolPart1.ContextMenu = lienDbContextMenu;

            // Gérer l'état activé/désactivé avant l'ouverture

            NameTol1Part1.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part1";
                lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            };
            descriptionPart1.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part1";
                lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            };
            TolPart1.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part1";
                lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            };





            // Affecter le même menu à la zone Pièce 2
            NameTol1Part2.ContextMenu = lienDbContextMenu;
            descriptionPart2.ContextMenu = lienDbContextMenu;
            TolPart2.ContextMenu = lienDbContextMenu;

            // Gérer l'état activé/désactivé avant l'ouverture
            NameTol1Part2.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part2";
                lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            };
            descriptionPart2.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part2";
                lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            };
            TolPart2.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part2";
                lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            };
            #endregion
        }

        public RequirementsViewModel ParentViewModel
        {
            get => (RequirementsViewModel)GetValue(ParentViewModelProperty);
            set => SetValue(ParentViewModelProperty, value);
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.Register(
                nameof(ParentViewModel),
                typeof(RequirementsViewModel),
                typeof(PanelReqs),
                new PropertyMetadata(null));


        // Création d'une DependencyProperty pour faciliter le binding
        public static readonly DependencyProperty RequirementProperty =
            DependencyProperty.Register(
                nameof(Requirement),
                typeof(Requirements),
                typeof(PanelReqs),
                 new PropertyMetadata(null));

        public Requirements Requirement
        {
            get => (Requirements)GetValue(RequirementProperty);
            set => SetValue(RequirementProperty, value);
        }

        #endregion

        #region Gestion des Tolerances DB

        //#region CheckBox 1
        private async void ToggleCheckBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            //// Rendre les TextBox modifiables
            //NameTol1Part1.IsReadOnly = false;
            //descriptionPart1.IsReadOnly = false;
            //TolPart1.IsReadOnly = false;

            //if (!int.TryParse(IdReqText.Text, out int idReq))
            //    return;

            //var datareq = await _databaseService.GetReqsByIdAsync(idReq);
            //// Effacer le contenu des TextBox
            //NameTol1Part1.Text = datareq.NameTolOri;
            //descriptionPart1.Text = datareq.Description1;
            //TolPart1.Text = datareq.tol1.ToString();
        }

        private async void ToggleCheckBox1_Checked(object sender, RoutedEventArgs e)
        {
            //NameTol1Part1.Text = string.Empty;
            //descriptionPart1.Text = string.Empty;
            //TolPart1.Text = string.Empty;

            //// Rendre les TextBox en lecture seule
            //NameTol1Part1.IsReadOnly = true;
            //descriptionPart1.IsReadOnly = true;
            //TolPart1.IsReadOnly = true;

            //// Vérifier la présence d'un ID valide
            //if (int.TryParse(IdTol1.Text, out int idTol1) && idTol1 > 0)
            //{
            //    try
            //    {
            //        InitializeDataBase();
            //        var tolerance = await _databaseService.GetTolerancesByIdAsync(idTol1);
            //        if (tolerance != null)
            //        {
            //            NameTol1Part1.Text = tolerance.NameTolInt;
            //            descriptionPart1.Text = tolerance.DescriptionTolInt;
            //            TolPart1.Text = tolerance.tolInt.ToString();
            //        }
            //        else
            //        {
            //            MessageBox.Show($"La tolérance Int provenant de la Base de Données n'a pas pu être récuperée sur la ponctuelle. Veuillez séléctionner une nouvelle tolérance dans la DB.");
            //            //CheckBoxTolInt.IsChecked = false;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Erreur lors du chargement de la tolérance : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}


        }

        //#endregion

        //#region CheckBox 2

        private async void ToggleCheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            //NameTol1Part2.Text = string.Empty;
            //descriptionPart2.Text = string.Empty;
            //TolPart2.Text = string.Empty;

            //// Rendre les TextBox modifiables
            //NameTol1Part2.IsReadOnly = false;
            //descriptionPart2.IsReadOnly = false;
            //TolPart2.IsReadOnly = false;

            //if (!int.TryParse(IdReqText.Text, out int idReq))
            //    return;

            //var datareq = await _databaseService.GetReqsByIdAsync(idReq);
            //// Effacer le contenu des TextBox
            //NameTol1Part2.Text = datareq.NameTolExtre;
            //descriptionPart2.Text = datareq.Description2;
            //TolPart2.Text = datareq.tol2.ToString();
        }

        private async void ToggleCheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            //// Rendre les TextBox en lecture seule
            //NameTol1Part2.IsReadOnly = true;
            //descriptionPart2.IsReadOnly = true;
            //TolPart2.IsReadOnly = true;

            //// Vérifier la présence d'un ID valide
            //if (int.TryParse(IdTol2.Text, out int idTol2) && idTol2 > 0)
            //{
            //    try
            //    {
            //        InitializeDataBase();
            //        var tolerance = await _databaseService.GetTolerancesByIdAsync(idTol2);
            //        if (tolerance != null)
            //        {
            //            NameTol1Part2.Text = tolerance.NameTolInt;
            //            descriptionPart2.Text = tolerance.DescriptionTolInt;
            //            TolPart2.Text = tolerance.tolInt.ToString();
            //        }
            //        else
            //        {
            //            MessageBox.Show($"La tolérance Int provenant de la Base de Données n'a pas pu être récupérée sur la ponctuelle. Veuillez sélectionner une nouvelle tolérance dans la DB.");
            //            //CheckBoxTolInt.IsChecked = false;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Erreur lors du chargement de la tolérance : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }


        //#endregion

        #region Fonctions principales
        //Fonction appelée par le "Click-droit" qui ouvre une fenetre 
        private void LienDbMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var floatingDbWindow = new SelectFloatingPanelDB();
            //floatingDbWindow.ToleranceSelected += FloatingDbWindow_ToleranceSelected;

            // Correction : Utilisation de Window.GetWindow pour définir le propriétaire
            var ownerWindow = Window.GetWindow(this);
            if (ownerWindow != null)
            {
                floatingDbWindow.Owner = ownerWindow;
            }

            floatingDbWindow.ShowDialog();
        }

        // Fonction qui capte les output du FloatingDbWindow_ToleranceSelected pour les réattibué au front
        //private void FloatingDbWindow_ToleranceSelected(object sender, SelectFloatingPanelDB.ToleranceSelectedEventArgs e)
        //{

        //    if (currentContextTarget == "Part1")
        //    {
        //        this.IdTol1.Text = e.ToleranceID.ToString();
        //        this.NameTol1Part1.Text = e.ToleranceName;
        //        this.descriptionPart1.Text = e.ToleranceDescription;
        //        this.TolPart1.Text = e.ToleranceValue;
        //    }
        //    else if (currentContextTarget == "Part2")
        //    {
        //        this.IdTol2.Text = e.ToleranceID.ToString();
        //        this.NameTol1Part2.Text = e.ToleranceName;
        //        this.descriptionPart2.Text = e.ToleranceDescription;
        //        this.TolPart2.Text = e.ToleranceValue;
        //    }

        //}

        #endregion

        #endregion


    }
}

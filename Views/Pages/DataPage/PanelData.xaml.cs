using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TOLTECH_APPLICATION.Behaviors;
using TOLTECH_APPLICATION.Front;
using TOLTECH_APPLICATION.ViewModels;
using TOLTECH_APPLICATION.Models;
using TOLTECH_APPLICATION.Services;

namespace TOLTECH_APPLICATION.FrontEnd.Controls
{
    public sealed partial class PanelData : UserControl
    {
        private string currentContextTarget = string.Empty; // "Part1" ou "Part2"

        #region Constructeur
        public PanelData()
        {
            InitializeComponent();

            #region Click droit

            ContextMenu lienDbContextMenu = new ContextMenu();

            MenuItem lienDbMenuItem = new MenuItem
            {
                Header = "Lien DB"
            };
            lienDbMenuItem.Click += LienDbMenuItem_Click;

            lienDbContextMenu.Items.Add(lienDbMenuItem);

            // === Zone Pièce 1 ===
            NameTol1Part1.ContextMenu = lienDbContextMenu;
            descriptionPart1.ContextMenu = lienDbContextMenu;
            Tol1Text.ContextMenu = lienDbContextMenu;

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
            Tol1Text.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part1";
                lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            };

            // === Zone Int ===
            NameTolInt.ContextMenu = lienDbContextMenu;
            descriptionPartInt.ContextMenu = lienDbContextMenu;
            TolIntText.ContextMenu = lienDbContextMenu;

            NameTolInt.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Int";
                lienDbMenuItem.IsEnabled = CheckBoxTolInt.IsChecked == true;
            };
            descriptionPartInt.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Int";
                lienDbMenuItem.IsEnabled = CheckBoxTolInt.IsChecked == true;
            };
            TolIntText.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Int";
                lienDbMenuItem.IsEnabled = CheckBoxTolInt.IsChecked == true;
            };

            // === Zone Pièce 2 ===
            NameTol2Part2.ContextMenu = lienDbContextMenu;
            descriptionPart2.ContextMenu = lienDbContextMenu;
            Tol2Text.ContextMenu = lienDbContextMenu;

            NameTol2Part2.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part2";
                lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            };
            descriptionPart2.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part2";
                lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            };
            Tol2Text.ContextMenuOpening += (s, e) =>
            {
                currentContextTarget = "Part2";
                lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            };

            #endregion

        }

        public DatasViewModel ParentViewModel
        {
            get => (DatasViewModel)GetValue(ParentViewModelProperty);
            set => SetValue(ParentViewModelProperty, value);
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.Register(
                nameof(ParentViewModel),
                typeof(DatasViewModel),
                typeof(PanelData),
                new PropertyMetadata(null));


        // Création d'une DependencyProperty pour faciliter le binding
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(ModelData),
                typeof(PanelData),
                 new PropertyMetadata(null));

        public ModelData Data
        {
            get => (ModelData)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }


        #endregion

        #region CheckBox INT

        private async void ToggleCheckBoxInt_Unchecked(object sender, RoutedEventArgs e)
        {
            //NameTolInt.Text = string.Empty;
            //descriptionPartInt.Text = string.Empty;
            //TolIntText.Text = string.Empty;

            //// Rendre les TextBox modifiables
            //NameTolInt.IsReadOnly = false;
            //descriptionPartInt.IsReadOnly = false;
            //TolIntText.IsReadOnly = false;

            //if (!int.TryParse(IdText.Text, out int id) || id <= 0)
            //    return;

            //var data = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(id);

            //if (data != null)
            //{
            //    // Mise à jour des champs uniquement si les données sont valides
            //    NameTolInt.Text = data.NameTolInt;
            //    descriptionPartInt.Text = data.DescriptionTolInt;
            //    TolIntText.Text = data.TolInt.ToString();
            //}
        }

        private async void ToggleCheckBoxInt_Checked(object sender, RoutedEventArgs e)
        {
            //NameTolInt.Text = string.Empty;
            //descriptionPartInt.Text = string.Empty;
            //TolIntText.Text = string.Empty;

            //// Rendre les TextBox en lecture seule
            //NameTolInt.IsReadOnly = true;
            //descriptionPartInt.IsReadOnly = true;
            //TolIntText.IsReadOnly = true;

            //// Vérifier la présence d'un ID valide
            //if (int.TryParse(IdTolInt.Text, out int idTolInt) && idTolInt > 0)
            //{
            //    try
            //    {
            //        var tolerance = await DatabaseService.ActiveInstance.GetTolerancesByIdAsync(idTolInt);
            //        if (tolerance != null)
            //        {
            //            NameTolInt.Text = tolerance.NameTolInt;
            //            descriptionPartInt.Text = tolerance.DescriptionTolInt;
            //            TolIntText.Text = tolerance.tolInt.ToString();
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
        #endregion

        #region CheckBox 1 Extremite
        private async void ToggleCheckBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            //NameTol1Part1.Text = string.Empty;
            //descriptionPart1.Text = string.Empty;
            //Tol1Text.Text = string.Empty;

            //// Rendre les TextBox modifiables
            //NameTol1Part1.IsReadOnly = false;
            //descriptionPart1.IsReadOnly = false;
            //Tol1Text.IsReadOnly = false;


            //if (!int.TryParse(IdText.Text, out int id))
            //    return;

            //var data = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(id);
            //// Effacer le contenu des TextBox
            //NameTol1Part1.Text = data.NameTolExtre;
            //descriptionPart1.Text = data.DescriptionTolExtre;
            //Tol1Text.Text = data.TolExtr.ToString();
        }

        private async void ToggleCheckBox1_Checked(object sender, RoutedEventArgs e)
        {

            //NameTol1Part1.Text = string.Empty;
            //descriptionPart1.Text = string.Empty;
            //Tol1Text.Text = string.Empty;

            //// Rendre les TextBox en lecture seule
            //NameTol1Part1.IsReadOnly = true;
            //descriptionPart1.IsReadOnly = true;
            //Tol1Text.IsReadOnly = true;

            //// Vérifier la présence d'un ID valide
            //if (int.TryParse(IdTol1.Text, out int idTol1) && idTol1 > 0)
            //{
            //    try
            //    {
            //        var tolerance = await DatabaseService.ActiveInstance.GetTolerancesByIdAsync(idTol1);
            //        if (tolerance != null)
            //        {
            //            NameTol1Part1.Text = tolerance.NameTolInt;
            //            descriptionPart1.Text = tolerance.DescriptionTolInt;
            //            Tol1Text.Text = tolerance.tolInt.ToString();
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

        #endregion

        #region CheckBox 2 Origine

        private async void ToggleCheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            //NameTol2Part2.Text = string.Empty;
            //descriptionPart2.Text = string.Empty;
            //Tol2Text.Text = string.Empty;

            //// Rendre les TextBox modifiables
            //NameTol2Part2.IsReadOnly = false;
            //descriptionPart2.IsReadOnly = false;
            //Tol2Text.IsReadOnly = false;


            //if (!int.TryParse(IdText.Text, out int id))
            //    return;

            //var data = await DatabaseService.ActiveInstance.GetModelDataByIdAsync(id);
            //// Effacer le contenu des TextBox
            //NameTol2Part2.Text = data.NameTolOri;
            //descriptionPart2.Text = data.DescriptionTolOri;
            //Tol2Text.Text = data.TolOri.ToString();
        }

        private async void ToggleCheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            //NameTol2Part2.Text = string.Empty;
            //descriptionPart2.Text = string.Empty;
            //Tol2Text.Text = string.Empty;

            //// Rendre les TextBox en lecture seule
            //NameTol2Part2.IsReadOnly = true;
            //descriptionPart2.IsReadOnly = true;
            //Tol2Text.IsReadOnly = true;

            //// Vérifier la présence d'un ID valide
            //if (int.TryParse(IdTol2.Text, out int idTol2) && idTol2 > 0)
            //{
            //    try
            //    {
            //        var tolerance = await DatabaseService.ActiveInstance.GetTolerancesByIdAsync(idTol2);
            //        if (tolerance != null)
            //        {
            //            NameTol2Part2.Text = tolerance.NameTolInt;
            //            descriptionPart2.Text = tolerance.DescriptionTolInt;
            //            Tol2Text.Text = tolerance.tolInt.ToString();
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
        #endregion

        #region Fonctions principales
        private void LienDbMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var floatingDbWindow = new SelectFloatingPanelDB();
            floatingDbWindow.ToleranceSelected += FloatingDbWindow_ToleranceSelected;

            // Correction : Utilisation de Window.GetWindow pour définir le propriétaire
            var ownerWindow = Window.GetWindow(this);
            if (ownerWindow != null)
            {
                floatingDbWindow.Owner = ownerWindow;
            }

            floatingDbWindow.ShowDialog();
        }

        private void FloatingDbWindow_ToleranceSelected(object sender, SelectFloatingPanelDB.ToleranceSelectedEventArgs e)
        {


            if (currentContextTarget == "Part1")
            {
                this.IdTol1.Text = e.ToleranceID.ToString();
                this.NameTol1Part1.Text = e.ToleranceName;
                this.descriptionPart1.Text = e.ToleranceDescription;
                this.Tol1Text.Text = e.ToleranceValue;
            }
            else if (currentContextTarget == "Part2")
            {
                this.IdTol2.Text = e.ToleranceID.ToString();
                this.NameTol2Part2.Text = e.ToleranceName;
                this.descriptionPart2.Text = e.ToleranceDescription;
                this.Tol2Text.Text = e.ToleranceValue;
            }
            else if (currentContextTarget == "Int")
            {
                this.IdTolInt.Text = e.ToleranceID.ToString();
                this.NameTolInt.Text = e.ToleranceName;
                this.descriptionPartInt.Text = e.ToleranceDescription;
                this.TolIntText.Text = e.ToleranceValue;
            }
        }

        #endregion

    }
}

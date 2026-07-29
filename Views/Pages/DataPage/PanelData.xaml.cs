using System.Windows;
using System.Windows.Controls;
using Toltech.App.Front;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.ViewModels;
using static Toltech.App.Models.NodesDefinition;
using static Toltech.App.Services.EventsManager;

namespace Toltech.App.FrontEnd.Controls
{
    /// <summary>
    /// Contrôle affichant une ligne ModelData dans la liste. 
    /// </summary>
    public sealed partial class PanelData : UserControl
    {
        private string currentContextTarget = string.Empty; // "Part1" ou "Part2"

        #region Constructeur
        public PanelData()
        {
            InitializeComponent();
            EventsManager.NodeChanged += OnNodeChangedAsync;
            EventsManager.PartCrud += OnPartCrudAsync;

            #region Click droit

            ContextMenu lienDbContextMenu = new ContextMenu();

            MenuItem lienDbMenuItem = new MenuItem
            {
                Header = "Lien DB"
            };
            lienDbMenuItem.Click += LienDbMenuItem_Click;

            lienDbContextMenu.Items.Add(lienDbMenuItem);

            // === Zone Pièce 1 ===
            //NameTol1Part1.ContextMenu = lienDbContextMenu;
            //descriptionPart1.ContextMenu = lienDbContextMenu;
            //Tol1Text.ContextMenu = lienDbContextMenu;

            //NameTol1Part1.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Part1";
            //    lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            //};
            //descriptionPart1.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Part1";
            //    lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            //};
            //Tol1Text.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Part1";
            //    lienDbMenuItem.IsEnabled = CheckBoxPart1.IsChecked == true;
            //};

            //// === Zone Int ===
            //NameTolInt.ContextMenu = lienDbContextMenu;
            //descriptionPartInt.ContextMenu = lienDbContextMenu;
            //TolIntText.ContextMenu = lienDbContextMenu;

            //NameTolInt.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Int";
            //    lienDbMenuItem.IsEnabled = CheckBoxTolInt.IsChecked == true;
            //};
            //descriptionPartInt.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Int";
            //    lienDbMenuItem.IsEnabled = CheckBoxTolInt.IsChecked == true;
            //};
            //TolIntText.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Int";
            //    lienDbMenuItem.IsEnabled = CheckBoxTolInt.IsChecked == true;
            //};

            //// === Zone Pièce 2 ===
            //NameTol2Part2.ContextMenu = lienDbContextMenu;
            //descriptionPart2.ContextMenu = lienDbContextMenu;
            //Tol2Text.ContextMenu = lienDbContextMenu;

            //NameTol2Part2.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Part2";
            //    lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            //};
            //descriptionPart2.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Part2";
            //    lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            //};
            //Tol2Text.ContextMenuOpening += (s, e) =>
            //{
            //    currentContextTarget = "Part2";
            //    lienDbMenuItem.IsEnabled = CheckBoxPart2.IsChecked == true;
            //};

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

        #region Events

        private Task OnNodeChangedAsync(NodeChangedEvent e)
        {
            if (e.Type != NodeType.PartNode) return Task.CompletedTask;

            return OnPartCrudAsync(new PartCrudEvent
            {
                Operation = e.Operation,
                Source = EventSource.Tree,
                EntityId = e.LinkedOriginalId,
                Entity = e.Operation == CrudOperation.Updated
                            ? new Part { Id = e.LinkedOriginalId, NamePart = e.NewName }
                            : null
            });
        }

        private Task OnPartCrudAsync(PartCrudEvent e)
        {
            if (e.Operation != CrudOperation.Updated) return Task.CompletedTask;

            var updatedParts = e.Entities?.Any() == true
                ? e.Entities
                : e.Entity != null ? new List<Part> { e.Entity } : null;

            if (updatedParts == null) return Task.CompletedTask;
            if (DataContext is not ModelData data) return Task.CompletedTask;

            App.Current.Dispatcher.Invoke(() =>
            {
                var extremite = updatedParts.FirstOrDefault(p => p.Id == data.ExtremitePartId);
                if (extremite != null)
                    Part1Text.Text = extremite.NamePart;

                var origine = updatedParts.FirstOrDefault(p => p.Id == data.OriginePartId);
                if (origine != null)
                    Part2Text.Text = origine.NamePart;
            });

            return Task.CompletedTask;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            EventsManager.NodeChanged -= OnNodeChangedAsync;
            EventsManager.PartCrud -= OnPartCrudAsync;
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


            //if (currentContextTarget == "Part1")
            //{
            //    this.IdTol1.Text = e.ToleranceID.ToString();
            //    this.NameTol1Part1.Text = e.ToleranceName;
            //    this.descriptionPart1.Text = e.ToleranceDescription;
            //    this.Tol1Text.Text = e.ToleranceValue;
            //}
            //else if (currentContextTarget == "Part2")
            //{
            //    this.IdTol2.Text = e.ToleranceID.ToString();
            //    this.NameTol2Part2.Text = e.ToleranceName;
            //    this.descriptionPart2.Text = e.ToleranceDescription;
            //    this.Tol2Text.Text = e.ToleranceValue;
            //}
            //else if (currentContextTarget == "Int")
            //{
            //    this.IdTolInt.Text = e.ToleranceID.ToString();
            //    this.NameTolInt.Text = e.ToleranceName;
            //    this.descriptionPartInt.Text = e.ToleranceDescription;
            //    this.TolIntText.Text = e.ToleranceValue;
            //}
        }

        #endregion

        // Méthodes pour NExtr
        public void ToggleCheckBoxNExtr_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxNExtr_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour NInt
        public void ToggleCheckBoxNInt_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxNInt_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour NOri
        public void ToggleCheckBoxNOri_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxNOri_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour T1Extr
        public void ToggleCheckBoxT1Extr_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxT1Extr_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour T1Int
        public void ToggleCheckBoxT1Int_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxT1Int_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour T1Ori
        public void ToggleCheckBoxT1Ori_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxT1Ori_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour T2Extr
        public void ToggleCheckBoxT2Extr_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxT2Extr_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour T2Int
        public void ToggleCheckBoxT2Int_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxT2Int_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour T2Ori
        public void ToggleCheckBoxT2Ori_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxT2Ori_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RnExtr
        public void ToggleCheckBoxRnExtr_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRnExtr_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RnInt
        public void ToggleCheckBoxRnInt_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRnInt_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RnOri
        public void ToggleCheckBoxRnOri_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRnOri_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RT1Extr
        public void ToggleCheckBoxRT1Extr_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRT1Extr_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RT1Int
        public void ToggleCheckBoxRT1Int_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRT1Int_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RT1Ori
        public void ToggleCheckBoxRT1Ori_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRT1Ori_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RT2Extr
        public void ToggleCheckBoxRT2Extr_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRT2Extr_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RT2Int
        public void ToggleCheckBoxRT2Int_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRT2Int_Unchecked(object sender, EventArgs e) { }

        // Méthodes pour RT2Ori
        public void ToggleCheckBoxRT2Ori_Checked(object sender, EventArgs e) { }
        public void ToggleCheckBoxRT2Ori_Unchecked(object sender, EventArgs e) { }

    }

        public class LiaisonTypeItem
        {
            public ModelData.LiaisonType Value { get; set; }
            public string Label { get; set; }
        }

        public static class LiaisonTypeProvider
        {
           public static List<LiaisonTypeItem> All { get; } = new()
            {
                new LiaisonTypeItem { Value = ModelData.LiaisonType.PointContact, Label = "Ponctuelle" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.PrismaticContact, Label = "Glissière" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.SphericalContact, Label = "Rotule" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.AnnularContact, Label = "AnnularContact" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.PlanarContact, Label = "PlanarContact" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.RevoluteContact, Label = "Pivot" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.LinearContact, Label = "Linéaire" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.CylindricalContact, Label = "Pivot Glissant" },
                new LiaisonTypeItem { Value = ModelData.LiaisonType.FixedContact, Label = "FixedContact" },
            };
        }




}

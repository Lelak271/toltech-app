using System.Windows.Controls;
using Toltech.App.FrontEnd.Controls;
using Toltech.App.Services;
using Toltech.App.Views;

namespace Toltech.App.Views
{

    // TODO : Créer une zone list de virtualizatioin UI pour fluidifier la vue des nombreux modele
    // creer plusieurs biblio
    public partial class PageModels : UserControl
    {
        private MainWindow _myfirstwindow;
        private DatabaseService _databaseservice;
        private DbModelService _dbmodelservice;
        public List<PanelModelMeta> PanelsModelControl = new List<PanelModelMeta>();

        public PageModels()
        {
            InitializeComponent();
        }




        //private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    string filterText = FilterTextBox.Text?.Trim().ToLowerInvariant();

        //    // Sécurité : si pas de panel chargé, on quitte
        //    if (PanelsModelControl == null || PanelsModelControl.Count == 0)
        //        return;

        //    // On va itérer sur chaque panel, en repérant son conteneur parent
        //    foreach (var panel in PanelsModelControl)
        //    {
        //        // Récupère le nom du modèle dans le PanelModelMeta
        //        string modelName = panel.Name_Model?.Text?.ToLowerInvariant();
        //        bool shouldShow = string.IsNullOrWhiteSpace(filterText) ||
        //                          (!string.IsNullOrEmpty(modelName) && modelName.Contains(filterText));

        //        // Masquer / afficher le panel lui-même
        //        panel.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;

        //        // 🔹 Retrouver le Grid parent direct du panel (celui créé dans GeneratePanelModelsAsync)
        //        if (panel.Parent is Grid rowGrid && rowGrid.Parent == GridModel)
        //        {
        //            // Affiche ou cache le conteneur entier
        //            rowGrid.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;

        //            // 🔹 Gestion de la barre de séparation (Border) juste après ce panel
        //            int rowIndex = Grid.GetRow(rowGrid);
        //            int separatorRowIndex = rowIndex + 1;

        //            // Recherche du Border séparateur à la ligne suivante
        //            var separator = GridModel.Children
        //                .OfType<Border>()
        //                .FirstOrDefault(b => Grid.GetRow(b) == separatorRowIndex);

        //            if (separator != null)
        //                separator.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        //        }
        //    }
        //}


    }
}

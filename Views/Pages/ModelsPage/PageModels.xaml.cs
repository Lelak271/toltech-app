using System.Windows.Controls;
using Toltech.App.FrontEnd.Controls;
using Toltech.App.Services;

namespace Toltech.App.Views
{

    // TODO : Créer une zone list de virtualizatioin UI pour fluidifier la vue des nombreux modele
    // TODO creer plusieurs biblio
    public partial class PageModels : UserControl
    {
        private MainWindow _myfirstwindow;
        private DatabaseService _databaseservice;
        private MetaModelDatabaseService _dbmodelservice;
        public List<PanelModelMeta> PanelsModelControl = new List<PanelModelMeta>();

        public PageModels()
        {
            InitializeComponent();
        }
    }
}

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Views.Controls.GrapheControl;
using TOLTECH_APPLICATION.Views.Controls.GrapheControl.GraphData;
using Westermo.GraphX.Common.Enums;
using Westermo.GraphX.Controls;
using Westermo.GraphX.Logic.Algorithms.EdgeRouting;
using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;

namespace TOLTECH_APPLICATION.FrontEnd.GraphePage.Controls
{
    public partial class MainGraphe : IDisposable
    {
        private DatabaseService _databaseService;
        private int _edgeDistance;
        private VertexControl _selectedVertex;
        public MainGraphe()
        {
            InitializeComponent();
            DataContext = this; // Tres important pour le BINDING
            var ggLogic = new GXLogicCoreExample();
            Area.LogicCore = ggLogic;

            ZoomControl.SetViewFinderVisibility(zoomctrl, Visibility.Visible);
            zoomctrl.ZoomToFill();

            //var defaultOptions = GraphLayoutOptions.Default;
            //GraphAreaExample_Setup(defaultOptions);
            gg_but_randomgraph.Click += gg_but_randomgraph_Click;
            gg_but_relayout.Click += gg_but_relayout_Click;

            _edgeDistance = 10;
            cbEnablePE.Checked += CbMathShapeOnChecked;
            cbEnablePE.Unchecked += CbMathShapeOnChecked;

            #region Type d'algoritme
            algo_choice.SelectionChanged += algo_choice_SelectionChanged;

            algo_choice.ItemsSource = Enum.GetValues<LayoutAlgorithmTypeEnum>().Cast<LayoutAlgorithmTypeEnum>();
            algo_choice.SelectedItem = LayoutAlgorithmTypeEnum.KK;
            #endregion

            #region Edge routing
            gg_eralgo.SelectionChanged += gg_eralgo_SelectionChanged;
            gg_eralgo.ItemsSource = Enum.GetValues<EdgeRoutingAlgorithmTypeEnum>()
                                     .Cast<EdgeRoutingAlgorithmTypeEnum>();
            gg_eralgo.SelectedItem = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            #endregion

            #region Overlap
            gg_oralgo.SelectionChanged += gg_oralgo_SelectionChanged;
            gg_oralgo.ItemsSource = Enum.GetValues<OverlapRemovalAlgorithmTypeEnum>()
               .Cast<OverlapRemovalAlgorithmTypeEnum>();
            gg_oralgo.SelectedIndex = 0;
            #endregion

            #region Label // aux edges
            alignEdgeLabels.Checked += alignEdgeLabels_Checked;
            alignEdgeLabels.Unchecked += alignEdgeLabels_Checked;
            #endregion

            Loaded += MainWindow_Loaded;
        }

        private double _globalImageSize = 40; // Valeur par défaut

        public double GlobalImageSize
        {
            get => _globalImageSize;
            set
            {
                if (Math.Abs(_globalImageSize - value) > 0.1)
                {
                    _globalImageSize = value;
                    OnPropertyChanged(nameof(GlobalImageSize));

                    // Mettre à jour tous les vertexs
                    UpdateAllVertexSizes(value);
                }
            }
        }

        private void UpdateAllVertexSizes(double newSize)
        {
            foreach (var vertexPair in Area.VertexList)
            {
                if (vertexPair.Value.Vertex is DataVertex dataVertex)
                {
                    dataVertex.ImageHeight = newSize;
                }
            }
        }


        private void InitializeDatabaseService()
        {
            _databaseService.Open(ModelManager.ModelActif);
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            var defaultOptions = GraphLayoutOptions.Default;
            await GraphAreaExample_Setup(defaultOptions);
            OnPropertyChanged("EdgeDistance");

            gg_but_randomgraph_Click(null, null);
        }

        private void gg_but_relayout_Click(object sender, RoutedEventArgs e)
        {
            Area.RelayoutGraph();
            zoomctrl.ZoomToFill();
        }

        private void gg_but_randomgraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Area.GenerateGraph(true);
            }
                        catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la génération du graphe : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Area.SetVerticesDrag(true);
            Area.SetEdgesDashStyle(EdgeDashStyle.Dot);
            Area.ShowAllEdgesArrows(false);
            Area.ShowAllEdgesLabels(false);
            Area.ShowAllVerticesLabels(true);

            //Area.Set = true;
            //Area.LogicCore.EdgeCurvingEnabled = true;

            // Ajout du routage des arêtes requirement **après** la génération du graphe
            foreach (var ec in Area.EdgesList.Values)
            {
                if (ec.Edge is DataEdge edge && edge.IsRequirement)
                {
                    ec.ShowArrows = true;
                    ec.Foreground = Brushes.Red;

                    // Récupération des positions des sommets source et cible (coordonnées graphiques)
                    var sourcePos = ec.Source.GetPosition();
                    var targetPos = ec.Target.GetPosition();
                }
            }

            foreach (var ec in Area.EdgesList.Values)
            {
                var label = FindChildOfType<TextBlock>(ec);
                if (label != null)
                {
                    label.FontSize = 3; // Ajustez selon vos besoins
                }
            }

            zoomctrl.ZoomToFill();
        }


        private Random _rand = new Random();

        // Déclaration du dictionnaire d'images
        private Dictionary<int, BitmapImage> _imageDictionary = new Dictionary<int, BitmapImage>()
                                        {
                                            { 0, new BitmapImage(new Uri("pack://application:,,,/Asset/add_model.png")) },
                                            { 1, new BitmapImage(new Uri("pack://application:,,,/Asset/add_model.png")) },
                                            { 2, new BitmapImage(new Uri("pack://application:,,,/Asset/add_model.png")) }
                                        };

        private async Task<GraphExample> GraphExample_Setup()
        {
            InitializeDatabaseService();

            // 1. Récupération des données
            var modelDataList = await _databaseService.GetAllModelDataAsync();
            var requirementsList = await _databaseService.GetAllRequirementsAsync();

            // 2. Création des noeuds (vertices)
            var vertices = new Dictionary<string, DataVertex>();
            var edgesToCreate = new List<(DataVertex Source, DataVertex Target, string Text, bool IsRequirement)>();

            var erreursLiaisons = new List<string>();

            // Traiter les ModelData
            foreach (var item in modelDataList)
            {
                if (string.IsNullOrEmpty(item.Origine) || string.IsNullOrEmpty(item.Extremite))
                {
                    erreursLiaisons.Add($"La liaison ID : {item.Id} est non valide.");
                    continue;
                }

                EnsureVertexExists(item.Origine, vertices);
                EnsureVertexExists(item.Extremite, vertices);

                edgesToCreate.Add((
                    vertices[item.Origine],
                    vertices[item.Extremite],
                    item.Model ?? $"{item.Origine} → {item.Extremite}",
                    false
                ));
            }

            // Traiter les Requirements
            foreach (var req in requirementsList)
            {
                if (string.IsNullOrEmpty(req.Part1) || string.IsNullOrEmpty(req.Part2) )
                {
                    erreursLiaisons.Add($"L'exigence ID : {req.Id_req} est non valide.");
                    continue;
                }

                EnsureVertexExists(req.Part1, vertices);
                EnsureVertexExists(req.Part2, vertices);

                edgesToCreate.Add((
                    vertices[req.Part1],
                    vertices[req.Part2],
                    req.NameReq ?? $"{req.Part1} → {req.Part2}",
                    true
                ));
            }

            // Affichage unique des erreurs
            if (erreursLiaisons.Count > 0)
            {
                var message = "Certaines liaisons sont nulles, vides ou référencent des pièces manquantes. Elles n’apparaîtront pas dans le graphe." + Environment.NewLine;

                foreach (var erreur in erreursLiaisons)
                {
                    message += "  - " + erreur + Environment.NewLine;
                }

                MessageBox.Show(message, "Erreur de liaison", MessageBoxButton.OK, MessageBoxImage.Warning);
            }




            // 3. Création du graphe avec les noeuds et arêtes
            var dataGraph = new GraphExample();

            // Ajouter tous les noeuds au graphe
            foreach (var vertex in vertices.Values)
            {
                dataGraph.AddVertex(vertex);
            }

            // Ajouter toutes les arêtes au graphe
            foreach (var edge in edgesToCreate)
            {
                dataGraph.AddEdge(new DataEdge(edge.Source, edge.Target)
                {
                    Text = edge.Text,
                    IsRequirement = edge.IsRequirement
                });
            }

            return dataGraph;
        }

        // Méthode helper pour créer un noeud s'il n'existe pas déjà
        private void EnsureVertexExists(string vertexId, Dictionary<string, DataVertex> vertices)
        {
            if (!vertices.ContainsKey(vertexId))
            {
                var imageIndex = _rand.Next(0, 3);
                vertices[vertexId] = new DataVertex(vertexId)
                {
                    ImageId = imageIndex,
                    ImageSource = _imageDictionary[imageIndex]
                };
            }
        }




        public class GraphLayoutOptions
        {
            public LayoutAlgorithmTypeEnum LayoutAlgorithm { get; set; } = LayoutAlgorithmTypeEnum.KK;
            public OverlapRemovalAlgorithmTypeEnum OverlapRemovalAlgorithm { get; set; } = OverlapRemovalAlgorithmTypeEnum.FSA;
            public EdgeRoutingAlgorithmTypeEnum EdgeRoutingAlgorithm { get; set; } = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            public int MaxIterations { get; set; } = 4000;
            public float HorizontalGap { get; set; } = 50;
            public float VerticalGap { get; set; } = 50;
            public bool UseAsyncCompute { get; set; } = false;

            // Options par défaut exposées en tant que propriété statique
            public static GraphLayoutOptions Default => new GraphLayoutOptions();

        }


        private async Task GraphAreaExample_Setup(GraphLayoutOptions options)
        {
            var graph = await GraphExample_Setup();

            var logicCore = new GXLogicCoreExample
            {
                Graph = graph,
                DefaultLayoutAlgorithm = options.LayoutAlgorithm,
                DefaultOverlapRemovalAlgorithm = options.OverlapRemovalAlgorithm,
                DefaultEdgeRoutingAlgorithm = options.EdgeRoutingAlgorithm,
                AsyncAlgorithmCompute = options.UseAsyncCompute
            };

            // Paramètres spécifiques au layout
            var layoutParams = logicCore.AlgorithmFactory.CreateLayoutParameters(options.LayoutAlgorithm);

            // Exemple : configuration spécifique à l’algorithme KK
            if (layoutParams is KKLayoutParameters kkParams)
            {
                kkParams.MaxIterations = options.MaxIterations;
            }

            logicCore.DefaultLayoutAlgorithmParams = layoutParams;

            // Paramètres d’overlap
            var overlapParams = logicCore.AlgorithmFactory.CreateOverlapRemovalParameters(options.OverlapRemovalAlgorithm);
            overlapParams.HorizontalGap = options.HorizontalGap;
            overlapParams.VerticalGap = options.VerticalGap;

            logicCore.DefaultOverlapRemovalAlgorithmParams = overlapParams;


            Area.LogicCore = logicCore;

            Area.LogicCore.EnableParallelEdges = true;
            Area.LogicCore.EdgeCurvingEnabled = true;

        }


        public void Dispose()
        {
            Area.Dispose();
        }

        public static T FindChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                if (child is T t)
                    return t;

                var result = FindChildOfType<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }



        #region Label

        private void EdgeLabelsVisibility_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && Area != null)
            {
                // Active/désactive tous les labels des arêtes selon l'état de la CheckBox
                Area.ShowAllEdgesLabels(checkBox.IsChecked ?? false);

                // Force le rafraîchissement de l'affichage
                Area.InvalidateVisual();
            }
        }

        #endregion

        private async void test_Click(object sender, RoutedEventArgs e)
        {

            var defaultOptions = GraphLayoutOptions.Default;
            await GraphAreaExample_Setup(defaultOptions);
            gg_but_randomgraph_Click(null, null);
        }


        #region Edge Distance

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int EdgeDistance
        {
            get => _edgeDistance;
            set
            {
                if (_edgeDistance != value)
                {
                    _edgeDistance = value;

                    if (Area?.LogicCore != null)
                    {
                        Area.LogicCore.ParallelEdgeDistance = value;
                        Area.UpdateAllEdges(true);
                    }

                    OnPropertyChanged("EdgeDistance");
                }
            }
        }

        
        // Empeche les valeurs non numérique 
        private void OnPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void CbMathShapeOnChecked(object sender, RoutedEventArgs routedEventArgs)
        {
            Area.LogicCore.EnableParallelEdges = (bool)cbEnablePE.IsChecked;
            Area.UpdateAllEdges(true);
        }

        #endregion


        #region Type d'algo
        private void algo_choice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var late = (LayoutAlgorithmTypeEnum)algo_choice.SelectedItem;
            Area.LogicCore.DefaultLayoutAlgorithm = late;
            if (late == LayoutAlgorithmTypeEnum.EfficientSugiyama)
            {
                var prms =
                    Area.LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.EfficientSugiyama)
                        as EfficientSugiyamaLayoutParameters;
                prms.EdgeRouting = SugiyamaEdgeRoutings.Orthogonal;
                prms.LayerDistance = prms.VertexDistance = 100;
                Area.LogicCore.EdgeCurvingEnabled = false;
                Area.LogicCore.DefaultLayoutAlgorithmParams = prms;
                gg_eralgo.SelectedItem = EdgeRoutingAlgorithmTypeEnum.None;
            }
            else
            {
                Area.LogicCore.EdgeCurvingEnabled = true;
            }

            if (late == LayoutAlgorithmTypeEnum.BoundedFR)
                Area.LogicCore.DefaultLayoutAlgorithmParams
                    = Area.LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.BoundedFR);
            if (late == LayoutAlgorithmTypeEnum.FR)
                Area.LogicCore.DefaultLayoutAlgorithmParams
                    = Area.LogicCore.AlgorithmFactory.CreateLayoutParameters(LayoutAlgorithmTypeEnum.FR);
        }

        #endregion

        #region Edge routing
        private void gg_eralgo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Area.LogicCore.DefaultEdgeRoutingAlgorithm = (EdgeRoutingAlgorithmTypeEnum)gg_eralgo.SelectedItem;
            if ((EdgeRoutingAlgorithmTypeEnum)gg_eralgo.SelectedItem == EdgeRoutingAlgorithmTypeEnum.Bundling)
            {
                var prm = new BundleEdgeRoutingParameters();
                Area.LogicCore.DefaultEdgeRoutingAlgorithmParams = prm;
                prm.Iterations = 200;
                prm.SpringConstant = 5;
                prm.Threshold = .1f;
                Area.LogicCore.EdgeCurvingEnabled = true;
            }
            else
                Area.LogicCore.EdgeCurvingEnabled = false;
        }
        #endregion

        #region OverLap
        private void gg_oralgo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var core = Area.LogicCore;
            core.DefaultOverlapRemovalAlgorithm = (OverlapRemovalAlgorithmTypeEnum)gg_oralgo.SelectedItem;
            if (core.DefaultOverlapRemovalAlgorithm == OverlapRemovalAlgorithmTypeEnum.FSA ||
                core.DefaultOverlapRemovalAlgorithm == OverlapRemovalAlgorithmTypeEnum.OneWayFSA)
            {
                core.DefaultOverlapRemovalAlgorithmParams.HorizontalGap = 30;
                core.DefaultOverlapRemovalAlgorithmParams.VerticalGap = 30;
            }
        }
        #endregion

        private void alignEdgeLabels_Checked(object sender, RoutedEventArgs e)
        {
            Area.AlignAllEdgesLabels(alignEdgeLabels.IsChecked != null && alignEdgeLabels.IsChecked.Value);
            Area.InvalidateVisual();
        }
    }
}

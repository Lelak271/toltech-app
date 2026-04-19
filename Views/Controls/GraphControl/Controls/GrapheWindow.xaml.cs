//using System;
//using System.Linq;
//using System.Windows;
//using Westermo.GraphX.Common.Enums;
//using Westermo.GraphX.Logic.Algorithms.LayoutAlgorithms;
//using Westermo.GraphX.Controls;
//using SimpleGraph.Models;


//namespace Toltech.App
//{
//    public partial class GrapheWindow : Window, IDisposable
//    {
//        public GrapheWindow()
//        {
//            InitializeComponent();
//            SetupGraphArea();
//            gg_but_randomgraph.Click += GenerateRandomGraph_Click;
//            gg_but_relayout.Click += RelayoutGraph_Click;
//            Loaded += GrapheWindow_Loaded;
//        }

//        private void GrapheWindow_Loaded(object sender, RoutedEventArgs e)
//        {
//            GenerateRandomGraph_Click(null, null);
//        }

//        private void RelayoutGraph_Click(object sender, RoutedEventArgs e)
//        {
//            Area.RelayoutGraph();
//            zoomctrl?.ZoomToFill();
//        }

//        private void GenerateRandomGraph_Click(object sender, RoutedEventArgs e)
//        {
//            Area.GenerateGraph();
//            Area.SetEdgesDashStyle(EdgeDashStyle.Dash);
//            Area.ShowAllEdgesArrows(false);
//            Area.ShowAllEdgesLabels();
//            zoomctrl?.ZoomToFill();
//        }

//        private void SetupGraphArea()
//        {
//            var logicCore = new GXLogicCoreExample
//            {
//                Graph = CreateExampleGraph(),
//                DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.KK,
//                DefaultLayoutAlgorithmParams = new KKLayoutParameters { MaxIterations = 100 },
//                DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA,
//                DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER,
//                AsyncAlgorithmCompute = false
//            };

//            var overlapParams = (OverlapRemovalParameters)logicCore.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
//            overlapParams.HorizontalGap = 50;
//            overlapParams.VerticalGap = 50;
//            logicCore.DefaultOverlapRemovalAlgorithmParams = overlapParams;

//            Area.LogicCore = logicCore;
//        }

//        private GraphExample CreateExampleGraph()
//        {
//            var dataGraph = new GraphExample();

//            for (int i = 1; i <= 9; i++)
//            {
//                dataGraph.AddVertex(new DataVertex($"MyVertex {i}"));
//            }

//            var v = dataGraph.Vertices.ToList();
//            dataGraph.AddEdge(new DataEdge(v[0], v[1]) { Text = $"{v[0]} -> {v[1]}" });
//            dataGraph.AddEdge(new DataEdge(v[2], v[3]) { Text = $"{v[2]} -> {v[3]}" });

//            return dataGraph;
//        }

//        public void Dispose()
//        {
//            Area.Dispose();
//        }
//    }

//    // =================== Modèles (inchangés ou légèrement ajustés) ===================

//    public class DataVertex : VertexBase
//    {
//        public string Text { get; set; }

//        public DataVertex(string text = "") => Text = text;

//        public override string ToString() => Text;
//    }

//    public class DataEdge : EdgeBase<DataVertex>
//    {
//        public string Text { get; set; }

//        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
//            : base(source, target, weight) { }

//        public DataEdge() : base(null, null) { }

//        public override string ToString() => Text;
//    }

//    public class GraphAreaExample : GraphArea<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>>
//    {
//        public GraphAreaExample()
//        {
//            EdgeLabelFactory = new DefaultEdgelabelFactory();
//            VertexLabelFactory = new DefaultVertexLabelFactory();
//            SetVerticesDrag(true, true);
//        }
//    }

//    public class GraphExample : BidirectionalGraph<DataVertex, DataEdge> { }

//    public class GXLogicCoreExample : GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }
//}

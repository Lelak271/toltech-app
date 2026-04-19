using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Toltech.App.Services;
using Toltech.App.ToltechCalculation.Resux;

namespace Toltech.App.FrontEnd.Controls.Dashboard
{
    public partial class PieChartControl : UserControl
    {
        // ── Palette de couleurs pour les tranches ──────────────────────────────
        private static readonly SKColor[] SlicePalette =
        {
            new SKColor(76,  175,  80),   // vert
            new SKColor(33,  150, 243),   // bleu
            new SKColor(255, 152,   0),   // orange
            new SKColor(233,  30,  99),   // rose
            new SKColor(156,  39, 176),   // violet
            new SKColor(0,   188, 212),   // cyan
            new SKColor(255, 235,  59),   // jaune
            new SKColor(121,  85,  72),   // brun
            new SKColor(96,  125, 139),   // gris-bleu
            new SKColor(255,  87,  34),   // rouge-orangé
        };

        private readonly ResuxSerializer _resuxSerializer;
        private bool _isLoadingCombo;

        // ── Modes d'affichage ─────────────────────────────────────────────────
        private enum PieMode { ByReq, AllReqs }
        private PieMode _pieMode = PieMode.ByReq;

        public PieChartControl()
        {
            InitializeComponent();
            _resuxSerializer = new ResuxSerializer();

            UpdateChartVisibility();

            // Écoute le changement de fichier résultats (même pattern que BarChartControl)
            ModelManager.FilePathResxChanged += _ =>
                Dispatcher.InvokeAsync(UpdateChartVisibility);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Chargement initial / visibilité
        // ════════════════════════════════════════════════════════════════════════

        private async Task UpdateChartVisibility()
        {
            bool hasData = ModelManager.FilePathResx != null && ModelManager.FilePathResx.Any();

            NoDataImage.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
            ChartArea.Visibility   = hasData ? Visibility.Visible   : Visibility.Collapsed;

            if (hasData)
                await LoadRequirementsToComboBox(ModelManager.FilePathResx);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Chargement de la ComboBox exigences
        // ════════════════════════════════════════════════════════════════════════

        public async Task LoadRequirementsToComboBox(string filePath)
        {
            _isLoadingCombo = true;

            var headers = _resuxSerializer.ExtractReqHeaders(filePath);
            var items   = headers
                .Select(h => new { Display = $"[{h.IdReq}] {h.Name}", Value = h.IdReq })
                .ToList();

            if (items == null || items.Count == 0)
            {
                ReqCombo.ItemsSource = null;
                _isLoadingCombo = false;
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ReqCombo.ItemsSource       = items;
                ReqCombo.DisplayMemberPath = "Display";
                ReqCombo.SelectedValuePath = "Value";

                if (items.Count > 0)
                {
                    ReqCombo.SelectedIndex = 0;
                    if (ReqCombo.SelectedValue is int idReq)
                        _ = BuildPieChartAsync(idReq, filePath);
                }
            });

            _isLoadingCombo = false;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Construction du graphe camembert
        // ════════════════════════════════════════════════════════════════════════

        private async Task BuildPieChartAsync(int idReq, string filePath)
        {
            var mode = _pieMode;

            var slices = await Task.Run(() =>
            {
                var results = _resuxSerializer.LoadInfluencedWCFromFile(idReq, filePath);
                IEnumerable<ResuxSerializer.ResultEachData> data = results.Data;

                var list = data.ToList();

                // ── Mode PAR PIÈCE ────────────────────────────────────────────────────
                if (mode == PieMode.ByReq)
                {
                    // ContribWC est déjà précalculé dans ResultEachData
                    // TolOri  → NameOri  | TolExtr → NameExtre | TolInt → 50/50 entre les deux
                    var byPart = new Dictionary<string, double>();

                    foreach (var d in list)
                    {
                        double ori = Math.Abs(d.ContribWCOri) ;
                        double intr =  Math.Abs(d.ContribWCInt);
                        double extr = Math.Abs(d.ContribWCExtr);

                        // TolOri → pièce NameOri
                        if (!string.IsNullOrEmpty(d.NameOri) && Math.Abs(ori) > 1e-10)
                        {
                            byPart.TryAdd(d.NameOri, 0);
                            byPart[d.NameOri] += ori;
                        }

                        // TolExtr → pièce NameExtre
                        if (!string.IsNullOrEmpty(d.NameExtre) && Math.Abs(extr) > 1e-10)
                        {
                            byPart.TryAdd(d.NameExtre, 0);
                            byPart[d.NameExtre] += extr;
                        }

                        // TolInt → 50/50 entre NameOri et NameExtre
                        if (Math.Abs(intr) > 1e-10)
                        {
                            double half = intr / 2.0;

                            if (!string.IsNullOrEmpty(d.NameOri))
                            {
                                byPart.TryAdd(d.NameOri, 0);
                                byPart[d.NameOri] += half;
                            }
                            if (!string.IsNullOrEmpty(d.NameExtre))
                            {
                                byPart.TryAdd(d.NameExtre, 0);
                                byPart[d.NameExtre] += half;
                            }
                        }
                    }

                    return byPart
                        .Where(kv => Math.Abs(kv.Value) > 1e-10)
                        .OrderByDescending(kv => Math.Abs(kv.Value))
                        .Select(kv => new SliceData { Label = kv.Key, Value = kv.Value })
                        .ToList();
                }

                // ── Mode ALL REQS : contribution par pièce sur toutes les exigences ──
                var allData = _resuxSerializer
                    .ExtractReqHeaders(filePath)
                    .SelectMany(h => _resuxSerializer.LoadInfluencedWCFromFile(h.IdReq, filePath).Data)
                    .ToList();

                var byPart1 = new Dictionary<string, double>();

                foreach (var d in allData)
                {
                    double ori = Math.Abs(d.ContribWCOri);
                    double intr = Math.Abs(d.ContribWCInt);
                    double extr = Math.Abs(d.ContribWCExtr);

                    if (!string.IsNullOrEmpty(d.NameOri) && ori > 1e-10)
                    {
                        byPart1.TryAdd(d.NameOri, 0);
                        byPart1[d.NameOri] += ori;
                    }

                    if (!string.IsNullOrEmpty(d.NameExtre) && extr > 1e-10)
                    {
                        byPart1.TryAdd(d.NameExtre, 0);
                        byPart1[d.NameExtre] += extr;
                    }

                    if (intr > 1e-10)
                    {
                        double half = intr / 2.0;
                        if (!string.IsNullOrEmpty(d.NameOri)) { byPart1.TryAdd(d.NameOri, 0); byPart1[d.NameOri] += half; }
                        if (!string.IsNullOrEmpty(d.NameExtre)) { byPart1.TryAdd(d.NameExtre, 0); byPart1[d.NameExtre] += half; }
                    }
                }

                return byPart1
                    .Where(kv => kv.Value > 1e-10)
                    .OrderByDescending(kv => kv.Value)
                    .Select(kv => new SliceData { Label = kv.Key, Value = kv.Value })
                    .ToList();

            });
            await Dispatcher.InvokeAsync(() => ApplySlicesToChart(slices)); 

        }

        // ════════════════════════════════════════════════════════════════════════
        // Application des tranches au PieChart + légende
        // ════════════════════════════════════════════════════════════════════════

        private void ApplySlicesToChart(List<SliceData> slices)
        {
            if (slices == null || slices.Count == 0)
            {
                PieChart.Series = Array.Empty<ISeries>();
                LegendPanel.Children.Clear();
                return;
            }

            double total = slices.Sum(s => Math.Abs(s.Value));

            var series = slices
                .Select((s, i) =>
                {
                    var color = SlicePalette[i % SlicePalette.Length];
                    return new PieSeries<double>
                    {
                        Name         = s.Label,
                        Values = new[] { Math.Round(Math.Abs(s.Value), 3) },
                        Fill         = new SolidColorPaint(color),
                        Stroke       = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsFormatter = point =>
                            total > 1e-10
                                ? $"{point.Coordinate.PrimaryValue / total * 100:F1} %"
                                : string.Empty,
                        DataLabelsSize     = 13,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Outer,
                        IsHoverable        = true,
                        InnerRadius        = 80       // camembert plein ; mettre ex. 80 pour donut
                    };
                })
                .ToArray<ISeries>();

            PieChart.Series = series;

            // ── Légende personnalisée ──────────────────────────────────────────
            LegendPanel.Children.Clear();

            foreach (var (s, i) in slices.Select((s, i) => (s, i)))
            {
                var skColor = SlicePalette[i % SlicePalette.Length];
                var wpfColor = Color.FromArgb(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);

                double pct = total > 1e-10 ? Math.Abs(s.Value) / total * 100 : 0;

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };

                var square = new Rectangle
                {
                    Width  = 14,
                    Height = 14,
                    Fill   = new SolidColorBrush(wpfColor),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var label = new TextBlock
                {
                    Text       = $"{s.Label}",
                    FontSize   = 11,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth   = 130,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var value = new TextBlock
                {
                    Text       = $"  {s.Value:F3}  ({pct:F1} %)",
                    FontSize   = 11,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                row.Children.Add(square);
                row.Children.Add(label);
                row.Children.Add(value);
                LegendPanel.Children.Add(row);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Handlers UI
        // ════════════════════════════════════════════════════════════════════════

        private void ReqCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingCombo) return;
            if (ReqCombo.SelectedValue is int idReq)
                _ = BuildPieChartAsync(idReq, ModelManager.FilePathResx);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReqCombo.SelectedValue is int idReq)
                await BuildPieChartAsync(idReq, ModelManager.FilePathResx);
        }

        private void RadioMode_Checked(object sender, RoutedEventArgs e)
        {
            _pieMode = RadioByType?.IsChecked == true ? PieMode.AllReqs : PieMode.ByReq;

            if (ReqCombo?.SelectedValue is int idReq)
                _ = BuildPieChartAsync(idReq, ModelManager.FilePathResx);
        }


        // ════════════════════════════════════════════════════════════════════════
        // DTO interne
        // ════════════════════════════════════════════════════════════════════════

        private class SliceData
        {
            public string Label { get; set; } = string.Empty;
            public double Value { get; set; }
        }
    }
}

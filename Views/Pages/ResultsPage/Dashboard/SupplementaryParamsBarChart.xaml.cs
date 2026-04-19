using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using static Toltech.App.FrontEnd.Controls.Dashboard.BarChartControl;

namespace Toltech.App.FrontEnd.Controls.Dashboard
{
    public partial class SupplementaryParamsBarChart : Window
    {
        public ChartStyleSettings Settings { get; private set; }

        public SupplementaryParamsBarChart(ChartStyleSettings settings)
        {
            InitializeComponent();

            Settings = settings ?? new ChartStyleSettings(); // Charger settings existants ou utiliser fallback

            LoadSettingsToControls();
            AttachLivePreviewHandlers();   //  permet le rendu live

            // Initialiser le chart au moment du Loaded (assure que PreviewChart est construit)
            this.Loaded += (s, e) =>
            {
                InitializeChart();
                UpdateChartFromUI(); // forcé une fois après que tout soit prêt
            };
        }

        // Charge les paramètres dans les contrôles UI
        private void LoadSettingsToControls()
        {
            var s = Settings;

            ColumnFill1Picker.SelectedColor = SKToMediaColor(s.ColumnFill1);
            ColumnFill2Picker.SelectedColor = SKToMediaColor(s.ColumnFill2);
            ColumnFill3Picker.SelectedColor = SKToMediaColor(s.ColumnFill3);
            ColumnOutlinePicker.SelectedColor = SKToMediaColor(s.ColumnOutline);
            ColumnStrokeThicknessSlider.Value = s.ColumnStrokeThickness;
            BarWidthThicknessSlider.Value = s.BarWidthThickness;

            LineStrokePicker.SelectedColor = SKToMediaColor(s.LineStroke);
            LineStrokeThicknessSlider.Value = s.LineStrokeThickness;
            LineGeometrySizeSlider.Value = s.LineGeometrySize;

            LineGeometryFillPicker.SelectedColor = SKToMediaColor(s.LineGeometryFill);
            LineGeometryStrokePicker.SelectedColor = SKToMediaColor(s.LineGeometryStroke);

            CheckHideLine.IsChecked = s.HideLineSerie;
        }


        private void AttachSliderHandler(RangeBase slider, Action<double> onChange)
        {
            if (slider != null && onChange != null)
            {
                slider.ValueChanged += (_, e) =>
                {
                    onChange(e.NewValue); // Met à jour uniquement le paramètre lié
                };
            }
        }


        // Attache les handlers pour mise à jour en temps réel
        private void AttachLivePreviewHandlers()
        {

            #region Gestion Slider

            // Slider : contour individuel
            AttachSliderHandler(ColumnStrokeThicknessSlider, val =>
            {
                if (_previewSeries != null)
                {
                    foreach (var series in _previewSeries)
                    {
                        switch (series)
                        {
                            case ColumnSeries<double> col:
                                col.Stroke = new SolidColorPaint(MediaToSKColor(ColumnOutlinePicker.SelectedColor ?? Colors.Black))
                                { StrokeThickness = (float)val };
                                break;
                        }
                    }

                    PreviewChart.UpdateLayout();
                }
            });


            // Slider : largeur des barres (toutes les séries)
            AttachSliderHandler(BarWidthThicknessSlider, val =>
            {
                if (_previewSeries != null)
                {
                    foreach (var serie in _previewSeries)
                    {
                        switch (serie)
                        {
                            case ColumnSeries<double> cs:
                                cs.MaxBarWidth = (float)val;
                                break;
                            case StackedColumnSeries<double> scs:
                                scs.MaxBarWidth = (float)val;
                                break;
                        }
                    }
                    PreviewChart.UpdateLayout();
                }
            });

            // Slider : épaisseur de la ligne
            AttachSliderHandler(LineStrokeThicknessSlider, val =>
            {
                if (_previewSeries != null)
                {
                    foreach (var series in _previewSeries.OfType<LineSeries<double>>())
                    {
                        series.Stroke = new SolidColorPaint(MediaToSKColor(LineStrokePicker.SelectedColor ?? Colors.Red))
                        { StrokeThickness = (float)val };
                    }
                    PreviewChart.UpdateLayout();
                }
            });

            // Slider : taille des marqueurs
            AttachSliderHandler(LineGeometrySizeSlider, val =>
            {
                if (_previewSeries != null)
                {
                    foreach (var series in _previewSeries.OfType<LineSeries<double>>())
                    {
                        series.GeometrySize = (float)val;
                        if (series.GeometryStroke != null)
                            series.GeometryStroke.StrokeThickness = Math.Max(0.5f, (float)val * 0.12f);
                    }
                    PreviewChart.UpdateLayout();
                }
            });


            #endregion

            #region Gestion des couleurs

            void AttachColorChanged(Xceed.Wpf.Toolkit.ColorPicker cp)
            {
                if (cp != null)
                    cp.SelectedColorChanged += (_, __) => UpdateChartFromUI();
            }

            // Attacher explicitement à chaque contrôle present dans le XAML
            AttachColorChanged(ColumnFill1Picker);
            AttachColorChanged(ColumnFill2Picker);
            AttachColorChanged(ColumnFill3Picker);
            AttachColorChanged(ColumnOutlinePicker);

            AttachColorChanged(LineStrokePicker);

            AttachColorChanged(LineGeometryFillPicker);
            AttachColorChanged(LineGeometryStrokePicker);

            #endregion

            // Checkbox
            if (CheckHideLine != null)
            {
                CheckHideLine.Checked += (_, __) => UpdateChartFromUI();
                CheckHideLine.Unchecked += (_, __) => UpdateChartFromUI();
            }
        }


        // OK : lecture UI -> sauvegarde dans Settings
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.ColumnFill1 = MediaToSKColor(ColumnFill1Picker.SelectedColor ?? Colors.LawnGreen);
            Settings.ColumnFill2 = MediaToSKColor(ColumnFill2Picker.SelectedColor ?? Colors.Beige);
            Settings.ColumnFill3 = MediaToSKColor(ColumnFill3Picker.SelectedColor ?? Colors.Tan);
            Settings.ColumnOutline = MediaToSKColor(ColumnOutlinePicker.SelectedColor ?? Colors.Black);
            Settings.ColumnStrokeThickness = (float)ColumnStrokeThicknessSlider.Value;
            Settings.BarWidthThickness = (float)BarWidthThicknessSlider.Value;

            Settings.LineStroke = MediaToSKColor(LineStrokePicker.SelectedColor ?? Colors.Red);
            Settings.LineStrokeThickness = (float)LineStrokeThicknessSlider.Value;
            Settings.LineGeometrySize = (float)LineGeometrySizeSlider.Value;
            Settings.LineGeometryFill = MediaToSKColor(LineGeometryFillPicker.SelectedColor ?? Colors.Purple);
            Settings.LineGeometryStroke = MediaToSKColor(LineGeometryStrokePicker.SelectedColor ?? Colors.Red);

            Settings.HideLineSerie = CheckHideLine.IsChecked ?? false;

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ---------- Helpers conversion ----------
        private static SKColor MediaToSKColor(System.Windows.Media.Color c)
            => new SKColor(c.R, c.G, c.B, c.A);

        private static System.Windows.Media.Color SKToMediaColor(SKColor sk)
            => System.Windows.Media.Color.FromArgb(sk.Alpha, sk.Red, sk.Green, sk.Blue);


        #region For render 
        // Champs privés pour les séries / axes
        private ISeries[] _previewSeries = null;
        private Axis[] _xAxes = null;
        private Axis[] _yAxes = null;

        private void InitializeChart()
        {
            // Définitions d'axes (3 catégories d'exemple)
            _xAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new[] { "Catégorie A", "Catégorie B", "Catégorie C" },
                    LabelsRotation = -45,
                    Name = "Categories"
                }
            };

            _yAxes = new Axis[]
            {
                new Axis { Name = "Valeur", MinLimit = 0 }
            };

            // Séries empilées initiales (valeurs d'exemple)
            var col1 = new StackedColumnSeries<double> { Values = new double[] { 30, 50, 40 } };
            var col2 = new StackedColumnSeries<double> { Values = new double[] { 40, 35, 25 } };
            var col3 = new StackedColumnSeries<double> { Values = new double[] { 20, 40, 35 } };

            var line = new LineSeries<double>
            {
                Values = new double[] { 30 + 50 + 40, 40 + 35 + 25, 20 + 40 + 35 },
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 1.5f },
                GeometryFill = new SolidColorPaint(SKColors.Purple),
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2f }
            };

            _previewSeries = new ISeries[] { col1, col2, col3, line };

            // Vérifier que le contrôle existe et lui assigner les séries/axes
            if (PreviewChart != null)
            {
                PreviewChart.Series = _previewSeries;
                PreviewChart.XAxes = _xAxes;
                PreviewChart.YAxes = _yAxes;
            }
        }

        private void UpdateChartFromUI()
        {
            if (PreviewChart == null) return;

            // Vérifier quel mode est actif
            bool isContribMode = RadioContrib.IsChecked ?? true;

            if (isContribMode)
                UpdateChartContrib();
            else
                UpdateChartInfluence();
        }

        // ================= Mode Contrib (Stacked Columns + Contour + Ligne) =================
        private void UpdateChartContrib()
        {
            if (PreviewChart == null) return;

            // Couleurs et paramètres UI
            var fill1 = ColumnFill1Picker.SelectedColor ?? Colors.LawnGreen;
            var fill2 = ColumnFill2Picker.SelectedColor ?? Colors.Beige;
            var fill3 = ColumnFill3Picker.SelectedColor ?? Colors.Tan;
            var outline = ColumnOutlinePicker.SelectedColor ?? Colors.Black;
            float outlineThickness = Math.Max(0.8f, (float)ColumnStrokeThicknessSlider.Value);
            float BarWidthThickness = Math.Max(0.8f, (float)BarWidthThicknessSlider.Value);

            var lineClr = LineStrokePicker.SelectedColor ?? Colors.Red;
            float lineThickness = Math.Max(0.8f, (float)LineStrokeThicknessSlider.Value);

            var markerFill = LineGeometryFillPicker.SelectedColor ?? Colors.Purple;
            var markerStroke = LineGeometryStrokePicker.SelectedColor ?? Colors.Red;
            float geoSize = Math.Max(4f, (float)LineGeometrySizeSlider.Value);

            // Données exemple stacked
            var s1 = new double[] { 30, 50, 40 };
            var s2 = new double[] { 40, 35, 25 };
            var s3 = new double[] { 20, 40, 35 };

            var sTotal = s1.Zip(s2, (a, b) => a + b)
                           .Zip(s3, (ab, c) => ab + c)
                           .ToArray();

            var lineValues = sTotal;

            SKColor MediaToSk(System.Windows.Media.Color c) => new SKColor(c.R, c.G, c.B, c.A);
            SolidColorPaint PaintFromMedia(System.Windows.Media.Color c, float thickness = 0)
                => new SolidColorPaint(MediaToSk(c)) { StrokeThickness = thickness };

            // Séries stacked + contour + ligne
            _previewSeries = new ISeries[]
            {
        new StackedColumnSeries<double> { Values = s1, Fill = PaintFromMedia(fill1), MaxBarWidth = BarWidthThickness },
        new StackedColumnSeries<double> { Values = s2, Fill = PaintFromMedia(fill2), MaxBarWidth = BarWidthThickness },
        new StackedColumnSeries<double> { Values = s3, Fill = PaintFromMedia(fill3), MaxBarWidth = BarWidthThickness },
        new ColumnSeries<double>
        {
            Values = sTotal,
            Fill = null,
            Stroke = new SolidColorPaint(MediaToSk(outline)) { StrokeThickness = outlineThickness },
            MaxBarWidth = BarWidthThickness
        },
        new LineSeries<double>
        {
            Values = lineValues,
            Fill = null,
            Stroke = new SolidColorPaint(MediaToSk(lineClr)) { StrokeThickness = lineThickness },
            GeometrySize = geoSize,
            GeometryFill = PaintFromMedia(markerFill),
            GeometryStroke = new SolidColorPaint(MediaToSk(markerStroke)) { StrokeThickness = Math.Max(0.5f, geoSize * 0.12f) },
            IsVisible = !(CheckHideLine.IsChecked ?? false)
        }
            };

            PreviewChart.Series = _previewSeries;
            PreviewChart.UpdateLayout();
        }

        // ================= Mode Influence (Colonnes simples) =================
        private void UpdateChartInfluence()
        {
            if (PreviewChart == null) return;

            var fill = ColumnFill1Picker.SelectedColor ?? Colors.LawnGreen;
            var outline = ColumnOutlinePicker.SelectedColor ?? Colors.Black;
            float outlineThickness = Math.Max(0.8f, (float)ColumnStrokeThicknessSlider.Value);
            float BarWidthThickness = Math.Max(0.8f, (float)BarWidthThicknessSlider.Value);

            var lineClr = LineStrokePicker.SelectedColor ?? Colors.Red;
            float lineThickness = Math.Max(0.8f, (float)LineStrokeThicknessSlider.Value);

            var markerFill = LineGeometryFillPicker.SelectedColor ?? Colors.Purple;
            var markerStroke = LineGeometryStrokePicker.SelectedColor ?? Colors.Red;
            float geoSize = Math.Max(4f, (float)LineGeometrySizeSlider.Value);

            // Exemple de valeurs simples
            var values = new double[] { 1.3, 2, 0.9 };

            SKColor MediaToSk(System.Windows.Media.Color c) => new SKColor(c.R, c.G, c.B, c.A);
            SolidColorPaint PaintFromMedia(System.Windows.Media.Color c, float thickness = 0)
                => new SolidColorPaint(MediaToSk(c)) { StrokeThickness = thickness };

            // Construire les séries
            _previewSeries = new ISeries[]
            {
        new ColumnSeries<double>
        {
            Values = values,
            Fill = PaintFromMedia(fill),
            Stroke = new SolidColorPaint(MediaToSk(outline)) { StrokeThickness = outlineThickness },
            MaxBarWidth = BarWidthThickness
        },
        new LineSeries<double>
        {
            Values = values, // La ligne suit la colonne
            Fill = null,
            Stroke = new SolidColorPaint(MediaToSk(lineClr)) { StrokeThickness = lineThickness },
            GeometrySize = geoSize,
            GeometryFill = PaintFromMedia(markerFill),
            GeometryStroke = new SolidColorPaint(MediaToSk(markerStroke))
            {
                StrokeThickness = Math.Max(0.5f, geoSize * 0.12f)
            },
            IsVisible = !(CheckHideLine.IsChecked ?? false)
        }
            };

            // Forcer le redraw
            PreviewChart.Series = _previewSeries;
            PreviewChart.UpdateLayout();
        }


        #endregion

        private void RadioContrib_Checked(object sender, RoutedEventArgs e)
        {
            UpdateChartMode(ChartMode.Contrib);
        }

        private void RadioInfluence_Checked(object sender, RoutedEventArgs e)
        {
            UpdateChartMode(ChartMode.Influence);
        }

        private enum ChartMode { Contrib, Influence }

        private void UpdateChartMode(ChartMode mode)
        {
                UpdateChartFromUI();
        }

        #region Theme Chart

        private void ThemeButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == ThemeDefaultButton)
            {
                // Appliquer les paramètres du thème par défaut
                ApplyTheme(defaultFill: Colors.LawnGreen, defaultOutline: Colors.Black, defaultLine: Colors.Red);
            }
            else if (sender == Theme1Button)
            {
                // Paramètres pour Thème 1
                ApplyTheme(defaultFill: Colors.LightBlue, defaultOutline: Colors.DarkBlue, defaultLine: Colors.Orange);
            }
            else if (sender == Theme2Button)
            {
                // Paramètres pour Thème 2
                ApplyTheme(defaultFill: Colors.Beige, defaultOutline: Colors.Brown, defaultLine: Colors.Purple);
            }
        }

        // Méthode pour appliquer un thème aux contrôles et mettre à jour l'aperçu
        private void ApplyTheme(Color defaultFill, Color defaultOutline, Color defaultLine)
        {
            ColumnFill1Picker.SelectedColor = defaultFill;
            ColumnOutlinePicker.SelectedColor = defaultOutline;
            LineStrokePicker.SelectedColor = defaultLine;

            // Recalculer le chart avec ces paramètres
            UpdateChartFromUI();
        }


        #endregion

    }
}

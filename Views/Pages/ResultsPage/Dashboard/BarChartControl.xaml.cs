using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Toltech.App.Properties;
using Toltech.App.Services;
using Toltech.App.ToltechCalculation.Resux;
using Toltech.App.Utilities;
using Toltech.App.Views;

namespace Toltech.App.FrontEnd.Controls.Dashboard
{
    // Logique d'interaction pour BarChartControl.xaml
    public partial class BarChartControl : UserControl
    {
        const double EPSILON = 1e-10; // Tolérance adaptée aux calculs

        private BarChartSortOption _barChartSortOption = BarChartSortOption.None;
        private ResuxSerializer _resuxSerializer;
        public BarChartControl()
        {
            InitializeComponent();
            UpdateComboBoxItems();

            _resuxSerializer = new ResuxSerializer();

            UpdateChartVisibility();

            ModelManager.FilePathResxChanged += filresx =>
            {
                Dispatcher.InvokeAsync(UpdateChartVisibility);
            };


        }
        public PageResultats ParentPage { get; set; }

        #region Generation Chart
        // Enum interne
        public enum BarChartSortOption
        {
            None,
            ByMaxInfl,
            ByMinInfl,

            ByContribOri,
            ByContribInt,
            ByContribExtr,
            ByContrib
        }

        private async Task GlobalDisplayBarChart(ResuxSerializer.ResultsForReq projections, ViewMode viewMode, CalculationMode calcMode)
        {
            if (viewMode == ViewMode.Influence && calcMode == CalculationMode.WorstCase)
            {
                await DisplayBarChartAsync(projections, false);
            }
            else if (viewMode == ViewMode.Contribution && calcMode == CalculationMode.WorstCase)
            {
                await DisplayBarChartAsync(projections, true);
            }
            else
            {
                MessageBox.Show("Fonction pas encore intégrée");
            }
        }


        public async Task DisplayBarChartAsync(ResuxSerializer.ResultsForReq resultsForReq, bool showContributions)
        {
            bool excludeNullZero = CheckExcludeNullZero.IsChecked == true;
            bool useAbsoluteValues = CheckAbsoluteValues.IsChecked == true;

            // 1️⃣ Calculs hors thread UI
            var data = await Task.Run(() =>
            {
                // Préparer les données
                IEnumerable<ResuxSerializer.ResultEachData> d = resultsForReq.Linkages;

                // Appliquer filtre Null/0
                if (excludeNullZero)
                {
                    double minValue = 0.01;

                    d = showContributions
                                   ? d.Where(x =>
                                       Math.Abs(x.GlobalContribWCOri) >= minValue ||
                                       Math.Abs(x.GlobalContribWCInt) >= minValue ||
                                       Math.Abs(x.GlobalContribWCExtr) >= minValue)
                                   : d.Where(x =>
                                       Math.Abs(x.InfluencWCN) >= minValue ||
                                       Math.Abs(x.InfluencWCT1) >= minValue ||
                                       Math.Abs(x.InfluencWCT2) >= minValue ||
                                       Math.Abs(x.RotationInfluencWCN) >= minValue ||
                                       Math.Abs(x.RotationInfluencWCRT1) >= minValue ||
                                       Math.Abs(x.RotationInfluencWCRT2) >= minValue);
                }

                // Appliquer valeur absolue si nécessaire
                if (useAbsoluteValues)
                {
                    d = d.Select(x => new ResuxSerializer.ResultEachData
                    {
                        NameData = x.NameData,
                        NameExtre = x.NameExtre,
                        NameOri = x.NameOri,
                        IdData = x.IdData,
                        InfluencWCN = Math.Abs(x.InfluencWCN),
                        InfluencWCT1 = Math.Abs(x.InfluencWCT1),
                        InfluencWCT2 = Math.Abs(x.InfluencWCT2),
                        RotationInfluencWCN = Math.Abs(x.RotationInfluencWCN) / 1000,
                        RotationInfluencWCRT1 = Math.Abs(x.RotationInfluencWCRT1) / 1000,
                        RotationInfluencWCRT2 = Math.Abs(x.RotationInfluencWCRT2) / 1000,
                        GlobalContribWCOri = Math.Abs(x.GlobalContribWCOri),
                        GlobalContribWCInt = Math.Abs(x.GlobalContribWCInt),
                        GlobalContribWCExtr = Math.Abs(x.GlobalContribWCExtr)
                    });
                    // GlobalRotationInfluencWC /mRad =>  permet d'afficher ensuite le mm/mRad
                }

                // Limite à 150 par valeur absolue la plus élevée
                const int MAX_POINTS = 100;
                if (showContributions)
                {
                    d = d
                        .OrderByDescending(x => Math.Max(
                            Math.Max(Math.Abs(x.GlobalContribWCOri), Math.Abs(x.GlobalContribWCInt)),
                            Math.Abs(x.GlobalContribWCExtr)))
                        .Take(MAX_POINTS)
                        .ToList();
                }
                else
                {
                    d = d
                        .OrderByDescending(x => new[]
                        {
                    Math.Abs(x.InfluencWCN),
                    Math.Abs(x.InfluencWCT1),
                    Math.Abs(x.InfluencWCT2),
                    Math.Abs(x.RotationInfluencWCN),
                    Math.Abs(x.RotationInfluencWCRT1),
                    Math.Abs(x.RotationInfluencWCRT2)
                        }.Max())
                        .Take(MAX_POINTS)
                        .ToList();
                }

                // Trier selon l'option de tri
                d = showContributions switch
                {
                    true => _barChartSortOption switch
                    {
                        BarChartSortOption.ByContribOri => d.OrderByDescending(x => x.GlobalContribWCOri),
                        BarChartSortOption.ByContribInt => d.OrderByDescending(x => x.GlobalContribWCInt),
                        BarChartSortOption.ByContribExtr => d.OrderByDescending(x => x.GlobalContribWCExtr),
                        BarChartSortOption.ByContrib => d.OrderByDescending(x => x.GlobalContribWCOri + x.GlobalContribWCInt + x.GlobalContribWCExtr),
                        _ => d
                    },
                    false => _barChartSortOption switch
                    {
                        BarChartSortOption.ByMaxInfl => d.OrderByDescending(x => x.InfluencWCN),
                        BarChartSortOption.ByMinInfl => d.OrderBy(x => x.InfluencWCN),
                        _ => d
                    }
                };

                return d.ToList();
            });

            // 2️⃣ Mise à jour UI via le thread principal
            await Dispatcher.InvokeAsync(() =>
            {
                BuildContributionChart(data, showContributions, resultsForReq.IdReq);
            });
        }

        private void BuildContributionChart(List<ResuxSerializer.ResultEachData> data, bool showContributions, int idReq)
        {
            var labels = new List<string>();

            // Fallbacks si les settings sont absents ou invalides
            var style = LoadChartStyleSettings();

            if (showContributions)
            {
                // Remplir les listes de contributions pour chaque catégorie
                var contribOri = new List<double>();
                var contribInt = new List<double>();
                var contribExtr = new List<double>();
                var nameori = new List<string>();

                foreach (var item in data)
                {
                    contribOri.Add(item.GlobalContribWCOri);
                    contribInt.Add(item.GlobalContribWCInt);
                    contribExtr.Add(item.GlobalContribWCExtr);
                    labels.Add(item.NameData);
                }

                // Créer les séries empilées pour chaque type de contribution
                var seriesOri = new StackedColumnSeries<double>
                {
                    Name = "Origine",
                    Values = contribOri,
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(style.ColumnFill1),
                };
                var seriesInt = new StackedColumnSeries<double>
                {
                    Name = "Interface",
                    Values = contribInt,
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(style.ColumnFill2)
                };
                var seriesExtr = new StackedColumnSeries<double>
                {
                    Name = "Pièce Extrémité",
                    Values = contribExtr,
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(style.ColumnFill3)
                };
                // série "cadre" = somme des 3
                var sTotal = contribOri
                                      .Zip(contribInt, (a, b) => a + b)
                                      .Zip(contribExtr, (ab, c) => ab + c)
                                      .ToArray();

                var outlineSeries = new ColumnSeries<double>
                {
                    Values = sTotal,
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = null, // transparent
                    Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 3 },
                    IsHoverable = false
                };

                var valuesLine = data.Select(d => d.GlobalContribWCOri + d.GlobalContribWCInt + d.GlobalContribWCExtr).ToList();
                var lineSerie = new LineSeries<double>
                {
                    Name = "Outcome",
                    Fill = null,
                    Values = valuesLine,
                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                    GeometrySize = 10,
                    IsHoverable = false
                };
                // Assigner les séries au graphique (StackMode.Values est implicite par défaut)
                BarChart.Series = new ISeries[] { seriesOri, seriesInt, seriesExtr, outlineSeries, lineSerie };

                // Configurer les axes avec les étiquettes X et le titre Y
                ConfigureAxes(labels, "Contribution");
            }
            else
            {
                labels.AddRange(data.Select(d => d.NameData));

                // --- Séries de translation (mm) => axe Y principal ---
                var serieN = new StackedColumnSeries<double>
                {
                    Name = "N",
                    Values = data.Select(d => d.InfluencWCN).ToArray(),
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(style.ColumnFill1),
                    YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:F2}"
                };

                var serieT1 = new StackedColumnSeries<double>
                {
                    Name = "T1",
                    Values = data.Select(d => d.InfluencWCT1).ToArray(),
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(style.ColumnFill1),
                    YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:F2}"
                };

                var serieT2 = new StackedColumnSeries<double>
                {
                    Name = "T2",
                    Values = data.Select(d => d.InfluencWCT2).ToArray(),
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(style.ColumnFill1),
                    YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:F2}"
                };

                // --- Séries de rotation (mm/mrad) => axe Y secondaire + couleurs distinctes ---
                var serieRn = new StackedColumnSeries<double>
                {
                    Name = "Rn",
                    Values = data.Select(d => d.RotationInfluencWCN).ToArray(),
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(SKColors.Orange), // couleur propre
                    YToolTipLabelFormatter = p => $"Rn : {p.Coordinate.PrimaryValue:F2} mm/mrad"
                };

                var serieRT1 = new StackedColumnSeries<double>
                {
                    Name = "RT1",
                    Values = data.Select(d => d.RotationInfluencWCRT1).ToArray(),
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(SKColors.OrangeRed),
                    YToolTipLabelFormatter = p => $"RT1 : {p.Coordinate.PrimaryValue:F2} mm/mrad"
                };

                var serieRT2 = new StackedColumnSeries<double>
                {
                    Name = "RT2",
                    Values = data.Select(d => d.RotationInfluencWCRT2).ToArray(),
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = new SolidColorPaint(SKColors.DarkRed),
                    YToolTipLabelFormatter = p => $"RT2 : {p.Coordinate.PrimaryValue:F2} mm/mrad"
                };

                // --- Somme des 6 contributions : mélange volontaire d'unités (mm + mm/mrad) ---
                // Attention : cette valeur "Total" n'a pas d'unité physique cohérente
                // puisqu'elle additionne des mm et des mm/mrad. Elle sert uniquement
                // de repère visuel de hauteur cumulée, pas de valeur physique exploitable.
                var total = data.Select(d =>
                    d.InfluencWCN + d.InfluencWCT1 + d.InfluencWCT2 +
                    d.RotationInfluencWCN + d.RotationInfluencWCRT1 + d.RotationInfluencWCRT2
                ).ToArray();

                var outlineSeries = new ColumnSeries<double>
                {
                    Name = "Total",
                    Values = total,
                    MaxBarWidth = style.BarWidthThickness,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.Black) { StrokeThickness = 3 },
                    IsHoverable = false, // pas de tooltip : valeur composite, pas de sens physique isolé
                    ScalesYAt = 0
                };

                var series = new List<ISeries>
                            {
                                serieN, serieT1, serieT2,
                                serieRn, serieRT1, serieRT2,
                                outlineSeries
                            };

                if (!style.HideLineSerie)
                {
                    series.Add(new LineSeries<double>
                    {
                        Name = "Total (ligne)",
                        Values = total,
                        Fill = null,
                        Stroke = new SolidColorPaint(style.LineStroke) { StrokeThickness = style.LineStrokeThickness },
                        GeometryFill = new SolidColorPaint(style.LineGeometryFill),
                        GeometryStroke = new SolidColorPaint(style.LineGeometryStroke),
                        GeometrySize = style.LineGeometrySize,
                        IsHoverable = false, // idem : évite d'afficher une "valeur totale" trompeuse au survol
                        ScalesYAt = 0
                    });
                }

                BarChart.Series = series;

                // 2 axes Y : un pour les translations (mm), un pour les rotations (mm/mrad)
                BarChart.YAxes = new[]
                {
        new Axis { Name = "Influence (mm)" },
        new Axis { Name = "Influence rotation (mm/mrad)", Position = LiveChartsCore.Measure.AxisPosition.End }
    };

                ConfigureAxes(labels, "Influence");
            }

            // Ajuster la largeur minimale du graphique en fonction du nombre de points
            BarChart.MinWidth = Math.Max(800, data.Count * 80);
        }

        private void ConfigureAxes(List<string> labels, string yTitle)
        {
            // Axe X : catégorie
            BarChart.XAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
        new LiveChartsCore.SkiaSharpView.Axis
        {
            Name = "Linkages",
            Labels = labels,
            LabelsRotation = 45,
            LabelsPaint = new SolidColorPaint(SKColors.Black, 8)
        }
            };

            // Axe Y : valeur numérique
            BarChart.YAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
        new LiveChartsCore.SkiaSharpView.Axis
        {
            Name = yTitle,
            Labeler = value => value.ToString("F2", CultureInfo.InvariantCulture),
            LabelsPaint = new SolidColorPaint(SKColors.Black, 14), // 14 = taille du texte
            MinLimit = 0
        }
            };
        }

        #endregion

        #region Fonctions UI

        private async Task UpdateChartVisibility()
        {
            bool hasData = ModelManager.FilePathResx != null && ModelManager.FilePathResx.Any();

            NoDataImage.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
            BarChart.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;

            await LoadRequirementsToComboBox(ModelManager.FilePathResx);
        }

        private bool _isLoadingCombo = false;
        public async Task LoadRequirementsToComboBox(string filePath)
        {
            _isLoadingCombo = true;

            var headers = _resuxSerializer.ExtractReqHeaders(filePath);

            var items = headers
                .Select(h => new { Display = $"[{h.IdReq}] {h.Name}", Value = h.IdReq })
                .ToList();

            if (items == null || items.Count == 0)
            {
                ReqCombo.ItemsSource = null;
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ReqCombo.ItemsSource = items;
                ReqCombo.DisplayMemberPath = "Display";
                ReqCombo.SelectedValuePath = "Value";

                if (items.Count > 0)
                {
                    ReqCombo.SelectedIndex = 0;

                    if (!(ReqCombo.SelectedValue is int selectedIdReq))
                    {
                        return;
                    }
                    ApplicationDirectionRequirementInCB(selectedIdReq);
                    ReloadUI_Function();
                }

            });
            _isLoadingCombo = false;
        }

        private void CheckBoxFreeDir_Checked(object sender, RoutedEventArgs e)
        {
            UReq.IsReadOnly = false;
            VReq.IsReadOnly = false;
            WReq.IsReadOnly = false;
        }

        private async void CheckBoxFreeDir_Unchecked(object sender, RoutedEventArgs e)
        {

            // 1. Récupération de l'IdReq depuis la ComboBox
            if (!(ReqCombo.SelectedValue is int selectedIdReq))
            {
                MessageBox.Show("Veuillez sélectionner une exigence dans la liste.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            String filePath = ModelManager.FilePathResx;

            ApplicationDirectionRequirementInCB(selectedIdReq);

            ResuxSerializer.ResultsForReq projections = _resuxSerializer.LoadInfluencedWCFromFile(selectedIdReq, filePath);
            var (viewMode, calcMode) = GetSelectedOptions();
            await GlobalDisplayBarChart(projections, viewMode, calcMode);

        }

        private void ApplicationDirectionRequirementInCB(int idReq)
        {
            String filePath = ModelManager.FilePathResx;

            UReq.Clear();
            UReq.IsReadOnly = true;
            VReq.Clear();
            VReq.IsReadOnly = true;
            WReq.Clear();
            WReq.IsReadOnly = true;
            UReq.Text = _resuxSerializer.ReadValueFromFile(idReq, filePath, "CoordU")?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            VReq.Text = _resuxSerializer.ReadValueFromFile(idReq, filePath, "CoordV")?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            WReq.Text = _resuxSerializer.ReadValueFromFile(idReq, filePath, "CoordW")?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

        }


        private async Task ReloadUI_Function()
        {
            Debug.WriteLine("➡️ Enter ReloadUI_Function");

            // 1️⃣ Récupération de l'ID de l'exigence
            if (!(ReqCombo.SelectedValue is int selectedIdReq))
            {
                return;
            }
            try
            {
                // 2️⃣ Récupération du chemin du fichier
                string filePath = ModelManager.FilePathResx;

                // 3️⃣ Parsing des directions U, V, W
                double ux = ConversionHelper.TryParseInvariant(UReq.Text, double.NaN);
                double uy = ConversionHelper.TryParseInvariant(VReq.Text, double.NaN);
                double uz = ConversionHelper.TryParseInvariant(WReq.Text, double.NaN);

                if (!ValidateVector(ux, uy, uz))
                    return;

                // 4️⃣ Chargement des projections
                var projections = LoadProjections(selectedIdReq, filePath, ux, uy, uz);

                // 5️⃣ Sélection des modes d'affichage
                var (viewMode, calcMode) = GetSelectedOptions();
                await GlobalDisplayBarChart(projections, viewMode, calcMode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Une erreur est survenue lors du calcul des projections : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Méthodes auxiliaires

        private bool ValidateVector(double ux, double uy, double uz)
        {
            if (double.IsNaN(ux) || double.IsNaN(uy) || double.IsNaN(uz))
            {
                MessageBox.Show(
                    "Veuillez entrer des valeurs numériques valides pour U, V et W.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            double EPSILON = Constants.EPSILON;
            if (Math.Abs(ux) < EPSILON && Math.Abs(uy) < EPSILON && Math.Abs(uz) < EPSILON)
            {
                MessageBox.Show(
                    "Le vecteur directionnel (U, V, W) ne peut pas être tous zéros.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private ResuxSerializer.ResultsForReq LoadProjections(int idReq, string filePath, double ux, double uy, double uz)
        {
            return (CkeckBoxFreeDir.IsChecked == true)
                ? _resuxSerializer.LoadInfluencedWCFromFile(idReq, filePath, ux, uy, uz)
                : _resuxSerializer.LoadInfluencedWCFromFile(idReq, filePath);
        }

        #endregion

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("➡️ Enter RefreshButton_Click");
            // 1️⃣ Récupération de l'ID de l'exigence
            if (!(ReqCombo.SelectedValue is int selectedIdReq))
            {
                MessageBox.Show(
                    "Veuillez sélectionner une exigence dans la liste.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ReloadUI_Function();
        }

        private async void GlobalRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("➡️ Enter GlobalRefreshButton_Click");
            // 1️⃣ Récupération de l'ID de l'exigence
            if (!(ReqCombo.SelectedValue is int selectedIdReq))
            {
                return;
            }
            UpdateComboBoxItems();
            await ReloadUI_Function();
        }

        private async void BarChartSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BarChartSortComboBox.SelectedItem is ComboBoxItem selectedItem &&
                Enum.TryParse(selectedItem.Tag.ToString(), out BarChartSortOption option))
            {
                _barChartSortOption = option;
                await ReloadUI_Function();
            }
        }

        #region Parametre Bar Chart 

        // Centraliser tous les paramètres visuels du graphique (colonnes et lignes)
        public class ChartStyleSettings
        {
            // Colonnes
            public SKColor ColumnFill1 { get; set; }
            public SKColor ColumnFill2 { get; set; }
            public SKColor ColumnFill3 { get; set; }
            public SKColor ColumnOutline { get; set; }
            public float ColumnStrokeThickness { get; set; }
            public float BarWidthThickness { get; set; }

            // Lignes
            public SKColor LineStroke { get; set; }
            public float LineStrokeThickness { get; set; }
            public float LineGeometrySize { get; set; }
            public SKColor LineGeometryFill { get; set; } // Marqueur
            public SKColor LineGeometryStroke { get; set; } // Marqueur
            public bool HideLineSerie { get; set; }

            // Constructeur par défaut (fallbacks sécurisés)
            public ChartStyleSettings()
            {
                ColumnFill1 = SKColors.LawnGreen;
                ColumnFill2 = SKColors.Beige;
                ColumnFill3 = SKColors.Tan;
                ColumnOutline = SKColors.Black;
                ColumnStrokeThickness = 2f;

                BarWidthThickness = 2f;

                HideLineSerie = false;
                LineStroke = SKColors.Red;
                LineStrokeThickness = 2f;
                LineGeometrySize = 10f;
                LineGeometryFill = SKColors.Purple;
                LineGeometryStroke = SKColors.Red;

            }
        }

        // Créer une instance de ChartStyleSettings à partir des paramètres persistés
        private ChartStyleSettings LoadChartStyleSettings()
        {
            var set = BarChartSettings.Default;

            var settings = new ChartStyleSettings
            {
                #region Colonnes
                ColumnFill1 = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.ColumnFill1Argb,
                    SKColors.LawnGreen),

                ColumnFill2 = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.ColumnFill2Argb,
                    SKColors.Beige),

                ColumnFill3 = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.ColumnFill3Argb,
                    SKColors.Tan),

                // outline (color) and thickness
                ColumnOutline = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.ColumnOutlineArgb,
                    SKColors.Black),

                ColumnStrokeThickness = set.ColumnStrokeThickness > 0
                    ? (float)set.ColumnStrokeThickness
                    : 2f,

                BarWidthThickness = set.BarWidthThickness > 0
                    ? (float)set.BarWidthThickness
                    : 45f,


                #endregion

                #region Lines
                // lines
                LineStroke = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.LineStrokeArgb,
                    SKColors.Red),

                LineStrokeThickness = set.LineStrokeThickness > 0
                    ? (float)set.LineStrokeThickness
                    : 2f,

                #region Marqueur Line
                LineGeometrySize = set.LineGeometrySize > 0
                    ? (float)set.LineGeometrySize
                    : 10f,

                LineGeometryFill = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.LineGeometryFillArgb,
                    SKColors.Red),

                LineGeometryStroke = ColorStorageHelper.LoadSKColorFromSettings(
                    () => set.LineGeometryStrokeArgb,
                    SKColors.Red),

                HideLineSerie = set.HideLineSerie
                #endregion
                #endregion

            };

            return settings;
        }

        //
        private void OpenSupplementaryParams_Click(object sender, RoutedEventArgs e)
        {
            var settings = LoadChartStyleSettings();
            var window = new SupplementaryParamsBarChart(settings)
            {
                Owner = Window.GetWindow(this) // Centrer sur la fenêtre parent
            };

            if (window.ShowDialog() == true)
            {
                SaveChartStyleSettings(settings);
                ReloadUI_Function();
            }
        }

        // Écrire les valeurs de l’instance ChartStyleSettings dans BarChartSettings.Default pour persistance
        private void SaveChartStyleSettings(ChartStyleSettings style)
        {
            var set = BarChartSettings.Default;

            // Colonnes
            set.ColumnFill1Argb = ColorStorageHelper.ArgbFromSKColor(style.ColumnFill1);
            set.ColumnFill2Argb = ColorStorageHelper.ArgbFromSKColor(style.ColumnFill2);
            set.ColumnFill3Argb = ColorStorageHelper.ArgbFromSKColor(style.ColumnFill3);
            set.ColumnOutlineArgb = ColorStorageHelper.ArgbFromSKColor(style.ColumnOutline);
            set.ColumnStrokeThickness = style.ColumnStrokeThickness;

            set.BarWidthThickness = style.BarWidthThickness;

            // Lignes
            set.LineStrokeArgb = ColorStorageHelper.ArgbFromSKColor(style.LineStroke);
            set.LineStrokeThickness = style.LineStrokeThickness;
            set.LineGeometrySize = style.LineGeometrySize;
            set.LineGeometryFillArgb = ColorStorageHelper.ArgbFromSKColor(style.LineGeometryFill);
            set.LineGeometryStrokeArgb = ColorStorageHelper.ArgbFromSKColor(style.LineGeometryStroke);

            set.HideLineSerie = style.HideLineSerie;

            set.Save(); // persistance .NET Settings
        }

        #endregion


        #region RadioButton

        public enum ViewMode
        {
            Influence,
            Contribution
        }

        public enum CalculationMode
        {
            WorstCase,
            Statistical
        }
        private (ViewMode viewMode, CalculationMode calcMode) GetSelectedOptions()
        {
            ViewMode viewMode = BoxInfl.IsChecked == true ? ViewMode.Influence : ViewMode.Contribution;
            CalculationMode calcMode = BoxWC.IsChecked == true ? CalculationMode.WorstCase : CalculationMode.Statistical;

            return (viewMode, calcMode);
        }


        private void UpdateComboBoxItems()
        {
            if (BarChartSortComboBox == null) return;

            bool showInfluence = BoxInfl.IsChecked == true;

            foreach (ComboBoxItem item in BarChartSortComboBox.Items)
            {
                string tag = item.Tag?.ToString() ?? "";

                // Activer uniquement les options correspondant au mode sélectionné
                if (showInfluence)
                {
                    // Activer uniquement les options d'influence
                    item.IsEnabled = tag == "ByMaxInfl" || tag == "ByMinInfl";
                }
                else
                {
                    // Activer uniquement les options de contribution
                    item.IsEnabled = tag == "ByContribOri" ||
                                     tag == "ByContribInt" ||
                                     tag == "ByContribExtr" ||
                                     tag == "ByContrib";
                }
            }

            // sélectionner la première option disponible automatiquement
            if (BarChartSortComboBox.SelectedItem == null || !((ComboBoxItem)BarChartSortComboBox.SelectedItem).IsEnabled)
            {
                BarChartSortComboBox.SelectedItem = BarChartSortComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.IsEnabled);
            }
        }

        //Lance un calcul lors du changement de Req via la Combobox
        private void ReqCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("➡️ Enter ReqCombo_SelectionChanged");
            if (_isLoadingCombo)
                return; //  Empêche le déclenchement pendant le chargement
            if (sender is ComboBox comboBox && comboBox.SelectedValue is int)
            {
                String filePath = ModelManager.FilePathResx;

                LoadProjectionsFromComboBox(filePath);

            }
        }

        // Fonction qui charge l'ui avec les résultats du Req choisi
        public void LoadProjectionsFromComboBox(string filePath)
        {
            if (ReqCombo.SelectedValue is int selectedIdReq)
            {
                ResuxSerializer.ResultsForReq projections = _resuxSerializer.LoadInfluencedWCFromFile(selectedIdReq, filePath);
                var (viewMode, calcMode) = GetSelectedOptions();
                GlobalDisplayBarChart(projections, viewMode, calcMode);


                UReq.Clear();
                VReq.Clear();
                WReq.Clear();
                UReq.Text = _resuxSerializer.ReadValueFromFile(selectedIdReq, filePath, "CoordU")?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                VReq.Text = _resuxSerializer.ReadValueFromFile(selectedIdReq, filePath, "CoordV")?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                WReq.Text = _resuxSerializer.ReadValueFromFile(selectedIdReq, filePath, "CoordW")?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

            }
            else
            {
                MessageBox.Show("Aucune exigence sélectionnée dans la liste.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion





    }
}

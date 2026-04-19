using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using TOLTECH_APPLICATION.Views;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.ToltechCalculation.Resux;
using static TOLTECH_APPLICATION.Views.PageResultats;
using static TOLTECH_APPLICATION.ToltechCalculation.Resux.ResuxSerializer;
using TOLTECH_APPLICATION.Utilities;

namespace TOLTECH_APPLICATION.FrontEnd.Controls.Dashboard
{
    public partial class BChartReqs : UserControl
    {
        private ResuxSerializer _resuxSerializer;
        public BChartReqs()
        {
            InitializeComponent();
            _resuxSerializer = new ResuxSerializer();

            UpdateChartVisibility();
            ModelManager.FilePathResxChanged += filresx =>
            {
                Dispatcher.Invoke(UpdateChartVisibility);
            };
        }

        public PageResultats ParentPage { get; set; }

        // cache pour réutiliser les résultats
        private readonly Dictionary<int, ResuxSerializer.ResultsForReq> _allReqLibrary = new();

        /// <summary>
        /// Handler appelé par l'UI pour charger TOUTES les exigences du fichier.
        /// </summary>
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            LoadBartChartReqs();
        }

        public void LoadBartChartReqs()
        {
            if (ParentPage == null) return;

            var filePath = ModelManager.FilePathResx;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            var allIds = _resuxSerializer.GetAllReqIdsFromFile(filePath);
            if (allIds.Count == 0) return;

            _allReqLibrary.Clear();

            foreach (var idReq in allIds)
            {
                try
                {
                    var result = _resuxSerializer.LoadInfluencedWCFromFile(idReq, filePath, Ucoord: 0, Vcoord: 0, Wcoord: 0);
                    if (result != null)
                    {
                        _allReqLibrary[idReq] = result;
                    }
                }
                catch (Exception)
                {
                    // TODO: log si besoin
                }
            }

            if (_allReqLibrary.Count > 0)
            {
                DisplayBarChartForAllReqs(_allReqLibrary.Values);
            }
        }


        /// <summary>
        /// Affiche une barre par exigence (IdReq) avec la somme des contributions.
        /// </summary>
        private void DisplayBarChartForAllReqs(IEnumerable<ResuxSerializer.ResultsForReq> allResults)
        {
            if (allResults == null || !allResults.Any())
            {
                BarChartReqs.Series = Array.Empty<ISeries>();
                BarChartReqs.XAxes = Array.Empty<Axis>();
                BarChartReqs.YAxes = Array.Empty<Axis>();
                return;
            }

            // Calcul contributions absolues pour chaque exigence
            var reqContribs = allResults
                .Where(req => req.Data != null && req.Data.Count > 0)
                .Select(req => new
                {
                    req.IdReq,
                    reqName = req.NameReq,
                    TotalContrib = req.Data.Sum(d =>
                        Math.Abs(d.ContribWCOri) +
                        Math.Abs(d.ContribWCInt) +
                        Math.Abs(d.ContribWCExtr)),
                     req.TargetWC,
                     req.TargetSTAT
                });

            // Application du filtre/tri selon l’option
            reqContribs = _barChartReqsOption switch
            {
                BarChartReqsOption.ByMax => reqContribs.OrderByDescending(r => r.TotalContrib),
                BarChartReqsOption.ByMin => reqContribs.OrderBy(r => r.TotalContrib),
                _ => reqContribs
            };

            var labels = reqContribs.Select(r => $"{r.reqName}").ToList();
            var totals = reqContribs.Select(r => r.TotalContrib).ToList();
            var targetWC = reqContribs.Select(r => r.TargetWC).ToList();
            var targetStat = reqContribs.Select(r => r.TargetSTAT).ToList();

            var wpfColor = (System.Windows.Media.Color)Application.Current.Resources["PrimaryForegroundColor"];

            var skColor = new SKColor(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);

            // Construction de la série
            var totalSeries = new ColumnSeries<double>
            {
                Name = "Contribution",
                Values = totals,
                MaxBarWidth = 45,
                DataLabelsPaint = new SolidColorPaint(skColor),  
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top, 
                DataLabelsFormatter = point => point.Coordinate.PrimaryValue.ToString("F2", CultureInfo.InvariantCulture)  
            };

            // Ligne horizontale par colonne pour TargetWC
            var targetWCSeries = new LineSeries<double>
            {
                Name = "Target WC",
                Values = targetWC,
                Stroke = new SolidColorPaint(skColor, 2),
                Fill = null, // pas de remplissage sous la courbe
                GeometrySize = 0, // pas de points visibles
                LineSmoothness = 0 // ligne droite (pas de courbe)
            };

            // Ligne horizontale par colonne pour TargetSTAT
            var targetStatSeries = new LineSeries<double>
            {
                Name = "Target STAT",
                Values = targetStat,
                Stroke = new SolidColorPaint(SKColors.Red, 2),
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0
            };

            // Ajout des séries
            BarChartReqs.Series = new ISeries[]
            {
                totalSeries,
                //targetWCSeries,
                //targetStatSeries
            };

            ConfigureAxes(labels, "Contribution");

            BarChartReqs.MinWidth = Math.Max(800, totals.Count * 80);
        }


        private void ConfigureAxes(List<string> labels, string yTitle)
        {
            var wpfColor = (System.Windows.Media.Color)Application.Current.Resources["PrimaryForegroundColor"];
            var skColor = new SKColor(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);

            BarChartReqs.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels.ToArray(),
                    LabelsRotation = 45,
                    LabelsPaint = new SolidColorPaint(skColor, 12)
                }
            };

            BarChartReqs.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = yTitle,
                    Labeler = value => value.ToString("F3", CultureInfo.InvariantCulture),
                    LabelsPaint = new SolidColorPaint(skColor, 12)
                }
            };
        }


        private void SetFilterOption(BarChartReqsOption option)
        {
            _barChartReqsOption = option;

            if (_allReqLibrary.Any())
            {
                DisplayBarChartForAllReqs(_allReqLibrary.Values);
            }
        }


        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
            {
                if (Enum.TryParse(item.Tag.ToString(), out BarChartReqsOption option))
                {
                    SetFilterOption(option);
                }
            }
        }


        private BarChartReqsOption _barChartReqsOption = BarChartReqsOption.Default;
        public enum BarChartReqsOption
        {
            Default,
            ByMax,
            ByMin
        }

        private void UpdateChartVisibility()
        {
            bool hasData = ModelManager.FilePathResx != null && ModelManager.FilePathResx.Any();

            NoDataImage.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
            BarChartReqs.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;

        }
    }
}

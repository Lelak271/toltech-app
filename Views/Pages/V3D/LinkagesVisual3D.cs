using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;

using Toltech.App.Models;
using Toltech.App.Views.Pages.V3D;
using Toltech.Solver.Contracts;

namespace Toltech.App.Views
{
    public class LinkagesVisual3D
    {
        public ModelUIElement3D Visual { get; }

        /// <summary>
        /// Donnée métier liée à ce symbole 3D : ModelData (liaison) ou
        /// Requirements (exigence). Toute lecture spécifique passe par
        /// pattern matching (voir LinkedOriginalId, ExtremitePartId,
        /// RefreshTooltip).
        /// </summary>
        public object LinkedLinkage { get; }

        public int LinkedOriginalId => LinkedLinkage switch
        {
            ModelData md => md.Id,
            Requirements req => req.Id_req,
            _ => throw new InvalidOperationException(
                $"Type de donnée liée non pris en charge : {LinkedLinkage?.GetType().Name ?? "null"}")
        };

        public int? ExtremitePartId => LinkedLinkage switch
        {
            ModelData md => md.ExtremitePartId,
            Requirements req => req.PartReq1Id,
            _ => null
        };

        /// <summary>
        /// Type de liaison, utilisé notamment pour l'étiquette "Type" et
        /// pour retrouver le template .obj chargé.
        /// </summary>
        public LinkageType Linkage { get; }

        /// <summary>
        /// Point d'origine (placement) du symbole dans la scène — sert de
        /// point d'ancrage pour l'étiquette flottante (BillboardTextVisual3D)
        /// gérée par le ViewModel.
        /// </summary>
        public Point3D Origin { get; }

        public Color CurrentColor { get; private set; }

        /// <summary>
        /// Opacité courante (0 = invisible, 1 = opaque). Combinée à
        /// CurrentColor dans ApplyMaterial pour produire le matériau final.
        /// </summary>
        public double CurrentOpacity { get; private set; } = 1.0;

        private readonly GeometryModel3D _geometryModel;

        private readonly Popup _tooltipPopup;

        private DispatcherTimer? _tooltipTimer;


        public LinkagesVisual3D(
            object data,
            GeometryModel3D geometryModel,
            Color color,
            Point3D origin,
            LinkageType linkage)
        {
            LinkedLinkage = data
                ?? throw new ArgumentNullException(nameof(data));

            _geometryModel =
                geometryModel
                ?? throw new ArgumentNullException(nameof(geometryModel));

            Origin = origin;
            Linkage = linkage;
            CurrentColor = color;

            ApplyMaterial();

            // --------------------------------------------------------
            // Visuel 3D
            // --------------------------------------------------------

            Visual = new ModelUIElement3D
            {
                Model = _geometryModel
            };

            // --------------------------------------------------------
            // Popup
            // --------------------------------------------------------

            _tooltipPopup = new Popup
            {
                AllowsTransparency = true,
                Focusable = false,
                IsOpen = false,
                Placement = PlacementMode.MousePoint,
                IsHitTestVisible = false,
                PopupAnimation = PopupAnimation.Fade,
                StaysOpen = true
            };

            // --------------------------------------------------------
            // Événements souris
            // --------------------------------------------------------

            Visual.MouseEnter += OnMouseEnter;
            Visual.MouseLeave += OnMouseLeave;

            RefreshTooltip();
        }

        public void UpdateColor(Color newColor)
        {
            CurrentColor = newColor;
            ApplyMaterial();
            RefreshTooltip();
        }

        /// <summary>
        /// Change l'opacité du symbole (0-1) sans toucher à sa couleur ni à
        /// sa géométrie. Utilisé par V3DViewModel.SymbolOpacity.
        /// </summary>
        public void UpdateOpacity(double opacity)
        {
            CurrentOpacity = Math.Clamp(opacity, 0.0, 1.0);
            ApplyMaterial();
        }

        /// <summary>
        /// Reconstruit le matériau à partir de CurrentColor + CurrentOpacity.
        /// Point d'entrée unique pour toute mise à jour visuelle du matériau,
        /// afin que couleur et opacité ne s'écrasent jamais l'une l'autre.
        /// </summary>
        private void ApplyMaterial()
        {
            byte alpha = (byte)Math.Round(Math.Clamp(CurrentOpacity, 0.0, 1.0) * 255.0);
            var finalColor = Color.FromArgb(alpha, CurrentColor.R, CurrentColor.G, CurrentColor.B);
            _geometryModel.Material = MaterialHelper.CreateMaterial(finalColor);
        }

        #region Tooltip

        public void RefreshTooltip()
        {
            TooltipInfo info = LinkedLinkage switch
            {
                ModelData md => new TooltipInfo
                {
                    Id = md.Id,
                    Name = md.Model,
                    Part1Id = md.ExtremitePartId ?? 0,
                    Part2Id = md.OriginePartId,
                    Linkage = md.Linkage,
                    Position = FormatVector(md.CoordX, md.CoordY, md.CoordZ),
                    Direction = FormatVector(md.CoordU, md.CoordV, md.CoordW),
                    Color = new SolidColorBrush(CurrentColor),
                    ColorHex = GetColorHex(CurrentColor)
                },

                // NOTE : noms de propriétés supposés d'après l'usage dans
                // V3DViewModel (Id_req, PartReq1Id, CoordX/Y/Z/U/V/W).
                // Ajustez si "Model"/second id diffèrent chez vous.
                Requirements req => new TooltipInfo
                {
                    Id = req.Id_req,
                    Name = $"Exigence {req.Id_req}",
                    Part1Id = req.PartReq1Id ?? 0,
                    Part2Id = 0,
                    Linkage = Linkage,
                    Position = FormatVector(req.CoordX, req.CoordY, req.CoordZ),
                    Direction = FormatVector(req.CoordU, req.CoordV, req.CoordW),
                    Color = new SolidColorBrush(CurrentColor),
                    ColorHex = GetColorHex(CurrentColor)
                },

                _ => throw new InvalidOperationException(
                    $"Type de donnée liée non pris en charge : {LinkedLinkage?.GetType().Name ?? "null"}")
            };

            var tooltip = new ToolTip_Vector
            {
                DataContext = info
            };

            _tooltipPopup.Child = tooltip;
        }

        private static string FormatVector(double x, double y, double z)
            => $"({x:F2} ; {y:F2} ; {z:F2})";

        private static string GetColorHex(Color color)
            => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        #endregion

        #region Mouse

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _tooltipTimer?.Stop();

            _tooltipTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            _tooltipTimer.Tick += OnTooltipTimerTick;
            _tooltipTimer.Start();
        }

        private void OnTooltipTimerTick(object? sender, EventArgs e)
        {
            _tooltipTimer?.Stop();
            RefreshTooltip();
            _tooltipPopup.IsOpen = true;
        }

        public bool _isMouseOver;
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            _tooltipTimer?.Stop();
            _tooltipPopup.IsOpen = false;
        }

        #endregion
    }

    public sealed class TooltipInfo
    {
        public int Id { get; init; }
        public int Part1Id { get; init; } = 0;
        public int Part2Id { get; init; } = 0;
        public LinkageType Linkage { get; init; } = 0;
        public string Name { get; init; } = string.Empty;
        public string Position { get; init; } = string.Empty;
        public string Direction { get; init; } = string.Empty;
        public Brush Color { get; init; } = Brushes.Transparent;
        public string ColorHex { get; init; } = string.Empty;
        public string Title => $"Liaison {Id}";
    }
}
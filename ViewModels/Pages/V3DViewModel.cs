using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using DocumentFormat.OpenXml.EMMA;
using HelixToolkit.Wpf;
using LiveChartsCore.VisualElements;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.Utilities.Object3D;
using Toltech.Solver.Contracts;
using static Toltech.App.Services.EventsManager;
using TtCore = Toltech.App.ViewModels;

namespace Toltech.App.ViewModels
{

    /// <summary>Mode de rendu appliqué au modèle importé via ImportObjCommand.</summary>
    public enum ModelRenderMode
    {
        Solide,
        Filaire,
        Points
    }

    public partial class V3DViewModel : BaseViewModel
    {
        #region Fields

        private readonly MainViewModel _mainVM;
        public MainViewModel MainVM => _mainVM;

        public ObservableCollection<Part> Parts => MainVM.Parts;

        private readonly DomainService _domainService;

        private readonly string _linkageTemplateDirectory;

        private static readonly Color[] _partColorPalette =
        {
            Colors.RoyalBlue, Colors.OrangeRed, Colors.MediumSeaGreen, Colors.Gold,
            Colors.MediumPurple, Colors.Turquoise, Colors.Crimson, Colors.SaddleBrown,
            Colors.DeepPink, Colors.DarkCyan
        };

        private readonly Dictionary<int, Color> _partColorCache = new Dictionary<int, Color>();

        private readonly Dictionary<int, List<Views.LinkagesVisual3D>> _partArrows = new Dictionary<int, List<Views.LinkagesVisual3D>>();

        private readonly Dictionary<ModelUIElement3D, Views.LinkagesVisual3D> _arrowsByVisual = new Dictionary<ModelUIElement3D, Views.LinkagesVisual3D>();

        #endregion

        #region Legend / part colors

        public ObservableCollection<PartColorEntry> PartColors { get; } = new ObservableCollection<PartColorEntry>();

        public ICommand ChangePartColorCommand { get; }

        #endregion

        #region Bindable properties

        public ObservableCollection<Visual3D> Visuals { get; } = new ObservableCollection<Visual3D>();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }


        private readonly Dictionary<LinkageType, double> _templateMaxDimensionCache = new Dictionary<LinkageType, double>();

        private readonly Dictionary<Views.LinkagesVisual3D, ArrowScaleInfo> _arrowScaleInfo =
            new Dictionary<Views.LinkagesVisual3D, ArrowScaleInfo>();

        private readonly struct ArrowScaleInfo
        {
            public readonly ScaleTransform3D Transform;
            public readonly double NormalizationFactor;

            public ArrowScaleInfo(ScaleTransform3D transform, double normalizationFactor)
            {
                Transform = transform;
                NormalizationFactor = normalizationFactor;
            }
        }


        #region CAD Objects

        /// <summary>
        /// Associe un CadObject à ses visuels 3D, Remplace toute association précédente
        /// pour ce CadObject.
        /// </summary>
        public void SetCadObjectVisuals(CadObject cadObject, IEnumerable<Visual3D> visuals)
        {
            if (cadObject == null)
                throw new ArgumentNullException(nameof(cadObject));

            foreach (var oldVisual in cadObject.Visuals)
                Visuals.Remove(oldVisual);

            _cadScene.SetVisuals(cadObject, visuals);

            // Ajoute les nouveaux visuels à la scène affichée.
            foreach (var visual in cadObject.Visuals)
                Visuals.Add(visual);
        }

        /// <summary>
        /// Retire tous les visuels associés au CadObject donné, de la scène et
        /// des deux tables de correspondance. Ne fait rien si le CadObject
        /// n'a aucun visuel enregistré.
        /// </summary>
        public void RemoveCadObjectOnScene(CadObject cadObject)
        {
            if (cadObject == null)
                return;

            foreach (var visual in cadObject.Visuals)
                Visuals.Remove(visual);

            _cadScene.RemoveObject(cadObject);
            cadObject.Visuals.Clear();
        }

        /// <summary>
        /// Retrouve le CadObject associé à un Visual3D, typiquement à partir
        /// du résultat d'un Viewport3DHelper.FindNearest lors du picking.
        /// </summary>
        public CadObject FindCadObject(Visual3D visual)
     => _cadScene.TryGetObject(visual, out var obj) ? obj : null;


        #endregion

        #region Binding Properties

        private const double DefaultSymbolSize = 50.0;

        private double _symbolSize = DefaultSymbolSize;
        public double SymbolSize
        {
            get => _symbolSize;
            set
            {
                double clamped = value <= 0 ? DefaultSymbolSize : value;
                if (SetProperty(ref _symbolSize, clamped))
                    UpdateAllArrowScales(clamped);
            }
        }


        private double _symbolOpacity = 1.0;
        public double SymbolOpacity
        {
            get => _symbolOpacity;
            set
            {
                double clamped = Math.Clamp(value, 0.1, 1.0);
                if (SetProperty(ref _symbolOpacity, clamped))
                    UpdateAllArrowOpacities(clamped);
            }
        }


        private GridLinesVisual3D _gridVisual;

        private bool _showGrid;
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                if (SetProperty(ref _showGrid, value))
                    UpdateGridVisibility(value);
            }
        }

        private ModelRenderMode _currentRenderMode = ModelRenderMode.Solide;
        public ModelRenderMode CurrentRenderMode
        {
            get => _currentRenderMode;
            set
            {
                if (SetProperty(ref _currentRenderMode, value))
                    RefreshImportedModelRenderMode();
            }
        }

        #endregion
      
        #endregion

        #region Commands

        public ICommand ImportObjCommand { get; }
        public ICommand LoadArrowsCommand { get; }
        public ICommand ClearAllSceneCommand { get; }
        public ICommand ClearAllModelTolerancingCommand { get; }
        public ICommand ClearAllCadObjectCommand { get; }
        public ICommand ClearSelectCadObjectCommand { get; }
        public ICommand SetSymbolSizeCommand { get; }

        public Func<string> RequestObjFilePath { get; set; }
        public Func<Color, Color?> RequestColorPicker { get; set; }
        public Action<string, string> ShowError { get; set; }

        #endregion

        public V3DViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _domainService = mainVM.DomainService;
            _cadPicker = new CadPicker(_cadScene);

            InitializeCadCommands();

            #region ICommand
            ImportObjCommand = new TtCore.RelayCommand(ImportObj);
            LoadArrowsCommand = new TtCore.AsyncRelayCommand(Load3DFromDatabaseAsync, () => !IsBusy);
            ClearAllSceneCommand = new TtCore.RelayCommand(ClearAllScene);
            ClearAllModelTolerancingCommand = new TtCore.RelayCommand(ClearAllModelTolerancing);
            ClearAllCadObjectCommand = new TtCore.RelayCommand(ClearAllCadObject);
            ClearSelectCadObjectCommand = new TtCore.RelayCommand<CadObject>(cadObject => ClearSelectCadObject(cadObject));
            ChangePartColorCommand = new TtCore.RelayCommand<PartColorEntry>(ChangePartColor);
            SetSymbolSizeCommand = new TtCore.RelayCommand<double>(size => SymbolSize = size);
            #endregion

            //_cadScene.SelectionChanged += obj =>
            //{
            //    OnPropertyChanged(nameof(SelectedCadObject));
            //    OnPropertyChanged(nameof(SelectedCadObjectColor));
            //};

            _linkageTemplateDirectory = ((App)System.Windows.Application.Current).TemplateDirectory; // TODO revoir l'appel
        }

        #region Import OBJ

        private void ImportObj()
        {
            string filePath = RequestObjFilePath?.Invoke();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            try
            {
                var reader = new ObjReader();
                Model3DGroup model = reader.Read(filePath);

                var cadObject = _cadScene.AddObject(filePath, model);
                _cadScene.SetColor(cadObject, Colors.LightGray);
                AddImportedModelVisual(cadObject);

                StatusMessage = $"OBJ chargé : {cadObject.Name}";
            }
            catch (Exception ex)
            {
                ShowError?.Invoke(
                    $"Erreur lors du chargement du modèle :\n{ex.Message}",
                    "Erreur");
            }
        }

        /// <summary>
        /// Ajoute la représentation d'un objet importé selon CurrentRenderMode.
        /// Les visuels sont également enregistrés dans CadScene afin que le picking
        /// puisse retrouver l'objet métier correspondant au visuel Helix.
        /// </summary>
        private void AddImportedModelVisual(CadObject cadObject)
        {
            if (cadObject?.Model == null)
                return;

            IEnumerable<Visual3D> visuals;
            switch (CurrentRenderMode)
            {
                case ModelRenderMode.Solide:
                    visuals = new[] { new ModelVisual3D { Content = cadObject.Model } };
                    break;
                case ModelRenderMode.Filaire:
                    visuals = BuildWireframeVisuals(cadObject.Model);
                    break;
                case ModelRenderMode.Points:
                    visuals = BuildPointVisuals(cadObject.Model);
                    break;
                default:
                    visuals = Enumerable.Empty<Visual3D>();
                    break;
            }

            SetCadObjectVisuals(cadObject, visuals.ToList());
        }

        private void RefreshImportedModelRenderMode()
        {
            foreach (var cadObject in _cadScene.Objects.ToList())
                AddImportedModelVisual(cadObject);
        }

        private IEnumerable<Visual3D> BuildWireframeVisuals(Model3DGroup modelGroup)
        {
            foreach (var geometryModel in modelGroup.Children.OfType<GeometryModel3D>())
            {
                if (geometryModel.Geometry is not MeshGeometry3D mesh)
                    continue;

                var points = new Point3DCollection();

                for (int i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
                {
                    var p0 = mesh.Positions[mesh.TriangleIndices[i]];
                    var p1 = mesh.Positions[mesh.TriangleIndices[i + 1]];
                    var p2 = mesh.Positions[mesh.TriangleIndices[i + 2]];

                    points.Add(p0); points.Add(p1);
                    points.Add(p1); points.Add(p2);
                    points.Add(p2); points.Add(p0);
                }

                yield return new LinesVisual3D
                {
                    Points = points,
                    Thickness = 1,
                    Color = Colors.Black
                };
            }
        }

        private IEnumerable<Visual3D> BuildPointVisuals(Model3DGroup modelGroup)
        {
            foreach (var geometryModel in modelGroup.Children.OfType<GeometryModel3D>())
            {
                if (geometryModel.Geometry is not MeshGeometry3D mesh)
                    continue;

                yield return new PointsVisual3D
                {
                    Points = mesh.Positions,
                    Size = 3,
                    Color = Colors.Black
                };
            }
        }

        #endregion

        #region Linakges depuis la base

        /// <summary>
        /// Fonction principale pour l'import de la base de tolérnace
        /// </summary>
        private async Task Load3DFromDatabaseAsync()
        {
            if (_domainService == null)
                return;

            IsBusy = true;
            StatusMessage = "Chargement des flèches...";

            try
            {
                var linkages = await _domainService.GetActivePartsModelDataAsync();
                var requirements = await _domainService.GetAllRequirementsAsync();
                int addedCount = 0;

                foreach (var link in linkages.Value)
                {
                    var direction = new Vector3D(link.CoordU, link.CoordV, link.CoordW);

                    if (direction.Length > 0.0001)
                    {
                        var color = GetColorForPart(link.ExtremitePartId.Value);
                        var model3D = Get3DModel(link, color);
                        Visuals.Add(model3D.Visual);
                        addedCount++;
                    }
                    else
                    {
                        Debug.WriteLine($"Vecteur nul ignoré pour l'élément ID={link.Id}");
                    }
                }

                foreach (var req in requirements.Value)
                {
                    var direction = new Vector3D(req.CoordU, req.CoordV, req.CoordW);

                    if (direction.Length > 0.0001)
                    {
                        var color = Color.FromArgb(255, 220, 40, 40);
                        var model3D = Get3DModel(req, color);
                        Visuals.Add(model3D.Visual);
                        addedCount++;
                    }
                    else
                    {
                        Debug.WriteLine($"Vecteur nul ignoré pour l'élément ID={req.Id_req}");
                    }
                }

                StatusMessage = $"{addedCount} liaisons chargée(s).";
            }
            catch (Exception ex)
            {
                ShowError?.Invoke($"Erreur lors du chargement des flèches : {ex.Message}", "Erreur");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Color GetColorForPart(int extremitePartId)
        {
            if (_partColorCache.TryGetValue(extremitePartId, out var cachedColor))
                return cachedColor;

            var color = _partColorPalette[_partColorCache.Count % _partColorPalette.Length];
            _partColorCache[extremitePartId] = color;

            var entry = new PartColorEntry(extremitePartId, color);
            entry.PropertyChanged += PartColorEntry_PropertyChanged;
            PartColors.Add(entry);

            return color;
        }

        private void ChangePartColor(PartColorEntry entry)
        {
            if (entry == null)
                return;

            var picked = RequestColorPicker?.Invoke(entry.Color);
            if (picked == null)
                return;

            var newColor = picked.Value;
            entry.Color = newColor;
            _partColorCache[entry.PartId] = newColor;

            if (_partArrows.TryGetValue(entry.PartId, out var arrows))
            {
                foreach (var arrow in arrows)
                    arrow.UpdateColor(newColor);
            }
        }

        private void PartColorEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PartColorEntry.IsVisible) && sender is PartColorEntry entry)
                UpdatePartVisibility(entry);
        }

        private void UpdatePartVisibility(PartColorEntry entry)
        {
            if (!_partArrows.TryGetValue(entry.PartId, out var arrows))
                return;

            var visibility = entry.IsVisible ? Visibility.Visible : Visibility.Collapsed;

            foreach (var arrow in arrows)
            {
                arrow.Visual.Visibility = visibility;
            }
        }

        private string GetLinkageTemplatePath(LinkageType linkageType)
        {
            return Path.Combine(_linkageTemplateDirectory, $"{linkageType}.obj");
        }

        /// <summary>
        /// Generation de {LinkagesVisual3D} provenant de ModelData 
        /// </summary>
        private Views.LinkagesVisual3D Get3DModel(ModelData modelData, Color color)
        {
            var direction = new Vector3D(modelData.CoordU, modelData.CoordV, modelData.CoordW);
            var origin = new Point3D(modelData.CoordX, modelData.CoordY, modelData.CoordZ);

            return Get3DModelCore(
                linkedData: modelData,
                linkage: modelData.Linkage,
                origin: origin,
                direction: direction,
                extremitePartId: modelData.ExtremitePartId,
                color: color);
        }

        /// <summary>
        /// Generation de {LinkagesVisual3D} provenant de Requirement 
        /// </summary>
        private Views.LinkagesVisual3D Get3DModel(Requirements requirement, Color color)
        {
            var direction = new Vector3D(requirement.CoordU, requirement.CoordV, requirement.CoordW);
            var origin = new Point3D(requirement.CoordX, requirement.CoordY, requirement.CoordZ);

            return Get3DModelCore(
                linkedData: requirement,
                linkage: LinkageType.Requirement,
                origin: origin,
                direction: direction,
                extremitePartId: requirement.PartReq1Id,
                color: color);
        }

        /// <summary>
        /// Generation type de {LinkagesVisual3D} provenant de ModelData ou Requirement 
        /// </summary>
        private Views.LinkagesVisual3D Get3DModelCore(
            object linkedData,
            LinkageType linkage,
            Point3D origin,
            Vector3D direction,
            int? extremitePartId,
            Color color)
        {
            string templatePath = GetLinkageTemplatePath(linkage);

            if (direction.Length < 0.0001)
                throw new InvalidOperationException("La direction de la liaison est nulle.");

            direction.Normalize();

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException(
                    $"Le template 3D de la liaison '{linkage}' est introuvable.",
                    templatePath);
            }

            var reader = new ObjReader();
            Model3DGroup model = reader.Read(templatePath);

            var geometryModel = model.Children.OfType<GeometryModel3D>().FirstOrDefault();

            if (geometryModel == null)
                throw new InvalidOperationException($"Le template '{templatePath}' ne contient aucune GeometryModel3D.");

            double normFactor = GetNormalizationFactor(linkage, geometryModel);
            double initialScale = SymbolSize * normFactor;

            var scaleTransform = new ScaleTransform3D(initialScale, initialScale, initialScale);
            geometryModel.Transform = CreateTemplateTransform(origin, direction, scaleTransform);

            var arrow = new Views.LinkagesVisual3D(linkedData, geometryModel, color, origin, linkage);
            arrow.UpdateOpacity(SymbolOpacity);

            arrow.Visual.MouseLeftButtonDown += Arrow_MouseLeftButtonDown;
            _arrowsByVisual[arrow.Visual] = arrow;
            _arrowScaleInfo[arrow] = new ArrowScaleInfo(scaleTransform, normFactor);

            if (extremitePartId.HasValue)
            {
                int partId = extremitePartId.Value;
                if (!_partArrows.TryGetValue(partId, out var list))
                {
                    list = new List<Views.LinkagesVisual3D>();
                    _partArrows[partId] = list;
                }
                list.Add(arrow);

                // Respecte l'état de visibilité déjà défini pour cette pièce
                // (ex. rechargement alors qu'elle avait été masquée avant).
                var existingEntry = PartColors.FirstOrDefault(p => p.PartId == partId);
                if (existingEntry != null && !existingEntry.IsVisible)
                    arrow.Visual.Visibility = Visibility.Collapsed;
            }

            return arrow;
        }

        private static Transform3D CreateTemplateTransform(
            Point3D origin,
            Vector3D direction,
            ScaleTransform3D scaleTransform)
        {
            var transform = new Transform3DGroup();
            transform.Children.Add(scaleTransform);

            Vector3D templateAxis = new Vector3D(0, 0, 1);
            Vector3D axis = Vector3D.CrossProduct(templateAxis, direction);
            double dot = Math.Clamp(Vector3D.DotProduct(templateAxis, direction), -1.0, 1.0);
            double angle = Math.Acos(dot) * 180.0 / Math.PI;

            if (axis.Length > 0.0001)
            {
                axis.Normalize();
                transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(axis, angle)));
            }
            else if (dot < 0)
            {
                transform.Children.Add(
                    new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
            }

            transform.Children.Add(new TranslateTransform3D(origin.X, origin.Y, origin.Z));
            return transform;
        }

        private double GetNormalizationFactor(LinkageType linkageType, GeometryModel3D geometryModel)
        {
            if (!_templateMaxDimensionCache.TryGetValue(linkageType, out double maxDim))
            {
                maxDim = 0.0;

                if (geometryModel.Geometry is MeshGeometry3D mesh)
                {
                    Rect3D bounds = mesh.Bounds;
                    if (!bounds.IsEmpty)
                        maxDim = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
                }

                _templateMaxDimensionCache[linkageType] = maxDim;
            }

            if (maxDim < 0.0001)
            {
                Debug.WriteLine(
                    $"[V3DViewModel] Cube englobant introuvable pour '{linkageType}' — normalisation par défaut (1.0) appliquée.");
                return 1.0;
            }

            return 1.0 / maxDim;
        }

        private void UpdateAllArrowScales(double newSize)
        {
            foreach (var info in _arrowScaleInfo.Values)
            {
                double s = newSize * info.NormalizationFactor;
                info.Transform.ScaleX = s;
                info.Transform.ScaleY = s;
                info.Transform.ScaleZ = s;
            }
        }

        /// <summary>
        /// Remet à jour l'opacité de tous les symboles déjà affichés, en
        /// mutant directement leur matériau (via LinkagesVisual3D.UpdateOpacity).
        /// </summary>
        private void UpdateAllArrowOpacities(double opacity)
        {
            foreach (var arrow in _arrowsByVisual.Values)
                arrow.UpdateOpacity(opacity);
        }

        #endregion

        #region Grille au sol

        /// <summary>
        /// Ajoute/retire la grille de sol. Dimensions par défaut arbitraires
        /// </summary>
        private void UpdateGridVisibility(bool show)
        {
            if (show)
            {
                if (_gridVisual == null)
                {
                    _gridVisual = new GridLinesVisual3D
                    {
                        Width = 2000,
                        Length = 2000,
                        MinorDistance = 50,
                        MajorDistance = 250,
                        Thickness = 1.0,
                        Fill = Brushes.DimGray
                    };
                }

                if (!Visuals.Contains(_gridVisual))
                    Visuals.Add(_gridVisual);
            }
            else
            {
                if (_gridVisual != null)
                    Visuals.Remove(_gridVisual);
            }
        }

        #endregion

        #region Sélection / édition

        private void Arrow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ModelUIElement3D visual && _arrowsByVisual.TryGetValue(visual, out var arrow))
                StatusMessage = $"Flèche sélectionnée : ModelData Id={arrow.LinkedOriginalId} (pièce {arrow.ExtremitePartId})";
        }

        public async Task ApplyArrowEditAsync<T>(Views.LinkagesVisual3D arrow, Action<T> applyChanges)
            where T : class
        {
            if (arrow == null || applyChanges == null)
                return;

            if (arrow.LinkedLinkage is not T typedData)
            {
                throw new InvalidCastException(
                    $"La flèche Id={arrow.LinkedOriginalId} porte une donnée de type " +
                    $"'{arrow.LinkedLinkage?.GetType().Name ?? "null"}', pas '{typeof(T).Name}'.");
            }

            applyChanges(typedData);
            arrow.RefreshTooltip();

            await NotifyArrowChangedAsync(arrow, CrudOperation.Updated);
        }

        private async Task NotifyArrowChangedAsync(Views.LinkagesVisual3D arrow, CrudOperation operation)
        {
            if (arrow == null)
                return;

            await EventsManager.RaiseArrowChangedAsync(new ArrowChangedEvent
            {
                Operation = operation,
                LinkedOriginalId = arrow.LinkedOriginalId,
                NewValue = (ModelData)arrow.LinkedLinkage,
                Source = EventSource.Arrow
            });
        }

        #endregion

        #region Nettoyage


        /// <summary>
        /// Supprime toutes les flèches de liaison de la scène.
        /// </summary>
        private void ClearAllModelTolerancing()
        {
            // Supprime tous les visuels des flèches de la scène.
            foreach (var arrows in _partArrows.Values)
            {
                foreach (var arrow in arrows)
                {
                    if (arrow.Visual != null)
                        Visuals.Remove(arrow.Visual);
                }
            }

            // Nettoyage des registres associés aux flèches.
            _partArrows.Clear();
            _arrowsByVisual.Clear();
            _arrowScaleInfo.Clear();
        }

        /// <summary>
        /// Supprime le modèle CAO importé.
        /// </summary>
        private void ClearAllCadObject()
        {
            foreach (var cadObject in _cadScene.Objects)
                foreach (var visual in cadObject.Visuals)
                    Visuals.Remove(visual);

            _cadScene.Clear();
        }

        /// <summary>
        /// Supprime le modèle CAO : retire ses visuels de la scène et des tables
        /// de correspondance, et désélectionne l'objet s'il était sélectionné.
        /// </summary>
        private void ClearSelectCadObject(CadObject cadObject)
        {
            if (cadObject == null)
                return;

            RemoveCadObjectOnScene(cadObject);

            if (ReferenceEquals(SelectedCadObject, cadObject))
                SelectedCadObject = null;
        }

        /// <summary>
        /// Nettoie l'ensemble de la scène graphique.
        /// </summary>
        private void ClearAllScene()
        {
            // Conservation des lumières existantes.
            var lights = Visuals
                .Where(v => v is DefaultLights)
                .ToList();

            // Suppression des éléments dynamiques.
            ClearAllModelTolerancing();
            ClearAllCadObject();
        }

        #endregion
    }

    public class PartColorEntry : BaseViewModel
    {
        public int PartId { get; }

        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                if (SetProperty(ref _color, value))
                    OnPropertyChanged(nameof(Brush));
            }
        }

        public SolidColorBrush Brush => new SolidColorBrush(Color);

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public PartColorEntry(int partId, Color color)
        {
            PartId = partId;
            _color = color;
        }
    }
}
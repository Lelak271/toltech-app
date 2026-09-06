using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Toltech.App.Utilities.Object3D;
using Toltech.App.Views;
using TtCore = Toltech.App.ViewModels;
using System.Windows.Media.Animation;

namespace Toltech.App.ViewModels
{
    /// <summary>
    /// Partie CAO minimale du V3DViewModel.
    /// Les fonctions de sélection et de géométrie restent séparées du moteur de tolérancement.
    /// </summary>
    public partial class V3DViewModel
    {
        private readonly CadScene _cadScene = new CadScene();
        private readonly CadPicker _cadPicker;
        public  CadPicker CadPicker => _cadPicker;

        private CadToolMode _cadToolMode = CadToolMode.Select;
        //private CadObject _selectedCadObject;
        private CadHit _lastCadHit;

        #region Properties Bindable

        private Point3D? _point1;
        private Point3D? _point2;
        private Point3D? _point3;

        private bool _isSelectModeActive;
        private bool _isPickPointModeActive;
        private bool _isPickVertexModeActive;

        public bool IsSelectModeActive
        {
            get => _isSelectModeActive;
            set => SetProperty(ref _isSelectModeActive, value);
        }
        public bool IsPickPointModeActive
        {
            get => _isPickPointModeActive;
            set => SetProperty(ref _isPickPointModeActive, value);
        }
        public bool IsPickVertexModeActive
        {
            get => _isPickVertexModeActive;
            set => SetProperty(ref _isPickVertexModeActive, value);
        }

        public ObservableCollection<CadObject> CadObjects => _cadScene.Objects;

        public CadObject SelectedCadObject
        {
            get => _cadScene.SelectedObject;
            private set
            {
                if (ReferenceEquals(_cadScene.SelectedObject, value))
                    return;

                _cadScene.SelectObject(value);

                StatusMessage = value != null
                    ? $"Objet sélectionné : {value.Name}"
                    : "Aucun objet sélectionné";

                OnPropertyChanged(nameof(SelectedCadObject));
                OnPropertyChanged(nameof(SelectedCadObjectColor));
            }
        }

        public Color SelectedCadObjectColor =>
            SelectedCadObject?.Color ?? Colors.LightGray;

        public CadHit LastCadHit
        {
            get => _lastCadHit;
            private set
            {
                if (ReferenceEquals(_lastCadHit, value))
                    return;

                _lastCadHit = value;
                OnPropertyChanged(nameof(LastCadHit));
                OnPropertyChanged(nameof(LastPointText));
                OnPropertyChanged(nameof(LastNormalText));
                OnPropertyChanged(nameof(LastObjectName));
            }
        }

        public string LastObjectName => LastCadHit?.Object?.Name ?? "Aucune sélection";

        private Point3D _currentMousePosition;

        public Point3D CurrentMousePosition
        {
            get => _currentMousePosition;

            private set
            {
                if (_currentMousePosition == value)
                    return;

                _currentMousePosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentMousePositionText));
            }
        }
        public string CurrentMousePositionText =>
                        $"X: {CurrentMousePosition.X:F2}  " +
                        $"Y: {CurrentMousePosition.Y:F2}  " +
                        $"Z: {CurrentMousePosition.Z:F2}";

        private Vector3D _currentMouseNormale;
        public Vector3D CurrentMouseNormale
        {
            get => _currentMouseNormale;

            private set
            {
                if (_currentMouseNormale == value)
                    return;

                _currentMouseNormale = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentMouseNormaleText));
            }
        }
        public string CurrentMouseNormaleText =>
                        $"U: {_currentMouseNormale.X:F2}  " +
                        $"V: {_currentMouseNormale.Y:F2}  " +
                        $"W: {_currentMouseNormale.Z:F2}";

        public string CadToolInstruction => CadToolMode switch
        {
            CadToolMode.Select => "Sélectionnez un objet dans la scène.",
            CadToolMode.PickPoint => "Cliquez sur un point de la surface.",
            CadToolMode.PickVertex => "Cliquez à proximité d'un sommet.",
            CadToolMode.MeasureDistance => _point1.HasValue
                ? "Cliquez sur le deuxième point."
                : "Cliquez sur le premier point.",
            CadToolMode.PlaneNormal => PlaneStage switch
            {
                1 => "Cliquez sur le premier point du plan.",
                2 => "Cliquez sur le deuxième point du plan.",
                _ => "Cliquez sur le troisième point du plan."
            },
            CadToolMode.Transform => "Modifiez les valeurs de transformation dans le panneau.",
            _ => string.Empty
        };

        public string LastPointText => LastCadHit == null
            ? "—"
            : FormatPoint(LastCadHit.EffectivePoint);

        public string LastNormalText => LastCadHit == null
            ? "—"
            : FormatVector(LastCadHit.Normal);

        public string Point1Text => _point1.HasValue ? FormatPoint(_point1.Value) : "—";
        public string Point2Text => _point2.HasValue ? FormatPoint(_point2.Value) : "—";
        public string Point3Text => _point3.HasValue ? FormatPoint(_point3.Value) : "—";

        public string MeasurementVectorText =>
            _point1.HasValue && _point2.HasValue
                ? FormatVector(GeometryCalculator.CreateVector(_point1.Value, _point2.Value))
                : "—";

        public string MeasurementLenghtText =>
            _point1.HasValue && _point2.HasValue
                ? GeometryCalculator.Distance(_point1.Value, _point2.Value).ToString("0.###")
                : "—";

        public Vector3D? PlaneNormal
        {
            get
            {
                if (!_point1.HasValue ||
                    !_point2.HasValue ||
                    !_point3.HasValue)
                {
                    return null;
                }

                return GeometryCalculator.TryCalculatePlaneNormal(
                    _point1.Value,
                    _point2.Value,
                    _point3.Value,
                    out Vector3D normal)
                    ? normal
                    : null;
            }
        }

        public string PlaneNormalText => PlaneNormal.HasValue
                                                                ? FormatVector(PlaneNormal.Value)
                                                                : "—";
        public int PlaneStage
        {
            get
            {
                if (!_point1.HasValue)
                    return 1;
                if (!_point2.HasValue)
                    return 2;
                return 3;
            }
        }

        #endregion

        #region ICommand
        public ICommand CadSelectCommand { get; set; }
        public ICommand CadPickPointCommand { get; set; }
        public ICommand CadPickVertexCommand { get; set; }
        public ICommand CadMeasureCommand { get; set; }
        public ICommand CadPlaneNormalCommand { get; set; }
        public ICommand CadCancelCommand { get; set; }
        public ICommand ChangeSelectedCadColorCommand { get; set; }
        public ICommand ResetSelectedTransformCommand { get; set; }
        public ICommand MoveSelectedVertexCommand { get; set; }
        public ICommand OpenCadToolWindowCommand { get; set; }
        public ICommand OpenCadTransformWindowCommand { get; set; }
        public ICommand CopyPointCommand { get; set; }  
        public ICommand CopyNormaleCommand { get; set; }
        public ICommand CopyVectorCommand { get; set; }
        public ICommand CopyLenghtCommand { get; set; }
        public ICommand CadObjectVisibilityCommand { get; set; }
        public ICommand CopyPointComponentCommand { get; }
        public ICommand CopyNormalComponentCommand { get; }

        #endregion

        private void InitializeCadCommands()
        {
            CadSelectCommand = new TtCore.RelayCommand(() => ActivateTool(CadToolMode.Select));
            CadPickPointCommand = new TtCore.RelayCommand(() => ActivateTool(CadToolMode.PickPoint));
            CadPickVertexCommand = new TtCore.RelayCommand(() => ActivateTool(CadToolMode.PickVertex));
            CadMeasureCommand = new TtCore.RelayCommand(() => ActivateTool(CadToolMode.MeasureDistance));
            CadPlaneNormalCommand = new TtCore.RelayCommand(() => ActivateTool(CadToolMode.PlaneNormal));
            CadCancelCommand = new TtCore.RelayCommand(CancelCadTool);
            ChangeSelectedCadColorCommand = new TtCore.RelayCommand<CadObject>(cadObject => ChangeSelectedCadColor(cadObject));
            ResetSelectedTransformCommand = new TtCore.RelayCommand(ResetSelectedTransform);
            OpenCadToolWindowCommand = new TtCore.RelayCommand(OpenCadToolWindow);
            OpenCadTransformWindowCommand = new TtCore.RelayCommand(OpenCadTransformWindow);
            CopyPointCommand = new TtCore.RelayCommand<int>(index => CopyPoint(index));
            CopyNormaleCommand = new TtCore.RelayCommand(CopyNormal);
            CopyVectorCommand = new TtCore.RelayCommand(CopyVector);
            CopyLenghtCommand = new TtCore.RelayCommand(CopyLenght);
            CadObjectVisibilityCommand = new TtCore.RelayCommand<CadObject>(cadObject => SetSelectedCadVisibility(cadObject));

            ZoomSelectedCommand = new TtCore.RelayCommand(ZoomSelected);
            ZoomAllCommand = new TtCore.RelayCommand(ZoomAll);
            ToggleCinematicOrbitCommand = new TtCore.RelayCommand(ToggleCinematicOrbit, CanToggleCinematicOrbit);

        }

        private void RaiseGeometryPropertiesChanged()
        {
            OnPropertyChanged(nameof(Point1Text));
            OnPropertyChanged(nameof(Point2Text));
            OnPropertyChanged(nameof(Point3Text));
            OnPropertyChanged(nameof(MeasurementVectorText));
            OnPropertyChanged(nameof(MeasurementLenghtText));
            OnPropertyChanged(nameof(PlaneNormalText));
            OnPropertyChanged(nameof(CadToolInstruction));
        }


        /// <summary>
        /// Met à jour la position courante de la souris dans le viewport.
        /// </summary>
        public void UpdateMousePosition(CadHit position)
        {
            CurrentMousePosition = position.Point;
            CurrentMouseNormale = position.Normal;

        }

        public CadToolMode CadToolMode
        {
            get => _cadToolMode;
            private set
            {
                if (SetProperty(ref _cadToolMode, value))
                    OnPropertyChanged(nameof(CadToolInstruction));
            }
        }

        /// <summary>
        /// Point d'entrée appelé par la vue après un clic intercepté dans le viewport.
        /// </summary>
        public bool TryPickCad(HelixViewport3D viewport, System.Windows.Point screenPoint, out CadHit hit)
        {
            return _cadPicker.TryGetCadHit(viewport, screenPoint, out hit);
        }

        public void HandleCadHit(CadHit hit)
        {
            if (hit == null || !hit.IsOn3DObject)
                return;


            LastCadHit = hit;
            SelectCadObject(hit.Object);

            switch (CadToolMode)
            {
                case CadToolMode.Select:
                    StatusMessage = $"Objet sélectionné : {hit.Object.Name}";
                    break;

                case CadToolMode.PickPoint:
                    SetSinglePoint(hit.EffectivePoint, "Point capturé");
                    break;

                case CadToolMode.PickVertex:
                    if (!hit.SnappedVertex.HasValue)
                    {
                        StatusMessage = "Aucun sommet trouvé dans la tolérance définie.";
                        return;
                    }

                    SetSinglePoint(hit.SnappedVertex.Value, "Sommet capturé");
                    break;

                case CadToolMode.MeasureDistance:
                    HandleMeasurementPoint(hit.EffectivePoint);
                    break;

                case CadToolMode.PlaneNormal:
                    HandlePlanePoint(hit.EffectivePoint);
                    break;
            }
        }

        public void SelectCadObject(CadObject obj)
        {
            SelectedCadObject = obj;
        }

        private void SetSinglePoint(Point3D point, string message)
        {
            _point1 = point;
            _point2 = null;
            _point3 = null;

            RaiseGeometryPropertiesChanged();
            StatusMessage = $"{message} : {FormatPoint(point)}";
            CadToolMode = CadToolMode.Select;
        }

        private void HandleMeasurementPoint(Point3D point)
        {
            if (!_point1.HasValue)
            {
                _point1 = point;
                _point2 = null;
                _point3 = null;
                RaiseGeometryPropertiesChanged();
                StatusMessage = "Premier point capturé. Cliquez sur le deuxième point.";
                OnPropertyChanged(nameof(CadToolInstruction));
                return;
            }

            _point2 = point;
            RaiseGeometryPropertiesChanged();

            var vector = GeometryCalculator.CreateVector(_point1.Value, _point2.Value);
            var distance = vector.Length;

            StatusMessage =
                $"Distance = {distance:0.###} ; Δ = {FormatVector(vector)}";

            CadToolMode = CadToolMode.Select;
        }

        private void HandlePlanePoint(Point3D point)
        {
            if (!_point1.HasValue)
            {
                _point1 = point;
            }
            else if (!_point2.HasValue)
            {
                _point2 = point;
            }
            else
            {
                _point3 = point;

                var normal = PlaneNormal;

                if (normal.HasValue)
                {
                    StatusMessage = $"Normale du plan : {FormatVector(normal.Value)}";
                    CadToolMode = CadToolMode.Select;
                }
                else
                {
                    StatusMessage = "Les trois points sont colinéaires : normale impossible à calculer.";
                    _point3 = null;
                }
            }

            RaiseGeometryPropertiesChanged();
            OnPropertyChanged(nameof(CadToolInstruction));
        }

        private void ActivateTool(CadToolMode newMode)
        {
            _point1 = null;
            _point2 = null;
            _point3 = null;
            RaiseGeometryPropertiesChanged();
            CadToolMode = newMode;

            // Active uniquement l'état correspondant au mode courant.
            IsSelectModeActive = newMode == CadToolMode.Select;
            IsPickPointModeActive = newMode == CadToolMode.PickPoint;
            IsPickVertexModeActive = newMode == CadToolMode.PickVertex;
            IsPickVertexModeActive = newMode == CadToolMode.None;

            StatusMessage = CadToolInstruction;
        }

        private void CancelCadTool()
        {
            _point1 = null;
            _point2 = null;
            _point3 = null;
            RaiseGeometryPropertiesChanged();
            CadToolMode = CadToolMode.Select;
            StatusMessage = "Outil CAO annulé.";
        }

        private static string FormatPoint(Point3D p)
            => $"({p.X:0.##} ; {p.Y:0.##} ; {p.Z:0.##})";

        private static string FormatVector(Vector3D v)
            => $"({v.X:0.##} ; {v.Y:0.##} ; {v.Z:0.##})";


        private void CopyPoint(int pointIndex)
        {
            Point3D? point = pointIndex switch
            {
                1 => _point1,
                2 => _point2,
                3 => _point3,
                _ => null
            };

            // Aucun point n'est actuellement défini.
            if (!point.HasValue)
                return;

            Point3D value = point.Value;

            string text =
                $"{value.X:F3}\t" +
                $"{value.Y:F3}\t" +
                $"{value.Z:F3}";

            Clipboard.SetText(text);
        }
        private void CopyNormal()
        {
            Clipboard.SetText(PlaneNormalText);
        }
        private void CopyVector()
        {
            Clipboard.SetText(MeasurementVectorText);
        }
        private void CopyLenght()
        {
            Clipboard.SetText(MeasurementLenghtText);
        }

        #region Transformation Function

        private void ResetSelectedTransform()
        {
            SelectedCadObject?.Transform.Reset();
        }

        public void SetSelectedCadVisibility(CadObject cadObject)
        {
            _cadScene.SetVisibility(cadObject, !cadObject.IsVisible);
        }

        private void ChangeSelectedCadColor(CadObject cadObject)
        {
            if (cadObject == null)
                return;

            var picked = RequestColorPicker(cadObject.Color);
            if (!picked.HasValue)
                return;

            _cadScene.SetColor(cadObject, picked.Value);
            //OnPropertyChanged(nameof(SelectedCadObjectColor));
        }

        #endregion

        #region Windows Tool & Transformation

        private CadToolWindow? _cadToolWindow;

        public void OpenCadToolWindow()
        {
            if (_cadToolWindow != null)
            {
                _cadToolWindow.Activate();
                return;
            }

            _cadToolWindow = new CadToolWindow
            {
                DataContext = this
            };

            _cadToolWindow.Closed += (_, _) =>
            {
                _cadToolWindow = null;
            };

            _cadToolWindow.Show();
        }

        private CadTransformWindow? _cadTransformWindow;
        public void OpenCadTransformWindow()
        {
            if (_cadTransformWindow != null)
            {
                _cadTransformWindow.Activate();
                return;
            }

            _cadTransformWindow = new CadTransformWindow
            {
                DataContext = this
            };

            _cadTransformWindow.Closed += (_, _) =>
            {
                _cadTransformWindow = null;
            };

            _cadTransformWindow.Show();
        }

        #endregion

        #region Commandes caméra / rendu cinéma

        public ICommand ZoomSelectedCommand { get; set; }
        public ICommand ZoomAllCommand { get; set; }
        public ICommand ToggleCinematicOrbitCommand { get; set; }

        // À placer dans le constructeur, avec les autres commandes :


        private void ZoomSelected()
        {
            if (SelectedCadObject != null)
                ZoomCadObject(SelectedCadObject);
        }

        private bool CanZoomSelected() => SelectedCadObject != null;

        private void ToggleCinematicOrbit()
        {
            if (_isOrbiting)
            {
                StopCinematicOrbit();
            }
            else if (_lastCadHit != null)
            {
                StartCinematicOrbit(_lastCadHit.Point);
            }
        }

        private bool CanToggleCinematicOrbit() => _isOrbiting || _lastCadHit != null;

        #endregion

        #region Caméra / zoom / orbite cinématique

        private PerspectiveCamera _camera = new PerspectiveCamera
        {
            //Position = new Point3D(1000, 1000, 1000),
            //LookDirection = new Vector3D(-1000, -1000, -1000),
            //UpDirection = new Vector3D(0, 0, 1),
            //FieldOfView = 45
        };

        public PerspectiveCamera Camera
        {
            get => _camera;
            set => SetProperty(ref _camera, value);
        }

        /// <summary>
        /// Calcule la boîte englobante (en coordonnées monde) d'un ensemble de visuels,
        /// en combinant Model3D.Bounds (coordonnées locales) et Visual3D.Transform.
        /// </summary>
        private static Rect3D ComputeWorldBounds(IEnumerable<Visual3D> visuals)
        {
            Rect3D result = Rect3D.Empty;

            foreach (var visual in visuals)
            {
                if (visual is not ModelVisual3D modelVisual || modelVisual.Content == null)
                    continue;

                var localBounds = modelVisual.Content.Bounds;
                if (localBounds.IsEmpty)
                    continue;

                var transform = visual.Transform ?? Transform3D.Identity;
                var worldBounds = transform.TransformBounds(localBounds);

                result = result.IsEmpty ? worldBounds : Rect3D.Union(result, worldBounds);
            }

            return result;
        }

        /// <summary>
        /// Anime la position de la caméra vers newPosition, en gardant la direction
        /// de visée actuelle (recentrage sans changer l'angle de vue).
        /// </summary>
        private void AnimateCameraPosition(Point3D newPosition, double animationSeconds, Action onCompleted = null)
        {
            var animation = new Point3DAnimation
            {
                To = newPosition,
                Duration = TimeSpan.FromSeconds(animationSeconds),
                AccelerationRatio = 0.3,
                DecelerationRatio = 0.3,
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                Camera.BeginAnimation(ProjectionCamera.PositionProperty, null);
                Camera.Position = newPosition;
                onCompleted?.Invoke();
            };

            Camera.BeginAnimation(ProjectionCamera.PositionProperty, animation);
        }

        /// <summary>
        /// Déclenché quand le VM souhaite fixer le point de pivot d'orbite de la caméra
        /// (typiquement après un zoom). La vue s'y abonne pour piloter CameraController,
        /// que le VM ne connaît pas directement.
        /// </summary>
        public event Action<Point3D> RequestSetCameraPivot;
        private void ZoomToBounds(Rect3D bounds, double animationSeconds = 0.4)
        {
            if (bounds.IsEmpty)
                return;

            var center = new Point3D(
                bounds.X + bounds.SizeX / 2,
                bounds.Y + bounds.SizeY / 2,
                bounds.Z + bounds.SizeZ / 2);

            double radius = new Vector3D(bounds.SizeX, bounds.SizeY, bounds.SizeZ).Length / 2;
            if (radius <= 0)
                radius = 1;

            double fovRadians = Camera.FieldOfView * Math.PI / 180.0;
            double distance = radius / Math.Sin(fovRadians / 2) * 1.2; // marge de 20 %

            var lookDirection = Camera.LookDirection;
            lookDirection.Normalize();

            var newPosition = center - lookDirection * distance;
            AnimateCameraPosition(newPosition, animationSeconds, onCompleted: () =>
            {
                // Une fois la caméra en place, demande à la vue de fixer le pivot
                // d'orbite sur le centre visé (la vue affinera si besoin via un
                // vrai hit-test de surface).
                RequestSetCameraPivot?.Invoke(center);
            });
        }

        /// <summary>
        /// Recentre et zoome la caméra sur un objet CAO précis.
        /// </summary>
        public void ZoomCadObject(CadObject cadObject, double animationSeconds = 0.4)
        {
            if (cadObject == null)
                return;

            var bounds = ComputeWorldBounds(cadObject.Visuals);
            ZoomToBounds(bounds, animationSeconds);
        }

        /// <summary>
        /// Recentre et zoome la caméra sur l'ensemble de la scène.
        /// </summary>
        public void ZoomAll()
        {
            double animationSeconds = 0.4;
            var allVisuals = _cadScene.Objects.SelectMany(o => o.Visuals);
            var bounds = ComputeWorldBounds(allVisuals);
            ZoomToBounds(bounds, animationSeconds);
        }

        // ---- Orbite cinématique autour de l'axe Z ----

        private bool _isOrbiting;
        private DateTime _orbitStartTime;
        private Point3D _orbitPivot;
        private double _orbitRadius;
        private double _orbitHeight;
        private double _orbitDurationSeconds;

        /// <summary>
        /// Démarre une rotation continue de la caméra autour de l'axe Z, centrée
        /// sur le point pivot donné (typiquement le dernier point sélectionné,
        /// ou le centre d'un objet). Effet "plateau tournant".
        /// </summary>
        public void StartCinematicOrbit(Point3D pivot, double durationSeconds = 8.0)
        {
            StopCinematicOrbit();

            var offset = Camera.Position - pivot;
            _orbitPivot = pivot;
            _orbitRadius = new Vector(offset.X, offset.Y).Length;
            _orbitHeight = offset.Z;
            _orbitDurationSeconds = durationSeconds;
            _orbitStartTime = DateTime.Now;
            _isOrbiting = true;

            CompositionTarget.Rendering += OnOrbitRendering;
        }

        public void StopCinematicOrbit()
        {
            if (!_isOrbiting)
                return;

            CompositionTarget.Rendering -= OnOrbitRendering;
            _isOrbiting = false;
        }

        private void OnOrbitRendering(object sender, EventArgs e)
        {
            double elapsed = (DateTime.Now - _orbitStartTime).TotalSeconds;
            double angle = elapsed / _orbitDurationSeconds * 2 * Math.PI;

            var newPosition = new Point3D(
                _orbitPivot.X + _orbitRadius * Math.Cos(angle),
                _orbitPivot.Y + _orbitRadius * Math.Sin(angle),
                _orbitPivot.Z + _orbitHeight);

            Camera.Position = newPosition;
            Camera.LookDirection = _orbitPivot - newPosition;
        }

        #endregion

    }
}

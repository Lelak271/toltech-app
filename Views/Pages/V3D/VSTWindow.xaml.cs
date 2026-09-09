using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using Toltech.App.ViewModels;
using Toltech.App.Utilities.Object3D;
using HelixToolkit.Wpf;

namespace Toltech.App.Views
{
    public partial class VSTWindow : UserControl
    {
        private V3DViewModel ViewModel => DataContext as V3DViewModel;

        private readonly HashSet<Visual3D> _trackedVisuals = new HashSet<Visual3D>();

        public VSTWindow()
        {
            InitializeComponent();

            DataContextChanged += VSTWindow_DataContextChanged;
            Loaded += VSTWindow_Loaded;
            Unloaded += VSTWindow_Unloaded;
        }

        private void VSTWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AttachVisuals();

            if (DataContext is V3DViewModel vm)
            {
                view3D.Camera = vm.Camera;
                vm.RequestSetCameraPivot += OnRequestSetCameraPivot;
            }
        }

        private void OnRequestSetCameraPivot(Point3D worldPoint)
        {
            var camera = view3D.Camera as ProjectionCamera;
            if (camera == null)
                return;

            var screenPoint = view3D.Viewport.Point3DtoPoint2D(worldPoint);

            Point3D pivot = worldPoint;
            if (ViewModel.CadPicker.TryGetCadHit(view3D, screenPoint, out var hit))
                pivot = hit.Vertex?.WorldPosition ?? hit.Point;

            view3D.CameraController.FixedRotationPointEnabled = true;
            view3D.CameraController.FixedRotationPoint = pivot;
        }
        private void View3D_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ReleaseCameraPivot();
        }
        /// <summary>
        /// Relâche le pivot fixé par un zoom, pour rendre la main au comportement
        /// natif d'HelixToolkit (recalcul automatique du pivot au double-clic droit).
        /// </summary>
        private void ReleaseCameraPivot()
        {
            view3D.CameraController.FixedRotationPointEnabled = false;
        }
        private void VSTWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachVisuals();
        }

        private void VSTWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is V3DViewModel oldVm)
            {
                oldVm.Visuals.CollectionChanged -= Visuals_CollectionChanged;
                RemoveTrackedVisuals();
            }

            if (ViewModel == null)
                return;

            ViewModel.RequestObjFilePath = RequestObjFilePath;
            ViewModel.RequestColorPicker = RequestColorPicker;
            ViewModel.ShowError = (message, title) =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

            if (IsLoaded)
                AttachVisuals();
        }

        private void AttachVisuals()
        {
            if (ViewModel == null)
                return;

            foreach (var visual in ViewModel.Visuals)
                AddTrackedVisual(visual);

            ViewModel.Visuals.CollectionChanged -= Visuals_CollectionChanged;
            ViewModel.Visuals.CollectionChanged += Visuals_CollectionChanged;
        }

        private void DetachVisuals()
        {
            if (ViewModel != null)
                ViewModel.Visuals.CollectionChanged -= Visuals_CollectionChanged;

            RemoveTrackedVisuals();
        }

        private void Visuals_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                        foreach (Visual3D item in e.NewItems)
                            AddTrackedVisual(item);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (Visual3D item in e.OldItems)
                            RemoveTrackedVisual(item);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                        foreach (Visual3D item in e.OldItems)
                            RemoveTrackedVisual(item);
                    if (e.NewItems != null)
                        foreach (Visual3D item in e.NewItems)
                            AddTrackedVisual(item);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    RemoveTrackedVisuals();
                    break;
            }
        }

        private void AddTrackedVisual(Visual3D visual)
        {
            if (_trackedVisuals.Add(visual))
                view3D.Children.Add(visual);
        }

        private void RemoveTrackedVisual(Visual3D visual)
        {
            if (_trackedVisuals.Remove(visual))
                view3D.Children.Remove(visual);
        }

        private void RemoveTrackedVisuals()
        {
            foreach (var visual in _trackedVisuals)
                view3D.Children.Remove(visual);

            _trackedVisuals.Clear();
        }

        /// <summary>
        /// Intercepte le clic avant le traitement de la caméra/du visuel 3D.
        /// La recherche géométrique est effectuée uniquement lorsque le clic
        /// correspond à un objet CAO enregistré dans CadScene.
        /// </summary>
        private void View3D_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null)
                return;

            Point mousePosition = e.GetPosition(view3D);

            if (!ViewModel.TryPickCad(view3D, mousePosition, out CadHit hit))
                return;

            ViewModel.HandleCadHit(hit);

            // Empêche le viewport de démarrer une rotation/translation caméra
            // lorsque l'utilisateur est en train d'utiliser un outil CAO.
            e.Handled = true;
        }

        /// <summary>
        /// Met à jour la position courante de la souris dans le viewport 3D.
        /// </summary>
        private void View3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (ViewModel == null)
                return;

            // Position de la souris exprimée dans le système de coordonnées
            // du HelixViewport3D.
            Point currentPosition = e.GetPosition(view3D);


            if (!ViewModel.TryPickCad(view3D, currentPosition, out CadHit hit))
                return; 

            // Transmet éventuellement la position au ViewModel.
            ViewModel.UpdateMousePosition(hit);
        }



        private string RequestObjFilePath()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Fichiers OBJ (*.obj)|*.obj"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private Color? RequestColorPicker(Color initial)
        {
            using (var dialog = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                Color = System.Drawing.Color.FromArgb(initial.A, initial.R, initial.G, initial.B)
            })
            {
                var owner = new Win32WindowWrapper(Window.GetWindow(this));

                if (dialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
                    return null;

                var c = dialog.Color;
                return Color.FromArgb(c.A, c.R, c.G, c.B);
            }
        }

        private sealed class Win32WindowWrapper : System.Windows.Forms.IWin32Window
        {
            public IntPtr Handle { get; }

            public Win32WindowWrapper(Window window)
            {
                Handle = new WindowInteropHelper(window).Handle;
            }
        }

        private void BackgroundStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackgroundStyleComboBox.SelectedItem is ComboBoxItem item && item.Tag is string key)
                view3D.Background = GetBackgroundBrush(key);
        }

        private static Brush GetBackgroundBrush(string key)
        {
            switch (key)
            {
                case "catia":
                    return new LinearGradientBrush(
                        new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0xAA, 0xB4, 0xD1), 0),
                            new GradientStop(Color.FromRgb(0x6E, 0x7C, 0xA8), 0.5),
                            new GradientStop(Color.FromRgb(0x33, 0x33, 0x66), 1)
                        },
                        new Point(0, 0), new Point(0, 1));

                case "dark":
                    return new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

                case "white":
                    return Brushes.White;

                case "gray":
                    return new LinearGradientBrush(
                        Colors.White,
                        Color.FromRgb(0xB0, 0xB0, 0xB0),
                        90);

                default:
                    return Brushes.White;
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Registre des objets CAO présents dans la scène 3D.
    /// </summary>
    public sealed class CadScene
    {
        private readonly Dictionary<Visual3D, CadObject> _visualToCadObject = new();

        public ObservableCollection<CadObject> Objects { get; } = new();

        public CadObject AddObject(string filePath, Model3DGroup model)
        {
            var obj = new CadObject(filePath, model);
            Objects.Add(obj);
            obj.Transform.PropertyChanged += Transform_PropertyChanged;
            return obj;
        }

        public void RemoveObject(CadObject obj)
        {
            if (obj == null)
                return;

            obj.Transform.PropertyChanged -= Transform_PropertyChanged;

            foreach (var visual in obj.Visuals)
                _visualToCadObject.Remove(visual);

            Objects.Remove(obj);
        }

        
        
        public void Clear()
        {
            foreach (var obj in Objects)
                obj.Transform.PropertyChanged -= Transform_PropertyChanged;

            Objects.Clear();
            _visualToCadObject.Clear();
        }

        public void SetVisuals(CadObject obj, IEnumerable<Visual3D> visuals)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            // Supprime les anciennes associations Visual3D -> CadObject.
            foreach (var oldVisual in obj.Visuals)
            {
                _visualToCadObject.Remove(oldVisual);
            }

            // Vide la collection des anciens visuels.
            obj.Visuals.Clear();

            // Ajoute les nouveaux visuels.
            foreach (var visual in visuals.Where(v => v != null).Distinct())
            {
                obj.Visuals.Add(visual);

                // Permet de retrouver le CadObject à partir d'un Visual3D lors du picking.
                _visualToCadObject[visual] = obj;

                // Applique la transformation du CadObject au Visual3D.
                visual.Transform = TransformService.BuildTransform(obj.Transform);
            }
        }

        
        
        public bool TryGetObject(DependencyObject visual, out CadObject obj)
        {
            obj = null;

            if (visual is Visual3D directVisual && _visualToCadObject.TryGetValue(directVisual, out obj))
                return true;

            // Remonte l'arbre visuel au cas où Helix/WPF renvoie un élément enfant.
            var current = visual;
            while (current != null)
            {
                if (current is Visual3D currentVisual && _visualToCadObject.TryGetValue(currentVisual, out obj))
                    return true;

                current = current is Visual v ? VisualTreeHelper.GetParent(v) : null;
            }

            return false;
        }

        private void Transform_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is not TransformState state)
                return;

            var obj = Objects.FirstOrDefault(x => ReferenceEquals(x.Transform, state));
            if (obj == null)
                return;

            var transform = TransformService.BuildTransform(state);

            foreach (var visual in obj.Visuals)
                visual.Transform = transform;
        }


        private readonly Dictionary<ModelVisual3D, Model3D?> _originalContents = new();
        public void SetVisibility(CadObject obj, bool isVisible)
        {
            if (obj == null)
                return;

            obj.IsVisible = isVisible;

            foreach (var visual in obj.Visuals)
            {
                if (visual is not ModelVisual3D modelVisual)
                    continue;

                // On mémorise le contenu original la première fois.
                if (!_originalContents.ContainsKey(modelVisual))
                {
                    _originalContents[modelVisual] = modelVisual.Content;
                }

                // Masquer
                if (!isVisible)
                {
                    modelVisual.Content = null;
                }
                // Restaurer
                else
                {
                    modelVisual.Content = _originalContents[modelVisual];
                }
            }
        }


        public void SetColor(CadObject obj, Color color)
        {
            if (obj == null)
                return;

            obj.Color = color;
            ApplyColor(obj.Model, color);

            foreach (var visual in obj.Visuals)
            {
                if (visual is LinesVisual3D lines)
                    lines.Color = color;
                else if (visual is PointsVisual3D points)
                    points.Color = color;
            }
        }

        private static void ApplyColor(Model3D model, Color color)
        {
            if (model is GeometryModel3D geometry)
            {
                geometry.Material = MaterialHelper.CreateMaterial(color);
                geometry.BackMaterial = geometry.Material;
                return;
            }

            if (model is not Model3DGroup group)
                return;

            foreach (var child in group.Children)
                ApplyColor(child, color);
        }

        public event Action<CadObject> SelectionChanged;
        public CadObject SelectedObject { get; private set; }
        public void SelectObject(CadObject obj)
        {
            if (ReferenceEquals(SelectedObject, obj))
                return;

            if (SelectedObject != null)
                SelectedObject.IsSelected = false;

            SelectedObject = obj;

            if (SelectedObject != null)
                SelectedObject.IsSelected = true;

            SelectionChanged?.Invoke(obj);
        }
    }
}

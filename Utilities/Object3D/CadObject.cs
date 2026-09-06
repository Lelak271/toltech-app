using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Objet OBJ chargé dans la scène Toltech.
    /// Cette classe contient les données de scène, pas la logique de tolérancement.
    /// </summary>
    public sealed class CadObject : INotifyPropertyChanged
    {
        private string _name;
        private Color _color = Colors.LightGray;
        private bool _isVisible = true;
        private bool _isSelected;

        public Guid Id { get; } = Guid.NewGuid();

        public string FilePath { get; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public Model3DGroup Model { get; }

        /// <summary>Visuels effectivement ajoutés au HelixViewport3D.</summary>
        public ObservableCollection<Visual3D> Visuals { get; } = new ObservableCollection<Visual3D>();

        /// <summary>Arbre interne affiché dans le TreeView.</summary>
        public ObservableCollection<CadNode> Children { get; } = new ObservableCollection<CadNode>();

        public TransformState Transform { get; } = new TransformState();

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;

                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Brush)));
            }
        }

        public Brush Brush => new SolidColorBrush(Color);

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                    return;

                _isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public CadObject(string filePath, Model3DGroup model)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _name = Path.GetFileNameWithoutExtension(filePath);
        }

 
        /// <summary>
        /// Retourne tous les maillages du modèle récursivement.
        /// </summary>
        public IEnumerable<GeometryModel3D> GetGeometryModels()
        {
            return EnumerateGeometryModels(Model);
        }

        private static IEnumerable<GeometryModel3D> EnumerateGeometryModels(Model3D model)
        {
            if (model is GeometryModel3D geometry)
            {
                yield return geometry;
                yield break;
            }

            if (model is not Model3DGroup group)
                yield break;

            foreach (var child in group.Children)
            {
                foreach (var item in EnumerateGeometryModels(child))
                    yield return item;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

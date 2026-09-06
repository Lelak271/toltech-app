using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Etat de transformation d'un objet CAO.
    /// Les angles sont exprimés en degrés.
    /// L'échelle est uniforme : ScaleX/ScaleY/ScaleZ sont dérivées de Scale.
    /// </summary>
    public sealed class TransformState : INotifyPropertyChanged
    {
        private const double MinScale = 0.000001;

        private double _translationX;
        private double _translationY;
        private double _translationZ;
        private double _rotationX;
        private double _rotationY;
        private double _rotationZ;
        private double _scale = 1.0;

        public double TranslationX { get => _translationX; set => SetField(ref _translationX, value); }
        public double TranslationY { get => _translationY; set => SetField(ref _translationY, value); }
        public double TranslationZ { get => _translationZ; set => SetField(ref _translationZ, value); }

        public double RotationX { get => _rotationX; set => SetField(ref _rotationX, value); }
        public double RotationY { get => _rotationY; set => SetField(ref _rotationY, value); }
        public double RotationZ { get => _rotationZ; set => SetField(ref _rotationZ, value); }

        /// <summary>
        /// Facteur d'échelle unique, appliqué de façon identique sur X, Y et Z.
        /// </summary>
        public double Scale
        {
            get => _scale;
            set
            {
                double clamped = value <= 0 ? MinScale : value;
                if (Equals(_scale, clamped))
                    return;

                _scale = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScaleX));
                OnPropertyChanged(nameof(ScaleY));
                OnPropertyChanged(nameof(ScaleZ));
            }
        }

        /// <summary>
        /// Dérivée de <see cref="Scale"/>. Conservée pour la construction de matrices
        /// (TransformService.BuildTransform, etc.) sans dupliquer l'état.
        /// </summary>
        public double ScaleX => _scale;
        public double ScaleY => _scale;
        public double ScaleZ => _scale;

        public void Reset()
        {
            TranslationX = 0;
            TranslationY = 0;
            TranslationZ = 0;
            RotationX = 0;
            RotationY = 0;
            RotationZ = 0;
            Scale = 1;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return;
            field = value;
            OnPropertyChanged(propertyName);
        }
    }
}
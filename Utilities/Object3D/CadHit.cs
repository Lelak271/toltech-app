using System.Windows;
using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Résultat d'un picking 3D.
    /// </summary>
    public sealed class CadHit
    {
        public CadObject Object { get; init; }

        public DependencyObject Visual { get; init; }

        public Point3D Point { get; init; }

        public Vector3D Normal { get; init; }

        public CadVertexReference Vertex { get; init; }

        public Point3D? SnappedVertex => Vertex?.WorldPosition;

        public bool IsVertexSnap => Vertex != null;
        public bool IsOn3DObject { get; init; }

        public Point3D EffectivePoint => SnappedVertex ?? Point;
    }
}

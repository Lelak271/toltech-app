using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Référence vers un sommet précis d'un MeshGeometry3D.
    /// L'index est l'index dans Positions.
    /// </summary>
    public sealed class CadVertexReference
    {
        public GeometryModel3D GeometryModel { get; init; }
        public MeshGeometry3D Mesh { get; init; }
        public int Index { get; init; }
        public Point3D WorldPosition { get; init; }
    }
}

using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    public sealed class PlaneResult
    {
        public Point3D Point1 { get; init; }
        public Point3D Point2 { get; init; }
        public Point3D Point3 { get; init; }
        public Vector3D Normal { get; init; }
    }
}

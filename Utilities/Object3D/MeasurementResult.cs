using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    public sealed class MeasurementResult
    {
        public Point3D Point1 { get; init; }
        public Point3D Point2 { get; init; }
        public Vector3D Vector => Point2 - Point1;
        public double Distance => Vector.Length;
    }
}

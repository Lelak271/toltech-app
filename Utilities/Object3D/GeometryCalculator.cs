using System;
using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Calculs géométriques de base indépendants de l'interface.
    /// </summary>
    public static class GeometryCalculator
    {
        public static Vector3D CreateVector(Point3D from, Point3D to)
        {
            return to - from;
        }

        public static double Distance(Point3D a, Point3D b)
        {
            return (b - a).Length;
        }

        public static bool TryCalculatePlaneNormal(
            Point3D p1,
            Point3D p2,
            Point3D p3,
            out Vector3D normal)
        {
            var u = p2 - p1;
            var v = p3 - p1;

            normal = Vector3D.CrossProduct(u, v);

            if (normal.LengthSquared < 1e-18)
            {
                normal = new Vector3D();
                return false;
            }

            normal.Normalize();
            return true;
        }

        public static bool TryProjectPointOnPlane(
            Point3D point,
            Point3D planeOrigin,
            Vector3D planeNormal,
            out Point3D projection)
        {
            projection = point;

            if (planeNormal.LengthSquared < 1e-18)
                return false;

            var normal = planeNormal;
            normal.Normalize();

            var delta = point - planeOrigin;
            var signedDistance = Vector3D.DotProduct(delta, normal);

            projection = point - normal * signedDistance;
            return true;
        }
    }
}

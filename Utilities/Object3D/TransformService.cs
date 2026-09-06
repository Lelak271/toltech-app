using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Construit les Transform3D appliquées aux objets CAO.
    /// Convention : mise à l'échelle, puis rotations X/Y/Z, puis translation.
    /// </summary>
    public static class TransformService
    {
        public static Transform3D BuildTransform(TransformState state)
        {
            var group = new Transform3DGroup();

            group.Children.Add(new ScaleTransform3D(
                state.ScaleX,
                state.ScaleY,
                state.ScaleZ));

            group.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), state.RotationX)));

            group.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), state.RotationY)));

            group.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 0, 1), state.RotationZ)));

            group.Children.Add(new TranslateTransform3D(
                state.TranslationX,
                state.TranslationY,
                state.TranslationZ));

            return group;
        }

        public static Point3D TransformPoint(Point3D point, Transform3D transform)
        {
            return transform == null ? point : transform.Transform(point);
        }
    }
}

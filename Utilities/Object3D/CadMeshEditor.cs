using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Opérations élémentaires d'édition de maillage.
    /// Ce service ne gère volontairement pas de topologie, de contraintes ou d'historique.
    /// </summary>
    public static class CadMeshEditor
    {
        public static Point3D GetWorldPosition(CadObject obj, CadVertexReference vertex)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));

            var objectTransform = TransformService.BuildTransform(obj.Transform);
            var geometryTransform = vertex.GeometryModel.Transform ?? Transform3D.Identity;

            var point = geometryTransform.Transform(vertex.Mesh.Positions[vertex.Index]);
            return objectTransform.Transform(point);
        }

        public static void MoveVertexToWorld(CadObject obj, CadVertexReference vertex, Point3D worldPosition)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));

            MeshGeometry3D mesh = vertex.Mesh;

            if (mesh.IsFrozen)
                throw new InvalidOperationException(
                    "Le maillage OBJ est gelé et ne peut pas être modifié directement.");

            // L'objet et le GeometryModel3D peuvent chacun appliquer une transformation.
            // Nous revenons donc de l'espace monde vers l'espace local du maillage.
            var objectMatrix = TransformService.BuildTransform(obj.Transform).Value;
            if (!objectMatrix.HasInverse)
                throw new InvalidOperationException("La transformation de l'objet n'est pas inversible.");
            objectMatrix.Invert();

            var geometryMatrix = (vertex.GeometryModel.Transform ?? Transform3D.Identity).Value;
            if (!geometryMatrix.HasInverse)
                throw new InvalidOperationException("La transformation du maillage n'est pas inversible.");
            geometryMatrix.Invert();

            Point3D afterObjectInverse = objectMatrix.Transform(worldPosition);
            Point3D localPosition = geometryMatrix.Transform(afterObjectInverse);

            mesh.Positions[vertex.Index] = localPosition;
        }
    }
}

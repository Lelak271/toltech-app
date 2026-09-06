using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Windows.Controls;

namespace Toltech.App.Utilities.Object3D
{
    /// <summary>
    /// Adaptateur entre les coordonnées écran WPF et le picking natif HelixToolkit.
    /// </summary>
    public sealed class CadPicker
    {
        private readonly CadScene _scene;
        public CadPicker(CadScene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        }

        public bool TryGetCadHit(HelixViewport3D viewport, Point screenPoint, out CadHit hit)
        {
            // Valeur par défaut : aucun objet sélectionné.
            hit = new CadHit
            {
                Object = null,
                Visual = null,
                Point = new Point3D(),
                Normal = new Vector3D(),
                IsOn3DObject = false
            };

            if (viewport == null)
                return false;

            // HelixViewport3D contient un Viewport3D WPF interne.
            Viewport3D viewport3D = viewport.Viewport;
            if (viewport3D == null)
                return false;

            if (!Viewport3DHelper.FindNearest(
                    viewport3D,
                    screenPoint,
                    out Point3D point,
                    out Vector3D normal,
                    out DependencyObject visual))
            {
                //Erreur donc pas de 3D Object sur le curseur
                return true;
            }

            if (!_scene.TryGetObject(visual, out var cadObject))
                return false;

            hit = new CadHit
            {
                Object = cadObject,
                Visual = visual,
                Point = point,
                Normal = normal,
                IsOn3DObject = true
            };

            return true;
        }

        
        
        public bool TryPickVertex(
            HelixViewport3D viewport,
            Point screenPoint,
            double tolerance,
            out CadHit hit)
        {
            if (!TryGetCadHit(viewport, screenPoint, out hit))
                return false;

            var nearest = FindNearestVertex(hit.Object, hit.Point, tolerance);
            if (nearest == null)
                return true;

            hit = new CadHit
            {
                Object = hit.Object,
                Visual = hit.Visual,
                Point = hit.Point,
                Normal = hit.Normal,
                Vertex = nearest
            };

            return true;
        }

        private static CadVertexReference FindNearestVertex(
            CadObject obj,
            Point3D worldPoint,
            double tolerance)
        {
            double toleranceSquared = tolerance * tolerance;
            double bestDistanceSquared = double.MaxValue;
            CadVertexReference best = null;

            var objectTransform = TransformService.BuildTransform(obj.Transform);

            foreach (var geometry in obj.GetGeometryModels())
            {
                if (geometry.Geometry is not MeshGeometry3D mesh)
                    continue;

                var geometryTransform = geometry.Transform ?? Transform3D.Identity;

                for (int i = 0; i < mesh.Positions.Count; i++)
                {
                    var point = geometryTransform.Transform(mesh.Positions[i]);
                    point = objectTransform.Transform(point);

                    double distanceSquared = DistanceSquared(point, worldPoint);
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        best = new CadVertexReference
                        {
                            GeometryModel = geometry,
                            Mesh = mesh,
                            Index = i,
                            WorldPosition = point
                        };
                    }
                }
            }

            return bestDistanceSquared <= toleranceSquared ? best : null;
        }

        private static double DistanceSquared(Point3D a, Point3D b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            double dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using Toltech.App.Services;

namespace Toltech.App.Visualisateur
{
    public partial class VSTWindow : UserControl
    {

        private DatabaseService _databaseServiceInstance;


        public VSTWindow()
        {
            InitializeComponent();
            //LoadObjModel();
            
            _databaseServiceInstance = new DatabaseService("TODO");
            _databaseServiceInstance.Open(ModelManager.ModelActif);

        }

        #region Chargement obj
        private void LoadObjModel()
        {
            string objPath = @"C:\Toltech\DataBase_Default\VST\FinalBaseMesh.obj";

            if (!File.Exists(objPath))
            {
                MessageBox.Show($"Fichier .obj introuvable : {objPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var reader = new ObjReader();
                Model3DGroup model = reader.Read(objPath);

                ModelVisual3D visual = new ModelVisual3D
                {
                    Content = model
                };

                view3D.Children.Add(visual);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du modèle :\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ImportObj_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers OBJ (*.obj)|*.obj"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                var reader = new ObjReader();
                var models = reader.Read(filePath); // retourne un Model3DGroup

                view3D.Children.Clear();
                view3D.Children.Add(new DefaultLights());
                view3D.Children.Add(new ModelVisual3D { Content = models });
            }
        }

        #endregion

        #region Flèches depuis la base

        /// <summary>
        /// Initialise le service de base de données avec le modèle actif
        /// </summary>
        private void InitializeDatabaseService()
        {
            if (string.IsNullOrEmpty(ModelManager.ModelActif))
            {
                Debug.WriteLine("Aucun modèle actif défini.");
                return;
            }

            // Updated to use the renamed field
            _databaseServiceInstance.Open(ModelManager.ModelActif);
            Debug.WriteLine($"Initialisation du DatabaseService avec : {ModelManager.ModelActif}");
        }

        /// <summary>
        /// Charge les données de flèches depuis la base de données et les affiche dans la scène 3D
        /// </summary>
        public async Task LoadArrowsFromDatabaseAsync()
        {
            InitializeDatabaseService();

            // Updated to use the renamed field
            if (_databaseServiceInstance == null)
                return;

            var modelDataList = await _databaseServiceInstance.GetAllModelDataAsync();

            foreach (var data in modelDataList)
            {
                Point3D origin = new Point3D(data.CoordX, data.CoordY, data.CoordZ);
                Vector3D direction = new Vector3D(data.CoordU, data.CoordV, data.CoordW);

                // Vérifie que la direction n'est pas nulle
                if (direction.Length > 0.0001)
                {
                    direction.Normalize();      // Unité
                    AddArrowToViewport(origin, direction, Colors.Blue);
                }
                else
                {
                    Debug.WriteLine($"Vecteur nul ignoré pour l'élément ID={data.Id}");
                }
            }
        }

        /// <summary>
        /// Ajoute une flèche 3D entièrement personnalisable et proportionnelle à la scène.
        /// </summary>
        private void AddArrowToViewport(Point3D origin, Vector3D direction, Color color)
        {
            double arrowLength = 100;
            if (direction.Length < 1e-6)
                return;

            direction.Normalize();

            // Proportions
            double headRatio = 0.3;         // 30 % pour la tête
            double bodyRatio = 1.0 - headRatio;

            double headLength = arrowLength * headRatio;
            double bodyLength = arrowLength * bodyRatio;

            double bodyDiameter = arrowLength * 0.1;
            double headDiameter = bodyDiameter * 3;

            // Calcul des points
            Point3D tip = origin; // pointe de la flèche
            Point3D headBase = tip - direction * headLength;       // base du cône
            Point3D bodyStart = headBase - direction * bodyLength; // début du cylindre

            var mb = new MeshBuilder(false, false);

            // Corps cylindrique
            mb.AddCylinder(bodyStart, headBase, bodyDiameter, 24);

            // Tête conique
            mb.AddCone(
                origin: headBase,
                direction: direction,
                baseRadius: headDiameter / 2,
                topRadius: 0,             // pointe fine
                height: headLength,
                baseCap: true,
                topCap: false,
                thetaDiv: 36
            );

            var geometry = new GeometryModel3D
            {
                Geometry = mb.ToMesh(),
                Material = MaterialHelper.CreateMaterial(color)
            };

            view3D.Children.Add(new ModelVisual3D { Content = geometry });
        }



        private void AddArrowToViewport2(Point3D origin, Vector3D direction, Color color)
        {
            // Longueur totale voulue
            double arrowLength = 100;

            // Proportions
            double headLengthRatio = 0.2;
            double diameterRatio = 0.03;

            // Longueurs calculées
            double headLength = arrowLength * diameterRatio*2;
            double shaftLength = arrowLength ;
            double diameter = arrowLength * diameterRatio;

            // Direction mise à l'échelle
            Vector3D unitDir = direction;
            unitDir.Normalize();
            Vector3D shaftVec = unitDir * shaftLength;

            // Points
            Point3D point2 = origin;                          // Tête
            Point3D point1 = origin - shaftVec;               // Début du corps

            // Création
            var arrow = new ArrowVisual3D
            {
                Point1 = point1,
                Point2 = point2,
                Diameter = diameter,
                HeadLength = headLength,
                Fill = new SolidColorBrush(color)
            };

            view3D.Children.Add(arrow);

        }



        private async void OnLoadArrowsClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadArrowsFromDatabaseAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des flèches : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ClearView_Click(object sender, RoutedEventArgs e)
        {
            // Supprime tout sauf les lumières
            var itemsToKeep = view3D.Children.OfType<DefaultLights>().ToList();

            view3D.Children.Clear();

            // Réajoute les lumières par défaut si besoin
            foreach (var item in itemsToKeep)
            {
                view3D.Children.Add(item);
            }
        }




        #endregion

        private void AddCustomArrow(Point3D origin, Vector3D direction, Color color)
        {
            double totalLength = 50.0;
            double headRatio = 0.2;
            double shaftLength = totalLength * (1 - headRatio);
            double headLength = totalLength * headRatio;
            double diameter = totalLength * 0.05;

            direction.Normalize();

            Vector3D shaftVec = direction * shaftLength;
            Vector3D headVec = direction * headLength;

            Point3D shaftStart = origin - shaftVec;
            Point3D headBase = origin;

            var mb = new MeshBuilder(false, false);

            // Ajoute le cylindre (corps)
            mb.AddCylinder(shaftStart, headBase, diameter, 20);

            // Ajoute le cône (tête)
            mb.AddCone(
                             headBase,            // Point de départ du cône (base)
                             direction,           // Direction de la flèche
                             headLength,          // Longueur de la tête
                             diameter * 1.5,      // Rayon de la base du cône
                             0,                   // Rayon au sommet (pointe)
                             true,                // Base cap (fermé à la base)
                             false,               // Top cap (pointe ouverte, c'est une vraie flèche)
                             24                   // Nombre de divisions circulaires
                         );

            var geometry = new GeometryModel3D
            {
                Geometry = mb.ToMesh(),
                Material = MaterialHelper.CreateMaterial(color)
            };

            var model = new ModelVisual3D { Content = geometry };
            view3D.Children.Add(model);
        }






    }
}

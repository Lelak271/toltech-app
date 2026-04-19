using System;
using System.Diagnostics;
using System.IO;
using TOLTECH_APPLICATION.FrontEnd.Controls;

namespace TOLTECH_APPLICATION.Services
{
    public static class ModelManager
    {
        private static string _modelActif;
        private static string _partActif;
        private static int _partIDActif;
        private static string _appDataPath;
        private static string _filepathResx;

        // Événements synchrones
        public static event Action<object> OnModelChanged;
        public static event Action<object> OnPartChanged;
        public static event Action<string> OnAppDataPathChanged;
        public static event Action<string> FilePathResxChanged;

        // Nouvelle propriété pour accéder à l'instance active du service DB
        public static DatabaseService DatabaseServiceActif { get; set; }

        #region Constructeur

        // Permet d'avoir un MODEL ACTIF DE LA DB
        public static string ModelActif
        {
            get => _modelActif;
            set
            {
                if (_modelActif != value)
                {
                    _modelActif = value;
                    Debug.WriteLine($"ModelActif Changed : Path - {_modelActif}");

                    // On notifie toujours, même si null
                    OnModelChanged?.Invoke(_modelActif);
                }
            }
        }
       
        
        // Permet d'avoir un chemin AppData
        public static string AppDataPath
        {
            get => _appDataPath;
            set
            {
                Debug.WriteLine("[ModelManager] - AppDataPath Mettre en variable App");
                if (_appDataPath != value)
                {
                    _appDataPath = value;
                    OnAppDataPathChanged?.Invoke(_appDataPath);
                }
            }
        }

        // Chemin utiliser pour l'affichage des résultats et de l'export 
        public static string FilePathResx
        {
            get => _filepathResx;
            set
            {
                if (_filepathResx != value)
                {
                    _filepathResx = value;
                    FilePathResxChanged?.Invoke(_filepathResx);
                }
            }
        }

        #endregion
        

        #region UI

        #endregion

        #region Gestions des dossiers

        // Création du fichier Temp ou obtention du chemin
        public static string GetTolTechTempPath()
        {
            // Récupération du répertoire temporaire de l’utilisateur
            string tempPath = Path.GetTempPath();

            // Combinaison avec le nom du sous-dossier de l’application
            string usertempPath = Path.Combine(tempPath, "TolTech_Temp");

            // Création du répertoire s’il n’existe pas
            if (!Directory.Exists(usertempPath))
            {
                Directory.CreateDirectory(usertempPath);
            }

            return usertempPath;
        }


        // Retourne le chemin TolTech\Results dans Documents, crée le dossier si nécessaire
        public static string GetResultsPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TolTech",
                "Results");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        // Retourne le chemin TolTech\Results dans Documents, crée le dossier si nécessaire
        public static string GetModelMetaPath()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TolTech",
                "ModelMeta");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        // Permet d'avoir un chemin AppData par defaut 
        public static string AppDataPathDefault()
        {
            string path = "C:\\Toltech\\DataBase_Default\\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }


        #endregion



    }
}
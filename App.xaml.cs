using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Toltech.App.Properties;
using Toltech.App.Resources;
using Toltech.App.Services.Dialog;
using Toltech.App.Services.Logging;
using Toltech.App.Services.Notification;
using Toltech.App.ViewModels;
using Toltech.FreeCAD;
using Toltech.Cad.Abstractions;
using Toltech.Cad.Model;
using Toltech.Solver;
using Toltech.Solver.Contracts;
using Toltech.App.Services.CAD;

namespace Toltech.App
{
    /// Interaction logic for App.xaml
    public partial class App : Application
    {
        public static ILoggerService Logger { get; set; }
        public static INotificationService NotificationService { get; set; }
        public static IDialogService DialogService { get; set; }

        public static UiSettingsService UiSettings { get; private set; }

        public static MainViewModel MainVM { get; set; }

        #region Test cad  TODO

        private ICadSelectionService? _cadSelectionService;
        public ICadSelectionService CadSelectionService
        {
            get
            {
                if (_cadSelectionService is null)
                {
                    throw new InvalidOperationException(
                        "Le service CAO n'a pas été initialisé.");
                }

                return _cadSelectionService;
            }
        }


        private FreeCadHttpServer? _freeCadHttpServer;


        public string TemplateDirectory = Path.Combine(
                                                AppContext.BaseDirectory,
                                                "Asset",
                                                "Templates",
                                                "Linkages");

        #endregion

        public App()
        {

            Logger = new LoggerService();

            DialogService = new DialogService();
            NotificationService = new NotificationService();
            UiSettings = new UiSettingsService();

            IComputeEngine engine = ComputeEngineFactory.Create();
            MainVM = new MainViewModel(engine);


            #region CAD INTEGRATION

            string freeCadPythonDirectory = Path.Combine(
                                                        AppContext.BaseDirectory,
                                                        "FreeCAD");

            string freeCadDirectory = @"C:\Users\louis\AppData\Roaming\FreeCAD\v1-1";

            // Création de l'installateur spécifique à FreeCAD.
            var installer = new FreeCadModuleInstaller(
                                                        freeCadPythonDirectory,
                                                        freeCadDirectory);

            installer.Install();

            var cadProvider = new CadServiceProvider();

            var freeCad = new FreeCadApplication();

            cadProvider.Register(freeCad);

            ICadSelectionService freeCadSelectionService = freeCad.Selection;

            _cadSelectionService = freeCadSelectionService;

            _freeCadHttpServer = new FreeCadHttpServer(freeCadSelectionService);

            _freeCadHttpServer.Start();

            #endregion
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Logger.LogInfo("Application démarrée");

            await UiSettings.LoadAsync();

            // Assure la chargement initial des ressources
            AppResourceLoader.ApplySettings();


            //if (!AccessControl.VerifyAccess())
            //{
            //    Shutdown();
            //    return;
            //}

            ShutdownMode = ShutdownMode.OnMainWindowClose;


            //AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            //{
            //    MessageBox.Show("Exception non gérée : " + e.ExceptionObject.ToString());
            //};

        }



    }



}

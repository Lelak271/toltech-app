using System.Windows;
using Toltech.ComputeEngine;
using Toltech.ComputeEngine.Contracts;
using TOLTECH_APPLICATION.FrontEnd.Interfaces;
using TOLTECH_APPLICATION.Properties;
using TOLTECH_APPLICATION.Resources;
using TOLTECH_APPLICATION.Services.Dialog;
using TOLTECH_APPLICATION.Services.Logging;
using TOLTECH_APPLICATION.ViewModels;

namespace TOLTECH_APPLICATION
{
    /// Interaction logic for App.xaml
    public partial class App : Application
    {
        public static ILoggerService Logger { get; set; }
        public static INotificationService NotificationService { get; set; }
        public static IDialogService DialogService { get; set; }

        public static UiSettingsService UiSettings { get; private set; }

        public static MainViewModel MainVM { get; set; }


        public App()
        {

            Logger = new LoggerService();

            DialogService = new DialogService();
            NotificationService = new NotificationService();
            UiSettings = new UiSettingsService();

            IComputeEngine engine = ComputeEngineFactory.Create();
            MainVM = new MainViewModel(engine);

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

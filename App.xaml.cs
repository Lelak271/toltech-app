using System.Windows;
using Toltech.ComputeEngine;
using Toltech.ComputeEngine.Contracts;
using Toltech.App.Services.Notification;
using Toltech.App.Properties;
using Toltech.App.Resources;
using Toltech.App.Services.Dialog;
using Toltech.App.Services.Logging;
using Toltech.App.ViewModels;

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

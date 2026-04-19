//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Data;
//using System.Windows.Input;
//using OpenTK.Graphics.OpenGL;
//using System.Collections.ObjectModel;
//using TOLTECH_APPLICATION.Services.Logging;

//namespace TOLTECH_APPLICATION.ViewModels
//{
//    public class LogViewerViewModel : BaseViewModel
//    {
//        private readonly ILoggerService _logger;

//        public ICollectionView LogsView { get; }

//        public bool ShowInfo { get; set; } = true;
//        public bool ShowWarning { get; set; } = true;
//        public bool ShowError { get; set; } = true;
//        public bool ShowDebug { get; set; } = true;

//        public ICommand EraseCommand { get; }

//        public LogViewerViewModel(ILoggerService logger)
//        {
//            _logger = logger;


//            LogsView = CollectionViewSource.GetDefaultView(_logger.Logs);

//            LogsView.Filter = FilterLogs;

//            EraseCommand = RelayCommand.FromAction(() =>
//            {
//                System.Diagnostics.Debug.WriteLine("EraseCommand exécuté");
//                _logger.Logs.Clear();
//                LogsView.Refresh();
//            });
//        }

//        private bool FilterLogs(object obj)
//        {
//            if (obj is not LogEntry log)
//                return false;

//            return log.Level switch
//            {
//                LogLevel.Info => ShowInfo,
//                LogLevel.Warning => ShowWarning,
//                LogLevel.Error => ShowError,
//                LogLevel.Debug => ShowDebug,
//                _ => false
//            };
//        }

//        public void Refresh() => LogsView.Refresh();


//    }

//}

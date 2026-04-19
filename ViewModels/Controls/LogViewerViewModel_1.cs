using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using OpenTK.Graphics.OpenGL;
using Toltech.App.Services.Logging;
using Toltech.App.Services;

namespace Toltech.App.ViewModels
{
    public class LogViewerViewModel : BaseViewModel
    {
        private readonly LoggerService _loggerService;

        private readonly CollectionViewSource _collectionViewSource;
        public ICollectionView LogsView => _collectionViewSource.View;
        private readonly ILoggerService _logger;
        public ICommand EraseCommand { get; }


        #region Properties Filter

        private bool _showInfo = true;
        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                if (SetProperty(ref _showInfo, value))
                    LogsView.Refresh();
            }
        }

        private bool _showWarning = true;
        public bool ShowWarning
        {
            get => _showWarning;
            set
            {
                if (SetProperty(ref _showWarning, value))
                    LogsView.Refresh();
            }
        }

        private bool _showError = true;
        public bool ShowError
        {
            get => _showError;
            set
            {
                if (SetProperty(ref _showError, value))
                    LogsView.Refresh();
            }
        }

        private bool _showDebug = true;
        public bool ShowDebug
        {
            get => _showDebug;
            set
            {
                if (SetProperty(ref _showDebug, value))
                    LogsView.Refresh();
            }
        }

        #endregion

        #region Panneau flottant

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value))
                    UpdatePanelHeight();
            }
        }

        private double _panelHeight;
        public double PanelHeight
        {
            get => _panelHeight;
            set
            {
                if (Math.Abs(_panelHeight - value) > Constants.EPSILON)
                {
                    _panelHeight = value;
                    OnPropertyChanged();
                }
            }
        }


        public double _panelHeightExpanded = 250; // hauteur précédente avant compact
        private double _panelHeightCompact = 40;

        private void UpdatePanelHeight()
        {
            //PanelHeight = IsExpanded ? _panelHeightExpanded : _panelHeightCompact;
        }

        #endregion

        public LogViewerViewModel(ILoggerService logger)
        {
            _loggerService = logger as LoggerService
                ?? throw new ArgumentException("Logger must be LoggerService");

            _collectionViewSource = new CollectionViewSource
            {
                Source = _loggerService.Logs
            };

            // Tri décroissant pour afficher les derniers logs en premier
            _collectionViewSource.SortDescriptions.Add(
                new SortDescription(nameof(LogEntry.Timestamp), ListSortDirection.Descending)
            );

           _collectionViewSource.Filter += OnFilterLogs;

            EraseCommand = RelayCommand.FromAction(() =>
            {
                System.Diagnostics.Debug.WriteLine("EraseCommand exécuté");
                _loggerService.Logs.Clear();
                LogsView.Refresh();
            });

        }

        private void OnFilterLogs(object sender, FilterEventArgs e)
        {
            if (e.Item is not LogEntry log)
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = log.Level switch
            {
                LogLevel.Info => ShowInfo,
                LogLevel.Warning => ShowWarning,
                LogLevel.Error => ShowError,
                LogLevel.Debug => ShowDebug,
                _ => false
            };
        }
    }
}

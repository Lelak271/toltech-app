using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Toltech.App.FrontEnd.Controls;
using Toltech.App.FrontEnd.GraphePage.Controls;
using Toltech.App.Services;
using Toltech.App.ToltechCalculation.Helpers;
using Toltech.App.Utilities;
using Toltech.App.ViewModels;


namespace Toltech.App.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel MainVM => DataContext as MainViewModel;

        private MainGraphe _maingraphe;
        private readonly ComputeValidationService _computeValidationService;
        private readonly DomainService _domainService;

        public static MainWindow Instance { get; private set; }

        #region Constructeur
        public MainWindow()
        {
            this.InitializeComponent();
            SourceInitialized += Window_SourceInitialized;
            Instance = this;
            this.DataContext = App.MainVM;

            _computeValidationService = MainVM.ComputeValidationService;

            _maingraphe = new MainGraphe();
        }

        #endregion

        #region Interface UI "Calculs"

        // Vérification de l'isostatisme de toutes les parts
        private async void CheckInverseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ModelValidationHelper.CheckModelActif(true))
                return;

            Debug.WriteLine("[MainWindow] - CheckInverseButton_Click");
            await _computeValidationService.CheckIsoForAllPartAsync();
        }

        private async void AfficherMatriceButton_Click(object sender, RoutedEventArgs e)
        {
            // Vérification du graphe avant d'afficher la matrice 
            // null paramètre pour verifier toutes les requirments de la DB
            bool isGraphValid = await _computeValidationService.ValidateGraphDataAsync(true, null);
        }

        private void GenerateGraph_Click(object sender, RoutedEventArgs e)
        {
            _maingraphe = new MainGraphe();
            _maingraphe.Show();
        }

        #endregion

        #region Database

        private void OpenFloatingPanelDB_Click(object sender, RoutedEventArgs e)
        {
            var floatingWindow = new FloatingPanelDB();
            floatingWindow.Owner = this; // Associe à la fenêtre principale
            floatingWindow.Show(); // Non modal
        }
        private void OpenFloatingPartDB_Click(object sender, RoutedEventArgs e)
        {
            if (!ModelValidationHelper.CheckModelActif(true))
                return;
            var floatingWindow = new FloatingPanelPartDB(MainVM);
            floatingWindow.Owner = this; // Associe à la fenêtre principale
            floatingWindow.Show(); // Non modal
        }

        #endregion

        #region Paramètre
        private void OpenFloatingParametre_Click(object sender, RoutedEventArgs e)
        {
            var floatingWindow = new SettingsPage();
            floatingWindow.Owner = this; // Associe à la fenêtre principale
            floatingWindow.Show(); // Non modal

        }
        #endregion

        #region Main Ribbon Custom

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Minimize_Click");
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Maximize_Click");
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        #region HOOK - Maximize screen

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);

                var workArea = monitorInfo.rcWork;
                var monitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.x = Math.Abs(workArea.left - monitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(workArea.top - monitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(workArea.right - workArea.left);
                mmi.ptMaxSize.y = Math.Abs(workArea.bottom - workArea.top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        #endregion

        #endregion
        private const double ResizeThreshold = 2.0; // seuil en pixels
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // Ignore les petits mouvements
            if (Math.Abs(e.VerticalChange) < ResizeThreshold)
                return;

            // Calcule la nouvelle hauteur
            double newHeight = LoggerGrid.ActualHeight - (e.VerticalChange * 0.95);

            // Hauteur minimale
            if (newHeight < 40)
                newHeight = 40;

            // Limite max = 60% de la fenêtre
            var window = Window.GetWindow(this);
            if (window != null)
            {
                double maxHeight = window.ActualHeight * 0.6;
                if (newHeight > maxHeight)
                    newHeight = maxHeight;
            }

            // **Met à jour la propriété PanelHeight dans la VM**
            if (DataContext is LogViewerViewModel vm)
            {
                MainVM.StatusBarVM.PanelHeight = newHeight;            // applique la nouvelle hauteur
                MainVM.StatusBarVM._panelHeightExpanded = newHeight;  // mémorise comme hauteur "expanded"
            }

            // Appliquer au Grid pour le rendu immédiat
            LoggerGrid.Height = newHeight;
        }

        public void ReduceLoggerGrid()
        {
            LoggerGrid.Height = 40;
        }
    }
}

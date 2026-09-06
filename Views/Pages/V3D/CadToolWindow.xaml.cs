using System.Windows;
using Toltech.App.Utilities.Object3D;
using Toltech.App.ViewModels;

namespace Toltech.App.Views
{
    /// <summary>
    /// Logique d'interaction pour CadToolWindow.xaml
    /// </summary>
    public partial class CadToolWindow : Window
    {
        private V3DViewModel ViewModel => DataContext as V3DViewModel;

        public CadToolWindow()
        {
            InitializeComponent();
        }


    }

}

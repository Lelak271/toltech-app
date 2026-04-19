using System;
using System.Collections.ObjectModel;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;
using QuickGraph;
using Toltech.App;
using Toltech.App.Resources;
using Toltech.App.Services;
using Toltech.App.ToltechCalculation;
using Toltech.App.Z_Test;
using Toltech.App.ViewModels;


namespace Toltech.App.Views
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();
        }
       
        private void AppelMatrixRain_Click(object sender, RoutedEventArgs e)
        {
            var matrixWindow = new MatrixRain(); // Assuming MatrixRainWindow is the class for the matrix rain effect
            matrixWindow.Show();
        }


    }
}

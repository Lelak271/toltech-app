using System;
using System.Collections.ObjectModel;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic;
using QuickGraph;
using TOLTECH_APPLICATION;
using TOLTECH_APPLICATION.Resources;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.ToltechCalculation;
using TOLTECH_APPLICATION.Z_Test;
using TOLTECH_APPLICATION.ViewModels;


namespace TOLTECH_APPLICATION.Views
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

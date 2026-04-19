using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Toltech.App.Resources;
using Toltech.App.ViewModels;

namespace Toltech.App.FrontEnd.Controls
{
    public partial class FloatingPanelPartDB : Window
    {
        public FloatingPanelPartDB(MainViewModel mainVM)
        {
            DataContext = mainVM.PartVM;

            InitializeComponent();
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (DataContext is PartDBViewModel vm && vm.LoadCommand.CanExecute(null))
                {
                    // On exécute la commande synchronously ou async selon ton implémentation
                    vm.LoadCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du LoadAsync à la fermeture : {ex}");
            }
        }


    }
}

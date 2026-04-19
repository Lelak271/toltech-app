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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Media;
using System.Windows;

namespace Toltech.App.FrontEnd.Controls
{
    /// <summary>
    /// Logique d'interaction pour LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
            this.Closing += MaFenetre_Closing;
        }

        public void UpdateMessage(string message)
        {
            Dispatcher.Invoke(() => ProgressMessage.Text = message);
        }

        private void MaFenetre_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SystemSounds.Exclamation.Play();
            //SystemSounds.Hand.Play();
        }
    }
}
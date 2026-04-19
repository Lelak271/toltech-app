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

namespace TOLTECH_APPLICATION.FrontEnd.Controls
{
    /// <summary>
    /// Logique d'interaction pour PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : Window
    {
        public string EnteredPassword { get; private set; }

        public PasswordWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            EnteredPassword = PwdBox.Password;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Psw_Window_Loaded(object sender, RoutedEventArgs e)
        {
            PwdBox.Focus();           // Met le focus sur le PasswordBox
            PwdBox.SelectAll();       // Sélectionne tout le texte si nécessaire
        }
    }
}

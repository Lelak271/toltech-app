using System.Windows;

namespace TOLTECH_APPLICATION.FrontEnd.Controls
{
    /// <summary>
    /// Logique d'interaction pour RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window
    {
        public string NewName { get; private set; }

        public RenameDialog(string oldName)
        {
            InitializeComponent();
            Title = "Rename Folder";
            TextBoxName.Text = oldName;
            TextBoxName.Focus();
            TextBoxName.SelectAll();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            NewName = TextBoxName.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RenameWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxName.Focus();           // Met le focus sur le PasswordBox
            TextBoxName.SelectAll();       // Sélectionne tout le texte si nécessaire
        }

    }
}

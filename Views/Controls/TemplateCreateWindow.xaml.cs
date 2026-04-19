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
using Toltech.App.Utilities;

namespace Toltech.App.FrontEnd.Controls
{
    public partial class TemplateCreateWindow : Window
    {
        public string EnteredName { get; private set; }

        public enum TemplateCreateWindowType
        {
            Model,
            Part,
            Requirement
        }


        private readonly TemplateCreateWindowType _windowType;

        public TemplateCreateWindow(TemplateCreateWindowType type, string defaultName = null)
        {
            InitializeComponent();
            _windowType = type;
            // Si pas de nom fourni, génère un nom automatique via RandomizeNaming
            TextBoxName.Text = string.IsNullOrWhiteSpace(defaultName)
                ? RandomizeNaming.GenerateName(type)
                : defaultName;



            ApplyTheme(type);
        }

        private void BtnRandom_Click(object sender, RoutedEventArgs e)
        {
            TextBoxName.Text = RandomizeNaming.GenerateName(_windowType);
        }

        private void ApplyTheme(TemplateCreateWindowType type)
        {
            // Ajustement du thème (texte + image) selon le type
            switch (type)
            {
                case TemplateCreateWindowType.Model:
                    DialogText.Text = "Enter new model name:";
                    DialogImage.Source = new BitmapImage(new Uri("pack://application:,,,/Asset/FolderModel.png"));
                    break;

                case TemplateCreateWindowType.Part:
                    DialogText.Text = "Enter new part name:";
                    DialogImage.Source = new BitmapImage(new Uri("pack://application:,,,/Asset/PARTS.png"));
                    break;

                case TemplateCreateWindowType.Requirement:
                    DialogText.Text = "Enter new requirement name:";
                    DialogImage.Source = new BitmapImage(new Uri("pack://application:,,,/Asset/Exigence.png"));
                    break;
            }

            // Style dynamique par thème
            switch (type)
            {
                case TemplateCreateWindowType.Model:
                    this.Resources["DialogBackground"] = Brushes.LightBlue;
                    break;
                case TemplateCreateWindowType.Part:
                    this.Resources["DialogBackground"] = Brushes.LightGreen;
                    break;
                case TemplateCreateWindowType.Requirement:
                    this.Resources["DialogBackground"] = Brushes.LightGoldenrodYellow;
                    break;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            EnteredName = TextBoxName.Text.Trim();
            if (string.IsNullOrEmpty(EnteredName))
            {
                MessageBox.Show("Please enter a name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TemplateMsgWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxName.Focus();           // Met le focus sur le PasswordBox
            TextBoxName.SelectAll();       // Sélectionne tout le texte si nécessaire
        }

    }
}

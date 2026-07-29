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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Toltech.App.Views.Pages.DataPage
{
    /// <summary>
    /// Éditeur d'une tolérance unique (nom, description, valeur).
    /// Se bind sur un ToleranceSlotViewModel (ex: N.Intermediate).
    /// </summary>
    public partial class ToleranceEditor : UserControl
    {
        public ToleranceEditor()
        {
            InitializeComponent();
        }

        public string PlaceholderName
        {
            get => (string)GetValue(PlaceholderNameProperty);
            set => SetValue(PlaceholderNameProperty, value);
        }

        public static readonly DependencyProperty PlaceholderNameProperty =
            DependencyProperty.Register(
                nameof(PlaceholderName),
                typeof(string),
                typeof(ToleranceEditor));

        public string PlaceholderDescription
        {
            get => (string)GetValue(PlaceholderDescriptionProperty);
            set => SetValue(PlaceholderDescriptionProperty, value);
        }

        public static readonly DependencyProperty PlaceholderDescriptionProperty =
            DependencyProperty.Register(
                nameof(PlaceholderDescription),
                typeof(string),
                typeof(ToleranceEditor));

        public string PlaceholderTolerance
        {
            get => (string)GetValue(PlaceholderToleranceProperty);
            set => SetValue(PlaceholderToleranceProperty, value);
        }

        public static readonly DependencyProperty PlaceholderToleranceProperty =
            DependencyProperty.Register(
                nameof(PlaceholderTolerance),
                typeof(string),
                typeof(ToleranceEditor));

        public bool Active
        {
            get => (bool)GetValue(ActiveProperty);
            set => SetValue(ActiveProperty, value);
        }

        public static readonly DependencyProperty ActiveProperty =
            DependencyProperty.Register(
                nameof(Active),
                typeof(bool),
                typeof(ToleranceEditor),
                new PropertyMetadata(false));


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows;
using Toltech.App.Utilities.Object3D;
using Toltech.App.ViewModels;

namespace Toltech.App.Views
{
    /// <summary>
    /// Logique d'interaction pour CadTransform.xaml
    /// </summary>
    public partial class CadTransformWindow : Window
    {
        private V3DViewModel ViewModel => DataContext as V3DViewModel;
        public CadTransformWindow()
        {
            InitializeComponent();
        }

        private void CadObjectsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            switch (e.NewValue)
            {
                case CadObject cadObject:
                    ViewModel?.SelectCadObject(cadObject);
                    break;

                case CadNode node:
                    ViewModel?.SelectCadObject(node.Owner);
                    break;
            }
        }

    }
}

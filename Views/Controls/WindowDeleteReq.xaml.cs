//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;
//using Toltech.App.Services;

//namespace Toltech.App
//{
//    /// <summary>
//    /// Logique d'interaction pour Window_Delete_Req.xaml
//    /// </summary>
//    /// 

//    public partial class WindowDeleteReq : Window
//    {
//        private DatabaseService _databaseService;
//        public WindowDeleteReq()
//        {
//            InitializeComponent();
//            InitializeDatabaseService();
//        }

//        private void InitializeDatabaseService()
//        {
//            _databaseService = new DatabaseService(ModelManager.ModelActif);
//        }

//        private async void LoadRequirements()
//        {

//            var pieces = await _databaseService.GetRequirementsAsync();

//            if (pieces == null || pieces.Count == 0)
//            {
//                pieces = new List<string> { "Veuillez sélectionner un modèle" };
//            }

//            var uniquePieces = pieces.Distinct().ToList();
//            ReqListBox.ItemsSource = uniquePieces;
//        }

//        private async void DeleteReqButton_Click(object sender, RoutedEventArgs e)
//        {
//            var selectedPiece = ReqListBox.SelectedItem as string;

//            if (string.IsNullOrWhiteSpace(selectedPiece))
//            {
//                MessageBox.Show("Veuillez sélectionner une exigence à supprimer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            var confirm = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer \"{selectedPiece}\" ?",
//                                          "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

//            if (confirm == MessageBoxResult.Yes)
//            {
//                await _databaseService.DeleteRequirementAsync(selectedPiece);
//                //MessageBox.Show($"Pièce \"{selectedPiece}\" supprimée.", "Suppression", MessageBoxButton.OK, MessageBoxImage.Information);
//                //LoadPieces(); // Refresh après suppression

//            }


//        }
//    }
//}

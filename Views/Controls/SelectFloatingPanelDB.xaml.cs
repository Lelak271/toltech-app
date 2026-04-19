using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.Models;

namespace TOLTECH_APPLICATION.Front
{
    public partial class SelectFloatingPanelDB : Window
    {
        private DatabaseService _databaseService;
        public event EventHandler<ToleranceSelectedEventArgs> ToleranceSelected;
        private string currentContextTarget = string.Empty; // "Part1" ou "Part2"

        public SelectFloatingPanelDB()
        {
            InitializeComponent();
            InitializeDataGridColumns();
            LoadTolerancesFromActiveModel();
        }


        public void UpdateUI()
        {
            InitializeDataGridColumns();
            LoadTolerancesFromActiveModel();
        }

        // Classe adaptée au tri et aux colonnes déclarées dynamiquement
        public class ToleranceRow
        {
            public int Id { get; set; }
            public string DescriptionTolInt { get; set; }
            public string NameTolInt { get; set; }
            public double tolInt { get; set; }
        }

        private void InitializeDataGridColumns()
        {
            ToleranceGrid.AutoGenerateColumns = false;
            ToleranceGrid.Columns.Clear();

            AddTextColumn("ID", "Id", 50);
            AddTextColumn("NameTolInt", "NameTolInt");
            AddTextColumn("DescriptionTolInt", "DescriptionTolInt");
            AddTextColumn("tolInt", "tolInt");
        }

        private void AddTextColumn(string header, string bindingPath, double width = double.NaN)
        {
            var column = new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(bindingPath),
                Width = double.IsNaN(width) ? DataGridLength.Auto : width
            };
            ToleranceGrid.Columns.Add(column);
        }

        private async Task LoadTolerancesFromActiveModel()
        {
            try
            {
                Debug.WriteLine($"[Tolérance] Chargement des données depuis le modèle actif : {ModelManager.ModelActif}");

                if (string.IsNullOrEmpty(ModelManager.ModelActif))
                {
                    MessageBox.Show("Aucun modèle actif sélectionné.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _databaseService = new DatabaseService("TODO");
                _databaseService.Open(ModelManager.ModelActif);
                var tolerances = await _databaseService.GetTolerancesAsync();

                if (tolerances == null || !tolerances.Any())
                {
                    Debug.WriteLine("Aucune tolérance trouvée dans la base.");
                    ToleranceGrid.ItemsSource = null;
                    return;
                }

                var rows = tolerances.Select(tol => new ToleranceRow
                {
                    Id = tol.Id_tol,
                    DescriptionTolInt = tol.DescriptionTolInt,
                    NameTolInt = tol.NameTolInt,
                    tolInt = tol.tolInt,
                }).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ToleranceGrid.Items.Clear();
                    ToleranceGrid.ItemsSource = rows;
                });

                Debug.WriteLine($"[Tolérance] {rows.Count} lignes chargées.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des tolérances : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveTolerancesAsync()
        {
            try
            {
                if (_databaseService == null)
                {
                    MessageBox.Show("Service base de données non initialisé.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (var item in ToleranceGrid.Items)
                {
                    if (item is ToleranceRow tol)
                    {
                        var toleranceToSave = new DBTolerances
                        {
                            Id_tol = tol.Id,
                            DescriptionTolInt = tol.DescriptionTolInt,
                            NameTolInt = tol.NameTolInt.ToString(),
                            tolInt = tol.tolInt,
                        };

                        if (toleranceToSave.Id_tol == 0)
                        {
                            // Création d'une nouvelle tolérance
                            await _databaseService.InsertToleranceAsync(toleranceToSave);
                            UpdateUI();
                        }
                        else
                        {
                            // Mise à jour d'une tolérance existante
                            await _databaseService.UpdateToleranceAsync(toleranceToSave);
                        }
                    }
                }

                MessageBox.Show("Tolérances sauvegardées avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveTolerancesAsync();
        }


        #region Clique-droit SELECT

        public class ToleranceSelectedEventArgs : EventArgs
        {
            public string ToleranceName { get; set; }
            public string ToleranceDescription { get; set; }
            public string ToleranceValue { get; set; }
            public int ToleranceID { get; set; }
        }


        // Méthode à appeler quand une tolérance est choisie dans votre UI
        private void OnToleranceChosen(int id, string name, string description, string tol)
        {
            ToleranceSelected?.Invoke(this, new ToleranceSelectedEventArgs
            {
                ToleranceID = id,
                ToleranceName = name,
                ToleranceDescription = description,
                ToleranceValue = tol
            });
            this.Close();
        }

        // Exemple : bouton ou double clic sur une tolérance dans la liste
        private void SelectToleranceButton_Click(object sender, RoutedEventArgs e)
        {
            // Vérifie qu'une ligne est bien sélectionnée
            if (ToleranceGrid.SelectedItem is ToleranceRow selectedRow)
            {
                int selectedId = selectedRow.Id;
                string selectedName = selectedRow.NameTolInt;
                string selectedDescription = selectedRow.DescriptionTolInt;
                string selectedTol = selectedRow.tolInt.ToString(); // Conversion double -> string

                OnToleranceChosen(selectedId, selectedName, selectedDescription, selectedTol);
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une ligne de tolérance avant de valider.", "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion


    }
}

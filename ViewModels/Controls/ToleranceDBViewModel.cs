using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Toltech.App.Models;
using Toltech.App.Services;
using Toltech.App.ViewModels;

namespace Toltech.App.ViewModels
{
    public class ToleranceDBViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;

        private ObservableCollection<ToleranceRow> _selectedTolerances
            = new ObservableCollection<ToleranceRow>();

        public ObservableCollection<ToleranceRow> SelectedTolerances
        {
            get => _selectedTolerances;
            set => SetProperty(ref _selectedTolerances, value);
        }


        public ToleranceDBViewModel()
        {
            if (string.IsNullOrEmpty(ModelManager.ModelActif))
            {
                MessageBox.Show("Aucun modèle actif sélectionné.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _databaseService = new DatabaseService("TODO");
            // TODO
            _databaseService.Open(ModelManager.ModelActif);

            Tolerances = new ObservableCollection<ToleranceRow>();
            LoadCommand = RelayCommand.FromAsync(LoadTolerancesAsync);
            SaveCommand = RelayCommand.FromAsync(SaveTolerancesAsync);
            CreateCommand = RelayCommand.FromAsync(CreateToleranceAsync);
            DeleteCommand = RelayCommand.FromAsync(DeleteSelectedTolerancesAsync);


            _ = LoadTolerancesAsync();
        }

        // ----------------------------------------------------------------
        // Collections
        // ----------------------------------------------------------------
        public ObservableCollection<ToleranceRow> Tolerances { get; }

        // ----------------------------------------------------------------
        // Commands
        // ----------------------------------------------------------------
        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand DeleteCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // ----------------------------------------------------------------
        // Méthodes Async
        // ----------------------------------------------------------------

        public async Task LoadTolerancesAsync()
        {
            await ExecuteBusyAsync("Chargement des tolérances...", async () =>
            {
                Tolerances.Clear();
                var tolerances = await _databaseService.GetTolerancesAsync();
                if (tolerances == null || !tolerances.Any()) return;

                foreach (var tol in tolerances)
                {
                    Tolerances.Add(new ToleranceRow
                    {
                        Id = tol.Id_tol,
                        NameTolInt = tol.NameTolInt,
                        DescriptionTolInt = tol.DescriptionTolInt,
                        tolInt = tol.tolInt
                    });
                }
            });
        }

        public async Task SaveTolerancesAsync()
        {
            await ExecuteBusyAsync("Sauvegarde des tolérances...", async () =>
            {
                foreach (var tol in Tolerances)
                {
                    var dbTol = new DBTolerances
                    {
                        Id_tol = tol.Id,
                        NameTolInt = tol.NameTolInt,
                        DescriptionTolInt = tol.DescriptionTolInt,
                        tolInt = tol.tolInt
                    };

                    if (dbTol.Id_tol == 0)
                        await _databaseService.InsertToleranceAsync(dbTol);
                    else
                        await _databaseService.UpdateToleranceAsync(dbTol);
                }
            });

            MessageBox.Show("Tolérances sauvegardées avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task CreateToleranceAsync()
        {
            await ExecuteBusyAsync("Création d'une tolérance...", async () =>
            {
                var newTol = new DBTolerances();
                await _databaseService.InsertToleranceAsync(newTol);
                Tolerances.Add(new ToleranceRow
                {
                    Id = newTol.Id_tol,
                    NameTolInt = newTol.NameTolInt,
                    DescriptionTolInt = newTol.DescriptionTolInt,
                    tolInt = newTol.tolInt
                });
            });
        }

        public async Task DeleteToleranceAsync(ToleranceRow tol)
        {
            if (tol == null) return;

            var result = MessageBox.Show($"Confirmez-vous la suppression de {tol.NameTolInt} ?",
                                         "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            await ExecuteBusyAsync("Suppression...", async () =>
            {
                var dbTol = new DBTolerances
                {
                    Id_tol = tol.Id,
                    NameTolInt = tol.NameTolInt,
                    DescriptionTolInt = tol.DescriptionTolInt,
                    tolInt = tol.tolInt
                };

                await _databaseService.DeleteToleranceAsync(dbTol);
                Tolerances.Remove(tol);
            });
        }

        public async Task DeleteSelectedTolerancesAsync()
        {
            if (SelectedTolerances == null || SelectedTolerances.Count == 0)
                return;

            var result = MessageBox.Show(
                $"Confirmez-vous la suppression de {SelectedTolerances.Count} tolérance(s) ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            await ExecuteBusyAsync("Suppression des tolérances...", async () =>
            {
                // Copie pour éviter modification pendant itération
                var toDelete = SelectedTolerances.ToList();

                foreach (var tol in toDelete)
                {
                    var dbTol = new DBTolerances
                    {
                        Id_tol = tol.Id,
                        NameTolInt = tol.NameTolInt,
                        DescriptionTolInt = tol.DescriptionTolInt,
                        tolInt = tol.tolInt
                    };

                    await _databaseService.DeleteToleranceAsync(dbTol);
                    Tolerances.Remove(tol);
                }

                SelectedTolerances.Clear();
            });
        }


        // ----------------------------------------------------------------
        // Helper
        // ----------------------------------------------------------------
        private async Task ExecuteBusyAsync(string message, Func<Task> action)
        {
            IsBusy = true;
            StatusMessage = message;

            try
            {
                await action();
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }
    }

    // DTO pour DataGrid
    public class ToleranceRow
    {
        public int Id { get; set; }
        public string NameTolInt { get; set; }
        public string DescriptionTolInt { get; set; }
        public double tolInt { get; set; }
    }
}

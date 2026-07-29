using System.ComponentModel;
using System.Linq.Expressions;
using Toltech.App.Models;

namespace Toltech.App.ViewModels
{
    /// <summary>
    /// Représente une tolérance à une position donnée (Origin, Intermediate ou Extremity).
    /// Expose Name, Description, Value, Id, UseDatabase via des PropertyAdapter.
    /// Relaie les PropertyChanged de ModelData vers le binding WPF.
    /// </summary>
    public sealed class ToleranceSlotViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly ModelData _model;
        private readonly PropertyAdapter<string> _name;
        private readonly PropertyAdapter<string> _description;
        private readonly PropertyAdapter<double> _value;
        private readonly PropertyAdapter<int> _id;
        private readonly PropertyAdapter<bool> _database;

        public ToleranceSlotViewModel(
             ModelData model,
             Expression<Func<ModelData, string>> name,
             Expression<Func<ModelData, string>> description,
             Expression<Func<ModelData, double>> value,
             Expression<Func<ModelData, int>> id,
             Expression<Func<ModelData, bool>> database)
        {
            _model = model;

            _name = new(model, name);
            _description = new(model, description);
            _value = new(model, value);
            _id = new(model, id);
            _database = new(model, database);

            _model.PropertyChanged += Model_PropertyChanged;
        }

        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        public string Description
        {
            get => _description.Value;
            set => _description.Value = value;
        }

        public double Value
        {
            get => _value.Value;
            set => _value.Value = value;
        }

        public int Id
        {
            get => _id.Value;
            set => _id.Value = value;
        }

        public bool UseDatabase
        {
            get => _database.Value;
            set => _database.Value = value;
        }

        private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _name.PropertyName)
                OnPropertyChanged(nameof(Name));

            if (e.PropertyName == _description.PropertyName)
                OnPropertyChanged(nameof(Description));

            if (e.PropertyName == _value.PropertyName)
                OnPropertyChanged(nameof(Value));

            if (e.PropertyName == _id.PropertyName)
                OnPropertyChanged(nameof(Id));

            if (e.PropertyName == _database.PropertyName)
                OnPropertyChanged(nameof(UseDatabase));
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

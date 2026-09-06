using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Toltech.App.ViewModels
{
    /// <summary>
    /// Base commune : gère CanExecuteChanged/RaiseCanExecuteChanged une seule fois
    /// pour toutes les variantes de commande ci-dessous.
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Commande synchrone sans paramètre. Cas le plus courant dans les VM de Toltech
    /// (CadSelectCommand, CadMeasureCommand, ResetSelectedTransformCommand, ...).
    /// </summary>
    public sealed class RelayCommand : CommandBase
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public override void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// Commande synchrone avec paramètre typé (ex : CopyPointComponentCommand,
    /// CommandParameter="X"/"Y"/"Z").
    /// </summary>
    public sealed class RelayCommand<T> : CommandBase
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return _canExecute?.Invoke(default) ?? true;
            return parameter is T t && (_canExecute?.Invoke(t) ?? true);
        }

        public override void Execute(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                _execute(default);
            else if (parameter is T t)
                _execute(t);
        }
    }

    /// <summary>
    /// Commande asynchrone sans paramètre. Se désactive automatiquement (CanExecute = false)
    /// tant qu'une exécution est en cours : empêche le double-clic pendant une mesure,
    /// un calcul de normale, un déplacement de sommet, etc.
    /// </summary>
    public sealed class AsyncRelayCommand : CommandBase
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter)
            => !_isRunning && (_canExecute?.Invoke() ?? true);

        public override async void Execute(object parameter)
        {
            _isRunning = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute();
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Commande asynchrone avec paramètre typé. Même garde-fou anti double-exécution
    /// que AsyncRelayCommand.
    /// </summary>
    public sealed class AsyncRelayCommand<T> : CommandBase
    {
        private readonly Func<T, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private bool _isRunning;

        public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter)
        {
            if (_isRunning) return false;
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                return _canExecute?.Invoke(default) ?? true;
            return parameter is T t && (_canExecute?.Invoke(t) ?? true);
        }

        public override async void Execute(object parameter)
        {
            T value = parameter is T t ? t : default;

            _isRunning = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute(value);
            }
            finally
            {
                _isRunning = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}
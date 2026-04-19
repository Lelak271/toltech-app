using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOLTECH_APPLICATION.ViewModels
{

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        // Constructeur
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // ICommand
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged;

        // Permet de notifier la vue que CanExecute a changé
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        // Méthode helper pour commandes sans paramètre
        public static RelayCommand FromAction(Action action, Func<bool> canExecute = null)
        {
            return new RelayCommand(
                _ => action(),
                canExecute != null ? new Func<object, bool>(_ => canExecute()) : (Func<object, bool>)null
            );
        }

        // Méthode helper pour commandes async
        public static RelayCommand FromAsync(Func<Task> asyncAction, Func<bool> canExecute = null)
        {
            return new RelayCommand(
                async _ => await asyncAction(),
                canExecute != null ? new Func<object, bool>(_ => canExecute()) : (Func<object, bool>)null
            );
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Func<T, Task> asyncExecute, Func<T, bool> canExecute = null)
        {
            if (asyncExecute == null) throw new ArgumentNullException(nameof(asyncExecute));
            _execute = async t => _ = asyncExecute(t); // Fire-and-forget
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null && typeof(T).IsValueType) return _canExecute(default!);
            return parameter is T t && _canExecute(t);
        }

        public void Execute(object parameter)
        {
            if (parameter == null && typeof(T).IsValueType)
                _execute(default!);
            else if (parameter is T t)
                _execute(t);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

}

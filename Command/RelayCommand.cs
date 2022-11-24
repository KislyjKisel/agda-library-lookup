using System;
using System.Windows.Input;

namespace AgdaLibraryLookup.Command
{
    public sealed class RelayCommand : ICommand
    {
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            => (_exectute, _canExectute) = (execute, canExecute);

        public void Execute   (object? parameter) => _exectute(parameter);
        public bool CanExecute(object? parameter) => _canExectute is null || _canExectute(parameter);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);



        private readonly Action<object?>     _exectute;
        private readonly Predicate<object?>? _canExectute;
    }
}

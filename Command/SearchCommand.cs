using AgdaLibraryLookup.Agda.Connection;
using AgdaLibraryLookup.Model;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AgdaLibraryLookup.Command
{
    public sealed class SearchCommand : ICommand
    {
        public SearchCommand(ICommunicator agda, Search search) {
            _search = search;
            _agda   = agda;

            search.PropertyChanged += (_, e) => {
                if(e.PropertyName == nameof(search.IsBusy))
                {
                    Application.Current.Dispatcher.Invoke(
                        () => CanExecuteChanged?.Invoke(this, new()));
                }
            };
        }

        public void Execute(object? parameter)
        {
            var qparams = (QueryParams)parameter!;

            if(String.IsNullOrWhiteSpace(qparams.Query)) return;
            
             _currentTask = Task.Run(() => _search.Query(qparams, _agda)).ConfigureAwait(false);
        }

        public bool CanExecute(object? parameter)
            => parameter is QueryParams && !_search.IsBusy;

        public event EventHandler? CanExecuteChanged;


        private readonly ICommunicator _agda;
        private readonly Search       _search;

        private ConfiguredTaskAwaitable? _currentTask;
    }
}

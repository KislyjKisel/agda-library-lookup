using AgdaLibraryLookup.Agda.Connection;
using AgdaLibraryLookup.Model;
using Microsoft.Win32;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AgdaLibraryLookup.Command
{
    public sealed class BuildIndexCommand : ICommand
    {
        public BuildIndexCommand(ICommunicator agda, IndexBuilder indexBuilder) 
        {
            (_agda, _indexBuilder) = (agda, indexBuilder);
            _agda.IsBusyChanged += (_, e) => Application.Current.Dispatcher.Invoke(() => CanExecuteChanged?.Invoke(this, e));
            _openFileDialog = new()
            {
                Title = "Select agda library description file",
                Filter = "Agda Library|*.agda-lib"
            };
        }

        #region ICommand

        public void Execute(object? parameter) 
        {
            bool? r = _openFileDialog.ShowDialog();
            if (r.HasValue && r.Value)
            {
                _currentTask = Task.Run(() => 
                    _indexBuilder.BuildIndexFor(_openFileDialog.FileName, _agda))
                    //.ContinueWith(t => { 
                    //    Application.Current.Dispatcher.Invoke(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)); 
                    //})
                    .ConfigureAwait(false);
            }
        }
        
        public bool CanExecute(object? parameter) 
            => !_currentTask.HasValue || _currentTask.Value.GetAwaiter().IsCompleted;

        public event EventHandler? CanExecuteChanged;

        #endregion

        private readonly ICommunicator _agda;
        private readonly IndexBuilder _indexBuilder;
        private readonly OpenFileDialog _openFileDialog;
        private ConfiguredTaskAwaitable? _currentTask;
    }
}

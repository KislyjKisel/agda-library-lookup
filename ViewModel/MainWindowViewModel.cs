using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using AgdaLibraryLookup.Agda.Connection;
using AgdaLibraryLookup.Command;
using AgdaLibraryLookup.Model;

namespace AgdaLibraryLookup.ViewModel
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        #region IndexBuilding
        
        public ICommand BuildIndexCommand { get; init; }

        public int IndexBuilderDefinitionCount => _indexBuilder.DefinitionsProcessedCount;
        
        public ObservableCollection<string> IndexBuilderModulesProcessed => _indexBuilder.ModulesProcessed;
        
        public ObservableCollection<string> IndexBuilderLog => _indexBuilder.Log;

        
        private readonly IndexBuilder _indexBuilder = new();

        #endregion

        #region Search

        public ICommand UpdateIndexCommand { get; init; }
        public ICommand SearchCommand { get; init; }
        public ICommand DefinitionSelected { get; init; }

        public QueryParams QueryParams { get; init; } = new();

        public ObservableCollection<LibraryIndex> IndexedLibraries => _search.IndexedLibraries;
        public ObservableCollection<string> SearchLog => _search.Log;

        public ObservableCollection<IndexEntry> FoundDefinitions 
            => _search.FoundDefinitions;


        private readonly Search _search = new();

        private void OpenDefinition(IndexEntry indexEntry)
        {
            string agdaLibReg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "agda", "libraries");
            
            string? modulePath = null;
            using var rsr = new StringReader(File.ReadAllText(agdaLibReg));
            string? libDescrPath;
            while((libDescrPath = rsr.ReadLine()) is not null)
            {
                using var lsr = new StringReader(File.ReadAllText(libDescrPath));
                string? line;
                string[]? libSrcDirs = null;
                bool reqlib = false;
                while((line = lsr.ReadLine()) is not null)
                {
                    if(line.StartsWith("include:"))
                    {
                        libSrcDirs = line[8..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    }
                    if(!line.StartsWith("name:")) continue;
                    string libName = line[5..].Trim();
                    if(libName == indexEntry.Library)
                    {
                        reqlib = true;
                    }
                }
                if(!reqlib) continue;

                if(libSrcDirs is null)
                {
                    SearchLog.Add("Couldn't parse library description file.");
                    return;
                }

                string moduleRelPath = indexEntry.Module.Replace('.', '/') + ".agda";
                foreach (var libSrcDir in libSrcDirs)
                {
                    string assumedModulePath = Path.Combine(Directory.GetParent(libDescrPath).FullName, libSrcDir, moduleRelPath);
                    if(File.Exists(assumedModulePath))
                    {
                        modulePath = assumedModulePath;
                        break;
                    }
                }

                break;
            }
            if(modulePath is null)
            {
                SearchLog.Add("Couldn't find file.");
                return;
            }
            new Process { StartInfo = new(modulePath) { UseShellExecute = true } }.Start();
        }

        #endregion

        public MainWindowViewModel()
        {
            _agda = new LocalProcessCommunicator("agda", true);

            _search.PropertyChanged += (_, e) => {
                string? propname = e.PropertyName switch
                {
                    nameof(_search.FoundDefinitions) => nameof(this.FoundDefinitions),
                    nameof(_search.Log)              => nameof(this.SearchLog),
                    nameof(_search.IndexedLibraries) => nameof(this.IndexedLibraries),
                    _ => null
                };
                /*if (propname is not null) */this.PropertyChanged?.Invoke(this, new(propname));
            };

            _indexBuilder.PropertyChanged += (_, e) => {
                string? propname = e.PropertyName switch
                {
                    nameof(_indexBuilder.ModulesProcessed)          => nameof(this.IndexBuilderModulesProcessed),
                    nameof(_indexBuilder.DefinitionsProcessedCount) => nameof(this.IndexBuilderDefinitionCount),
                    nameof(_indexBuilder.Log)                       => nameof(this.IndexBuilderLog),
                    _ => null 
                };
                /*if (propname is not null) */this.PropertyChanged?.Invoke(this, new(propname));
            };

            SearchCommand        = new SearchCommand(_agda, _search);
            BuildIndexCommand    = new BuildIndexCommand(_agda, _indexBuilder);
            UpdateIndexCommand   = new RelayCommand(_ => _search.UpdateIndex(), _ => !_search.IsBusy);
            DefinitionSelected   = new RelayCommand(
                entry => OpenDefinition((IndexEntry)entry!), 
                entry => entry is IndexEntry);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        private readonly ICommunicator _agda;
    }
}

using AgdaLibraryLookup.Agda.Connection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static AgdaLibraryLookup.Functional.FunctionalExt;

namespace AgdaLibraryLookup.Model
{
    public sealed class Search : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<IndexEntry> FoundDefinitions 
            => _definitionsFound;

        public ObservableCollection<string> Log => _log;
        
        public bool IsBusy { get; private set; }

        public ObservableCollection<LibraryIndex> IndexedLibraries => _indexedLibraries;

        public async Task Query(QueryParams qparams, ICommunicator agda)
        {
            IsBusy = true;
            NotifyPropertyChanged(nameof(IsBusy));

            List<(string, List<string>)> examinedLibraries = new();
            foreach(var indexEntry in _indexedLibraries)
            {
                List<string> examinedModules = new();
                bool containsEnabledModules = false;
                foreach(var module in indexEntry.Modules.Traverse())
                {
                    if(module.Enabled)
                    {
                        containsEnabledModules = true;
                        examinedModules.Add(module.Path);
                    }
                }
                if(containsEnabledModules) 
                {
                    examinedLibraries.Add((indexEntry.Library, examinedModules));
                }
            }


            Application.Current.Dispatcher.Invoke(() => { _log.Clear(); _log.Add("Started..."); });
            NotifyPropertyChanged(nameof(Log));

            Application.Current.Dispatcher.Invoke(() => _definitionsFound.Clear());
            NotifyPropertyChanged(nameof(FoundDefinitions));

            static Agda.Type ParseType(string expr) 
                => Agda.Type.Parse(IndexBuilder.UnifyIntroducedVariableNames(expr));

            string query = StringEncode.UserInput(qparams.Query);


            LookupData lookupData = await LookupData.CreateAsync(qparams, async () => 
            {
                // Get type of query string

                // Libs & modules to search definitions in query in, user specified
                string[] qiLibraries = qparams.ImportedLibraries.Split(", \t\n".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                string[] qiModules = qparams.ImportedModules.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                // optionally also use examined modules & libs
                IEnumerable<string> totalImportedLibraries = qiLibraries;
                IEnumerable<string> totalImportedModules = qiModules;
                if (qparams.IncludeExaminedModules)
                {
                    totalImportedLibraries = totalImportedLibraries.Concat(examinedLibraries.Select(el => el.Item1));
                    totalImportedModules = totalImportedModules.Concat(examinedLibraries.Select(el => el.Item2).Concat());
                }

                var tmplib = new TempLibrary(totalImportedLibraries);
                var tmpMain = tmplib.CreateModule("Main", sw =>
                {
                    //////////////{-# OPTIONS --without-K   --guardedness #-}
                    //sw.WriteLine("{-# OPTIONS --sized-types --guardedness #-}");
                    totalImportedModules.ForEach(t => sw.WriteLine($"open import {t}"));

                    sw.WriteLine("library-lookup-query : _");
                    sw.WriteLine($"library-lookup-query = {query}");
                },
                "--sized-types", "--guardedness");

                var tmpShield = tmplib.CreateModule("Shield", sw =>
                {
                    sw.WriteLine($"import {tmpMain.FullName}");
                },
                "--sized-types", "--guardedness");

                await agda.SendRequestAsync(new LoadRequest(tmpMain.Path));

                string? agdaNormalizationError = null;
                var queryType = (await agda.SendRequestAsync(new ComputeNormalFormGlobalRequest(
                                                 new()
                                                 {
                                                     FilePath = tmpShield.Path,
                                                     HighlightingMethod = HighlightingMethod.Direct
                                                 },
                                                 ComputeMode.DefaultCompute,
                                                 $"{tmpMain.FullName}.library-lookup-query"
                )))
                .Bimap(
                    err => agdaNormalizationError = err, 
                    normf => 
                    {
                        Application.Current.Dispatcher.Invoke(() => _log.Add("Agda inferred type..."));
                        return ParseType(normf); 
                    })
                .GetRight();

                tmplib.Dispose();

                Application.Current.Dispatcher.Invoke(() => 
                    _log.Add((bool)queryType ? "Normalized type..."
                                             : $"Agda error: \"{agdaNormalizationError}\".")
                );
                NotifyPropertyChanged(nameof(Log));

                return queryType;
            }, 
            () => Task.CompletedTask.ContinueWith(_ => query.Split(' ')));

            if (lookupData.Empty)
            {
                Application.Current.Dispatcher.Invoke(() => _log.Add("Query is fully ignored."));
                NotifyPropertyChanged(nameof(Log));
                IsBusy = false;
                NotifyPropertyChanged(nameof(IsBusy));
                return;
            }

            void examineDefinition(Definition d, string module, string library)
            {
                if(d.Examine(lookupData))
                {
                    Application.Current.Dispatcher.Invoke(() => _definitionsFound.Add(new() 
                    { 
                            Definition = d,
                            Module = module,
                            Library = library
                    }));
                }
            }

            foreach(var exlib in examinedLibraries) {
                string lib = exlib.Item1;
                List<string> examinedModules = exlib.Item2;
                string libIndexDir = Path.Combine("index", lib);

                Dictionary<string, List<Definition>> moduleDefLists;
                if (!_loadedDefinitions.TryGetValue(lib, out moduleDefLists))
                {
                    _loadedDefinitions.Add(lib, moduleDefLists = new());
                }

                foreach(var module in examinedModules)
                {
                    List<Definition> moduleDefs = null;
                    if (!moduleDefLists.TryGetValue(module, out moduleDefs)) 
                    {
                        moduleDefLists.Add(module, moduleDefs = new List<Definition>());

                        using var br = new BinaryReader(File.OpenRead(Path.Combine(libIndexDir, module.Replace('.', '/') + ".ali")));
                        Definition.GetDeserializer(br)
                                  .DeserializeManyPrefixed()
                                  .ForEach(def => 
                                  { 
                                      moduleDefs.Add(def); 
                                      examineDefinition(def, module, lib); 
                                  });
                    }
                    else
                    {
                        foreach(var def in moduleDefs)
                            examineDefinition(def, module, lib);
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() => { 
                _log.Add("Finished.");
                NotifyPropertyChanged(nameof(Log));
                NotifyPropertyChanged(nameof(FoundDefinitions));
            });
            IsBusy = false;
            NotifyPropertyChanged(nameof(IsBusy));
        }

        public void UpdateIndex()
        {
            IsBusy = true;
            NotifyPropertyChanged(nameof(IsBusy));

            _indexedLibraries.Clear();
            NotifyPropertyChanged(nameof(IndexedLibraries));

            if(!Directory.Exists("index"))
            {
                Directory.CreateDirectory("index");
                IsBusy = false;
                NotifyPropertyChanged(nameof(IsBusy));
                return;
            }

            void indexDir(string topDir, ModuleTreeBranch? sink)
            {
                string topDirName = new DirectoryInfo(topDir).Name;
                foreach (var dir in Directory.EnumerateDirectories(topDir))
                {
                    string dirName = new DirectoryInfo(dir).Name;
                    ModuleTreeBranch innerModuleTree = new(sink is null ? String.Empty : dirName, new()) { Parent = sink };
                    if(sink is null)
                    {
                        _indexedLibraries.Add(new(library: dirName, moduleTreeRoot: innerModuleTree));
                    }
                    else
                    { 
                        sink.Nodes.Add(innerModuleTree);
                    }
                    indexDir(dir, innerModuleTree);
                }
                if(sink is null) return;

                foreach(var file in Directory.EnumerateFiles(topDir))
                {
                    sink.Nodes.Add(new ModuleTreeLeaf(Path.GetFileNameWithoutExtension(file)){ Parent = sink });
                }
            }

            indexDir("index", null);

            NotifyPropertyChanged(nameof(IndexedLibraries));

            IsBusy = false;
            NotifyPropertyChanged(nameof(IsBusy));
        }


        private readonly ObservableCollection<IndexEntry> _definitionsFound = new();
        private readonly ObservableCollection<string> _log = new();

        private readonly Dictionary<string, Dictionary<string, List<Definition>>> _loadedDefinitions = new();

        private readonly ObservableCollection<LibraryIndex> _indexedLibraries = new();

        private void NotifyPropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    }
}

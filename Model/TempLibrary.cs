using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AgdaLibraryLookup.Model
{
    public sealed class TempLibrary : IDisposable
    {
        private const string _directory = "temp";
        private const string _baseName = "library-lookup-temp";
        private const string _basePath = _directory + "/" + _baseName;
        private const string _topModuleBaseName = "LibraryLookupTemp";

        public TempLibrary(IEnumerable<string> dependencies)
        {
            string suffix = CreateSuffix();
            _actualPath = _basePath + suffix + "/";
            _actualTopModule = _topModuleBaseName + suffix;
            _actualModulesDirectory = _actualPath + _actualTopModule + "/";

            // create dirs for lib and modules
            Directory.CreateDirectory(_actualModulesDirectory);

            // convert dependencies enum to single string
            StringBuilder depends = new StringBuilder();
            foreach (var lib in dependencies)
            {
                if (depends.Length > 0) depends.Append(", ");
                depends.Append(lib);
            }

            // create description file
            string actualName = _baseName + suffix;
            string descriptionFilePath = _actualPath + actualName + ".agda-lib";
            using (var sw = new StreamWriter(descriptionFilePath, false))
            {
                sw.Write($"name: {actualName}\ninclude: .");
                if (depends.Length > 0) 
                { 
                    sw.Write($"\ndepend: {depends}");
                }
            }
        }

        public TempLibraryModule CreateModule(string moduleName, Action<StreamWriter> writeContents, params string[] options)
        {
            string modulePath = _actualModulesDirectory + moduleName + ".agda";
            string fullModuleName = $"{_actualTopModule}.{moduleName}";
            using var sw = new StreamWriter(modulePath, false);
            if(options.Length > 0)
            {
                sw.Write("{-# OPTIONS ");
                foreach(var opt in options) { sw.Write(opt); sw.Write(' '); }
                sw.Write("#-}\n");
            }
            sw.Write($"module {fullModuleName} where\n");
            writeContents(sw);
            return new TempLibraryModule()
            {
                Path = modulePath,
                FullName = fullModuleName
            };
        }

        public void Dispose()
        {
            if(!_disposed && Directory.Exists(_actualPath)) 
            { 
                new DirectoryInfo(_actualPath).Delete(true);
            }
            _disposed = true;
        }

        
        private readonly string _actualPath;
        private readonly string _actualTopModule;
        private readonly string _actualModulesDirectory;
        private bool _disposed = false;

        private static string CreateSuffix()
        {
            int index = 0;
            string suffix = index.ToString();
            while(Directory.Exists(_basePath + suffix)) 
            {
                ++index;
                suffix = index.ToString();
            }
            return suffix;
        }
    }

    public struct TempLibraryModule
    {
        public string FullName { get; init; }
        public string Path           { get; init; }
    }
}

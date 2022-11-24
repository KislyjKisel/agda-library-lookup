namespace AgdaLibraryLookup.Model
{
    public class QueryParams
    {
        public bool StrictTypes { get; set; } = true;
        public bool SearchTypes { get; set; } = true;
        public bool StrictNames { get; set; } = true;
        public bool SearchNames { get; set; } = true;

        public string Query                  { get; set; } = string.Empty;
        public string ImportedLibraries      { get; set; } = string.Empty;
        public string ImportedModules        { get; set; } = string.Empty;
        public bool   IncludeExaminedModules { get; set; } = true;
    }
    // todo: allow default query imported libs/modules to be specified in some config file || multiple presets
}

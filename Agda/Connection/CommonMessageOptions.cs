namespace AgdaLibraryLookup.Agda.Connection
{
    public struct CommonMessageOptions
    {
        public HighlightingMethod HighlightingMethod { get; set; }

        public string FilePath           
        { 
            get => _filePath; 
            set => _filePath = StringEncode.Path(value); 
        }

        private string _filePath;
    }
}

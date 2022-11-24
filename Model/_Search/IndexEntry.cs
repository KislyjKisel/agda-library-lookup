namespace AgdaLibraryLookup.Model
{
    public struct IndexEntry
    {
        public string     Module     { get; init; }
        public string     Library    { get; init; }
        public Definition Definition { get; init; }

        public string InnerPath => Definition.InnerPath.Length == 0 ? string.Empty : Definition.InnerPath[..^1];
        public string Title     => Definition.Title;
        public string Kind      => Definition.Kind;
    }
}

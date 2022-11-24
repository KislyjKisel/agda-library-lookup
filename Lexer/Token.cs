namespace AgdaLibraryLookup.Lexer
{
    public record Token(string Tag, string Value, TextRegion Region)
    {
        public override string ToString() => $"{Tag}:{Value}";
    }

    public class MutToken
    {
        public string Tag { get; set; }
        public string Value { get; set; }

        public MutToken(string tag, string val) => (Tag, Value) = (tag, val);

        public override string ToString() => $"{Tag}:{Value}";
    }
}

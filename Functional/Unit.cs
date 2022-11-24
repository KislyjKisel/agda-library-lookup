namespace AgdaLibraryLookup
{
    public class Unit
    {
        public static readonly Unit Value = new();

        public override string ToString() => "()";

        private Unit() { }
    }
}

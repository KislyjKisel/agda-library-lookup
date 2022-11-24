using System.Globalization;

namespace AgdaLibraryLookup.Agda.Connection
{
    public static class StringEncode
    {
        public static string UserInput(string inp)
            => inp.Trim().Replace('\n', ' ').Replace('\t', ' ');

        public static string Path(string winPath) 
            => string.IsNullOrWhiteSpace(winPath) ? "" :
                System.IO.Path.GetFullPath( 
                    StringInfo.GetNextTextElement(winPath).Equals("\u202A") ? winPath[1..] 
                                                                            : winPath)
               .Replace('\\', '/');
    }
}

using System;
using System.Collections.Generic;

namespace AgdaLibraryLookup.Collections
{
    public static class ReadonlyListExtensions
    {
        public static IReadOnlyList<T> Range<T>(this IReadOnlyList<T> list, Range range) 
            => new ListView<T>(list, list.ConvertIndex(range.Start), list.ConvertIndex(range.End));

        public static int ConvertIndex<T>(this IReadOnlyList<T> list, Index index) 
            => index.IsFromEnd ? list.Count - index.Value : index.Value;
    }
}

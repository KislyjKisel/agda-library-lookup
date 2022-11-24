using System;

namespace AgdaLibraryLookup.Collections
{
    public sealed class PredicateTraverseConstrainer<T> : ITraverseConstrainer<T>
    {
        public PredicateTraverseConstrainer(Predicate<T> predicate) => _predicate = predicate;

        public bool Examine(T value) => _predicate(value);


        private readonly Predicate<T> _predicate;
    }
}

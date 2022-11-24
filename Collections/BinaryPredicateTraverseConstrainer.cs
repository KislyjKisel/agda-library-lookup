using System;

namespace AgdaLibraryLookup.Collections
{
    public sealed class BinaryPredicateTraverseConstrainer<T> : ITraverseConstrainer<T>
    {
        public BinaryPredicateTraverseConstrainer(Func<T?, T, bool> predicate, T? initial = default(T?)) 
            => (_prev, _predicate) = (initial, predicate);

        public bool Examine(T value) {
            T? prev = _prev;
            _prev = value;
            return _predicate(prev, value);
        }

        private readonly Func<T?, T, bool> _predicate;
        private T? _prev;
    }
}

using System;

namespace AgdaLibraryLookup.Collections
{
    public sealed class NestingTraverseConstrainer<T> : ITraverseConstrainer<T>
    {
        public NestingTraverseConstrainer(Predicate<T> nests, Predicate<T> unnests, int initialNestingLevel = 0)
            => (_nests, _unnests, _nestingLevel) = (nests, unnests, initialNestingLevel);

        public bool Examine(T val)
        {
            if (_nests  (val)) ++_nestingLevel;
            if (_unnests(val)) --_nestingLevel;
            return _nestingLevel == 0;
        }

        public void SetNestingLevel(int nestingLevel) => _nestingLevel = nestingLevel;


        private readonly Predicate<T> _nests;
        private readonly Predicate<T> _unnests;
        private int _nestingLevel;
    }
}

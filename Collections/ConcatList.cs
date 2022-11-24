using System.Collections;
using System.Collections.Generic;

namespace AgdaLibraryLookup.Collections
{
    public struct ConcatList<T> : IReadOnlyList<T>
    {
        public ConcatList(IReadOnlyList<T> first, IReadOnlyList<T> second) 
            => (_first, _second) = (first, second);

        public T this[int i] => i < _first.Count ? _first[i] : _second[i - _first.Count];

        public int Count => _first.Count + _second.Count;

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _first.Count; ++i) yield return _first[i];
            for (int i = 0; i < _second.Count; ++i) yield return _second[i];
        }

        private IReadOnlyList<T> _first, _second;
    }

}

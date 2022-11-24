using System.Collections;
using System.Collections.Generic;

namespace AgdaLibraryLookup.Collections
{
    public sealed class ReadOnlyList<T> : IReadOnlyList<T>
    {
        public ReadOnlyList(IList<T> list) => _list = list;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        public int Count => _list.Count;

        public T this[int index] => _list[index];

        private readonly IList<T> _list;
    }
}

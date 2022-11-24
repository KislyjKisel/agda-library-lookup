using System;
using System.Collections;
using System.Collections.Generic;

namespace AgdaLibraryLookup.Collections
{
    public struct SingletonList<T> : IReadOnlyList<T>
    {
        public SingletonList(T value) => _value = value;

        public T this[int i] 
        {
            get 
            { 
                if(i != 0) throw new IndexOutOfRangeException();
                return _value;
            }
        }

        public int Count => 1;

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public IEnumerator<T> GetEnumerator()
        {
            yield return _value;
        }

        private readonly T _value;
    }
}

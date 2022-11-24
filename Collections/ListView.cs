using System;
using System.Collections;
using System.Collections.Generic;

namespace AgdaLibraryLookup.Collections
{
    public struct ListView<T> : IReadOnlyList<T>
    {
        public ListView(IReadOnlyList<T> values, int start = 0, int end = -1) 
            => (_values, _start, _end) = (values, start, end == -1 ? values.Count : end);

        public ListView<T> SliceLeft(int left = 1)
            => new(_values, _start + left, _end);

        public ListView<T> SliceRight(int right = 1)
            => new(_values, _start, _end - right);

        public ListView<T> Slice(int left, int right)
            => new(_values, _start + left, _end - right);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public IEnumerator<T> GetEnumerator()
        {
            for(int i = _start; i < _end; ++i) 
                yield return _values[i];
        }
        
        public int Count => _end - _start;
        
        public T this[int offset] => _values[_start + offset];
        
        public T this[Index index] => _values[this.ConvertIndex(index)];

        public ListView<T> this[Range range] 
            => new ListView<T>(_values, this.ConvertIndex(range.Start), this.ConvertIndex(range.End));

        private readonly IReadOnlyList<T> _values;
        private readonly int _start;
        private readonly int _end;
    }
}

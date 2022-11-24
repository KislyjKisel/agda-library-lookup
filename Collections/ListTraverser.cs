using AgdaLibraryLookup.Functional;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static AgdaLibraryLookup.Maybe<AgdaLibraryLookup.Unit>;

namespace AgdaLibraryLookup.Collections
{
    public sealed class ListTraverser<T> : IEnumerable<T>
    {
        public ListTraverser(IReadOnlyList<T> values) 
            => (_values, _index) = (values, 0);

        public T    Get (int offset = 0) => _values[_index   + offset];
        public T    Read(int offset = 0) => _values[_index++ + offset];
        public void Move(int offset = 1) => _index += offset;

        public Maybe<T> GetM(int offset)
            => (_index + offset).Subst(i => IsValidIndex(i) ? Just(_values[i]) : Nothing<T>());

        public int IndexAt(int offset = 0) => _index + offset;

        public bool EndOfList() => _index >= _values.Count;
        public bool IsValidOffset(int offset) => IsValidIndex(_index);

        public ListView<T> View() => new(_values, _index);

        public T this[int offset] => Get(offset);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        public IEnumerator<T> GetEnumerator() => GetEnumerator(0);
        public IEnumerator<T> GetEnumerator(int offsetFrom)
        {
            for (int i = _index + offsetFrom; i < _values.Count; ++i)
                yield return _values[i];
        }

        public IEnumerable<ListView<T>> Split(ITraverseConstrainer<T> when, int offsetFrom = 0, bool ignoreDelim = true)
        {
            int ignoreStep = ignoreDelim ? 1 : 0;
            int start = _index + offsetFrom;
            for(int i = start; i < _values.Count; ++i)
            {
                if(when.Examine(_values[i]))
                { 
                    yield return new(_values, start, i);
                    start = i + ignoreStep;
                }
            }
            if(start < _values.Count) 
                yield return new(_values, start);
        } 

        public Maybe<T> Find(ITraverseConstrainer<T> constrainer, int offsetFrom = 0) 
            => this.GetEnumerator(offsetFrom).Find(constrainer);

        public Maybe<int> FindOffset(ITraverseConstrainer<T> constrainer, int offsetFrom = 0)
            => this.GetEnumerator(offsetFrom).FindIndex(constrainer);

        public int Count(ITraverseConstrainer<T> constrainer, int offsetFrom = 0)
            => this.GetEnumerator(offsetFrom).Count(constrainer);


        private readonly IReadOnlyList<T> _values;
        private int _index;

        private bool IsValidIndex(int index) => 0 <= index && index < _values.Count;
    }
}

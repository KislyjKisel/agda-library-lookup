using System;

namespace AgdaLibraryLookup.Collections
{
    public sealed class ValueTraverseConstrainer<T> : ITraverseConstrainer<T> where T : IEquatable<T>
    {
        public ValueTraverseConstrainer(T value) => _value = value;

        public bool Examine(T value) => value.Equals(_value);

        private readonly T _value;
    }
}

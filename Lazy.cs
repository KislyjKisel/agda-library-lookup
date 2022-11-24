using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// using static Kursa42vM.Functional;

namespace AgdaLibraryLookup
{
    public struct Lazy<T>
    {
        public Lazy(T @default) 
            => (_constructor, _hasValue, _value) = (null, true, @default);

        public Lazy(Func<T> con) 
            => (_constructor, _hasValue, _value) = (con, false, default(T));

        public static implicit operator T(Lazy<T> lx) => lx.Value;
        public static implicit operator Lazy<T>(T x)  => new(x);
        
        //public T Value => _hasValue ? _value! 
        //                            : (_value = _constructor!())
        //                                .Run(Apply(this, (Lazy<T> l) => l._hasValue = true));

        public T Value
        {
            get
            {
                if(_hasValue) return _value!;
                _hasValue = true;
                return _value = _constructor!();
            }
        }

        private readonly Func<T>? _constructor;
        private bool     _hasValue;
        private T?       _value;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgdaLibraryLookup
{
    public struct Maybe<T>
    {
        public Maybe(bool hasValue, T? value) => (_hasValue, _value) = (hasValue, value);

        public static implicit operator Maybe<T>(T x)
            => new() { _hasValue = true, _value = x };

        public static explicit operator bool(Maybe<T> mx) => mx._hasValue;

        public Either<Unit, T> ToEither() 
            => _hasValue ? Either<Unit, T>.Right(_value!) : Either<Unit, T>.Left(Unit.Value);

        public static Maybe<T> Nothing() 
            => new() { _hasValue = false };

        public static Maybe<U> Nothing<U>()
            => new() { _hasValue = false };

        public static Maybe<T> Just   (T value) => value;
        public static Maybe<U> Just<U>(U value) => value;
        
        public T Default(Lazy<T> @default) => _hasValue ? _value! : @default;

        public Maybe<U> Map<U>(Func<T, U> f) 
            => _hasValue ? f(_value!) : Nothing<U>();

        public async Task<Maybe<U>> MapAsync<U>(Func<T, Task<U>> f)
            => _hasValue ? await f(_value!) : Nothing<U>();

        public Maybe<bool> Map(Predicate<T> p)
            => _hasValue ? p(_value!) : Nothing<bool>();

        public void Map(Action<T> f)
        {
            if(_hasValue) f(_value!);
        }

        public static Maybe<T> Join(Maybe<Maybe<T>> mmx) 
            => mmx.Default(Nothing());

        // maybe = map + default

        public Maybe<U> Bind<U>(Func<T, Maybe<U>> f)
            => Maybe<U>.Join(Map(f));

        public async Task<Maybe<U>> BindAsync<U>(Func<T, Task<Maybe<U>>> f)
           => Maybe<U>.Join(await MapAsync(f));

        public Maybe<U> Seq<U>(Maybe<Func<T, U>> mf)
            => Bind(x => mf.Map(f => f(x)));


        // Enumerables

        public static IEnumerable<T> Concat(IEnumerable<Maybe<T>> mxs)
        {
            foreach(var mx in mxs)
                if(mx._hasValue) 
                    yield return mx._value!;
        }

        public static async IAsyncEnumerable<T> Concat(IAsyncEnumerable<Maybe<T>> mxs)
        {
            await foreach (var mx in mxs)
                if (mx._hasValue)
                    yield return mx._value!;
        }


        private bool _hasValue;
        private T? _value;
    }
}

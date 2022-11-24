using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgdaLibraryLookup
{
    public struct Either<L, R>
    {
        public static Either<L, R> Left(L left) => new(true, left, default(R));
        public static Either<L, R> Right(R right) => new(false, default(L), right);

        public static Either<L1, R1> Left<L1, R1>(L1 left) => new(true, left, default(R1));
        public static Either<L1, R1> Right<L1, R1>(R1 right) => new(false, default(L1), right);

        //public static implicit operator Either<L, R>(L left)  => Left(left);
        public static implicit operator Either<L, R>(R right) => Right(right);

        public Either<L1, R1> Bimap<L1, R1>(Func<L, L1> fl, Func<R, R1> fr)
            => _isLeft ? Left<L1, R1>(fl(_left!)) : Right<L1, R1>(fr(_right!));

        public void Bimap(Action<L> fl, Action<R> fr)
        {
            if(_isLeft) fl(_left!);
            else fr(_right!);
        }

        public T Fold<T>(Func<L, T> left, Func<R, T> right) 
            => _isLeft ? left(_left!) : right(_right!);

        public Maybe<L> GetLeft() => _isLeft ? Maybe<L>.Just(_left!) : Maybe<L>.Nothing();
        public Maybe<R> GetRight() => _isLeft ? Maybe<R>.Nothing() : Maybe<R>.Just(_right!);

        public Either<L1, R> MapLeft<L1>(Func<L, L1> f)
            => _isLeft ? Left<L1, R>(f(_left!)) : Right<L1, R>(_right!);

        public Either<L, R1> MapRight<R1>(Func<R, R1> f)
            => _isLeft ? Left<L, R1>(_left!) : Right<L, R1>(f(_right!));

        public void MapLeft(Action<L> f)
        {
            if(_isLeft) f(_left!);
        }

        public void MapRight(Action<R> f)
        {
            if (!_isLeft) f(_right!);
        }

        public Either<L1, R> BindLeft<L1>(Func<L, Either<L1, R>> f)
            => _isLeft ? f(_left!) : Right<L1, R>(_right!);

        public Either<L, R1> BindRight<R1>(Func<R, Either<L, R1>> f)
            => _isLeft ? Left<L, R1>(_left!) : f(_right!);

        public async Task<Either<L1, R>> AsyncBindLeft<L1>(Func<L, Task<Either<L1, R>>> f) 
            => _isLeft ? await f(_left!) : Right<L1, R>(_right!);

        public async Task<Either<L, R1>> AsyncBindRight<R1>(Func<R, Task<Either<L, R1>>> f)
            => _isLeft ? Left<L, R1>(_left!) : await f(_right!);

        public override string? ToString()
            => _isLeft ? _left!.ToString() : _right!.ToString();


        // Enumerables

        public static IEnumerable<L> ConcatLeft(IEnumerable<Either<L, R>> elrs)
        {
            foreach(var elr in elrs)
                if(elr._isLeft)
                    yield return elr._left!;
        }

        public static IEnumerable<R> ConcatRight(IEnumerable<Either<L, R>> elrs)
        {
            foreach (var elr in elrs)
                if (!elr._isLeft)
                    yield return elr._right!;
        }

        public static async IAsyncEnumerable<L> ConcatLeft(IAsyncEnumerable<Either<L, R>> elrs)
        {
            await foreach (var elr in elrs)
                if (elr._isLeft)
                    yield return elr._left!;
        }

        public static async IAsyncEnumerable<R> ConcatRight(IAsyncEnumerable<Either<L, R>> elrs)
        {
            await foreach (var elr in elrs)
                if (!elr._isLeft)
                    yield return elr._right!;
        }



        private readonly bool _isLeft;
        private readonly L? _left;
        private readonly R? _right;

        private Either(bool isLeft, L? left, R? right)
            => (_isLeft, _left, _right) = (isLeft, left, right);
    }
}

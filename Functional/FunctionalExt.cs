using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AgdaLibraryLookup.Maybe<AgdaLibraryLookup.Functional.Empty>;

namespace AgdaLibraryLookup.Functional
{
    public static class FunctionalExt
    {
        //partial

        public static Action         Apply<T1>        (T1 x, Action<T1>         f) => ()     => f(x);
        public static Action<T2>     Apply<T1, T2>    (T1 x, Action<T1, T2>     f) => (y)    => f(x, y);
        public static Action<T2, T3> Apply<T1, T2, T3>(T1 x, Action<T1, T2, T3> f) => (y, z) => f(x, y, z);

        public static Func<T2, U>         Apply<T1, T2, U>        (T1 x, Func<T1, T2, U>         f) => (y)       => f(x, y);
        public static Func<T2, T3, U>     Apply<T1, T2, T3, U>    (T1 x, Func<T1, T2, T3, U>     f) => (y, z)    => f(x, y, z);
        public static Func<T2, T3, T4, U> Apply<T1, T2, T3, T4, U>(T1 x, Func<T1, T2, T3, T4, U> f) => (y, z, w) => f(x, y, z, w);

        // forwarding

        public static U Subst<T, U>(this T x, Func<T, U> f) => f(x);
        public static T Pipe<T>(this T x, Action<T> f) { f(x); return x; }
        public static T Pipe<T>(this T x, Action f) { f(); return x; }

        // _

        public static void ForEach<T>(this IEnumerable<T> xs, Action<T> f)
        {
            foreach(var x in xs) f(x);
        }

        public static async IAsyncEnumerable<T> Concat<T>(this IEnumerable<IAsyncEnumerable<T>> tss)
        {
            foreach(var ts in tss)
                await foreach(var t in ts)
                    yield return t;
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<IEnumerable<T>> tss)
        {
            foreach(var ts in tss)
                foreach(var t in ts)
                    yield return t;
        }

        public static async Task<IEnumerable<T>> Concat<T>(this IEnumerable<Task<IEnumerable<T>>> tss)
        {
            List<T> r = new List<T>();
            foreach(var ts in tss)
                foreach(var t in await ts)
                    r.Add(t);

            return r;
        }

        public static Maybe<bool> AllOrNone(this IEnumerable<bool> bs)
        {
           bool any = false;
           bool all = true;
           foreach(var b in bs) {
                all &= b;
                any |= b; 
           }
           return all ? true : (any ? Nothing<bool>() : false);
        }

        public static Maybe<bool> AllOrNone<T>(this IEnumerable<T> xs, Predicate<T> p)
        {
            bool any = false;
            bool all = true;
            foreach (var x in xs)
            {
                bool b = p(x);
                all &= b;
                any |= b;
            }
            return all ? true : (any ? Nothing<bool>() : false);
        }

        public static bool? ToNullable(this Maybe<bool> mb) => mb.ToEither().Fold<bool?>(_ => null, x => x);

        public static StringBuilder Concat(this IEnumerable<string> ss)
            => ss.Aggregate(new StringBuilder(), (sb, s) => sb.Append(s));

        //

        public static Action Noop => () => { };

        public static Func<U, T> Const<U, T>(T x) => y => x;

        public static T Id<T>(T x) => x;

        // Maybe

        public static IEnumerable<T> Concat<T>(this IEnumerable<Maybe<T>> mxs)
            => Maybe<T>.Concat(mxs);

        public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<Maybe<T>> mxs)
            => Maybe<T>.Concat(mxs);

        // Either

        public static IEnumerable<L> ConcatLeft<L, R>(this IEnumerable<Either<L, R>> elrs)
            => Either<L, R>.ConcatLeft(elrs);

        public static IEnumerable<R> ConcatRight<L, R>(this IEnumerable<Either<L, R>> elrs)
            => Either<L, R>.ConcatRight(elrs);

        public static IAsyncEnumerable<L> ConcatLeft<L, R>(this IAsyncEnumerable<Either<L, R>> elrs)
            => Either<L, R>.ConcatLeft(elrs);

        public static IAsyncEnumerable<R> ConcatRight<L, R>(this IAsyncEnumerable<Either<L, R>> elrs)
            => Either<L, R>.ConcatRight(elrs);

        public static (IEnumerable<L>, IEnumerable<R>) BiConcat<L, R>(this IEnumerable<Either<L, R>> elrs) 
            => (elrs.ConcatLeft(), elrs.ConcatRight());
    }
}

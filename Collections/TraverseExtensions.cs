using System.Collections.Generic;
using System.Linq;

using static AgdaLibraryLookup.Maybe<AgdaLibraryLookup.Unit>;

namespace AgdaLibraryLookup.Collections
{
    public static class TraverseExtensions
    {
        /// <summary>
        /// Searches for the first element that satisfies all search constraints. Mutates constrainers.
        /// </summary>
        public static Maybe<T> Find<T>(this IEnumerator<T> it, ITraverseConstrainer<T> constrainer)
        {
            while(it.MoveNext())
                if(constrainer.Examine(it.Current)) 
                    return it.Current;

            return Nothing<T>();
        }
        
        public static Maybe<int> FindIndex<T>(this IEnumerator<T> it, ITraverseConstrainer<T> constrainer)
        {
            int index = 0;
            while(it.MoveNext())
            {
                if (constrainer.Examine(it.Current)) 
                    return index;

                ++index;
            }
            return Nothing<int>();
        }

        public static int Count<T>(this IEnumerator<T> it, ITraverseConstrainer<T> constrainer)
        {
            int count = 0;
            while(it.MoveNext() && constrainer.Examine(it.Current))
                ++count;

            return count;
        }

        //values.Any(value => constraints.Aggregate(true, (all, constraint) => all & constraint.Examine(value)));
    }
}

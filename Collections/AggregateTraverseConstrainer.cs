using System;
using System.Collections.Generic;
using System.Linq;

namespace AgdaLibraryLookup.Collections
{
    public sealed class AggregateTraverseConstrainer<T> : ITraverseConstrainer<T>
    {
        public AggregateTraverseConstrainer(IEnumerable<ITraverseConstrainer<T>> constrainers) 
            => _constrainers = constrainers;

        public bool Examine(T value) 
            => _constrainers.Aggregate(true, (all, constrainer) => all & constrainer.Examine(value));

        private readonly IEnumerable<ITraverseConstrainer<T>> _constrainers;
    }
}

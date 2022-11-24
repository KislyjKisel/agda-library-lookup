using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgdaLibraryLookup.Agda
{
    public abstract partial class Type
    {
        private class UnsortedFunction : UnsortedType
        {
            public UnsortedType /*T, IEnumerable<int> Deps)*/[] Types { get; init; }

            public UnsortedFunction(UnsortedType/*, IEnumerable<int> Deps)*/[] types) => Types = types;

            public override int CompareTo(UnsortedType? other)
            {
                if (other is null) return 1;
                if (other is UnsortedFunction otherf)
                {
                    if (otherf.Types.Length != this.Types.Length)
                        return this.Types.Length - otherf.Types.Length;

                    for (int i = 0; i < Types.Length; ++i)
                    {
                        int cmp = this.Types[i].CompareTo(otherf.Types[i]);
                        if (cmp != 0) return cmp;
                    }
                    return 0;
                }
                else return 1; // Func > Expr
            }

            public override Type Sort()
            {
                // partial -> total, only deps
                //var order = new DirectedGraph(Types.Select((t, i) => (i, t.Deps))).TopologicalSort();
                //return new Function(order.Select(i => Types[i].T.Sort()).ToArray());
                // full w/o deps
                Array.Sort(Types);
                return new Function(Types.Select(t => t.Sort()).ToArray());
            }
        }
    }
}

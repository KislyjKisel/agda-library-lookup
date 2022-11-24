using AgdaLibraryLookup.Lexer;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AgdaLibraryLookup.Agda
{
    public abstract partial class Type
    {
        private class UnsortedExpression : UnsortedType
        {
            public UnsortedExpression(IEnumerable<Token> toks)
            {
                _units = toks.Select(t => new ExpressionUnit(t.Tag, t.Value));
                _unitCount = _units.Count();
            }

            public override int CompareTo(UnsortedType? other)
            {
                if (other is null) return 1;
                if (other is not UnsortedExpression othere) return -1; // Expr < Func
                int cmpL = this._unitCount.CompareTo(othere._unitCount);
                if (cmpL != 0) return cmpL;
                var it1 = this._units.GetEnumerator();
                var it2 = othere._units.GetEnumerator();
                while (it1.MoveNext() && it2.MoveNext())
                {
                    int cmpU = it1.Current.Value.CompareTo(it2.Current.Value);
                    if (cmpU != 0) return cmpU;
                }
                return 0;
            }

            public override Type Sort()
                => new Expression(_units.ToList());

            public override string ToString()
                => _units.Aggregate(new StringBuilder(), (sb, x) => sb.Append(x.Value).Append(' ')).ToString().Trim();



            private readonly IEnumerable<ExpressionUnit> _units;
            private readonly int _unitCount;
        }
    }
}

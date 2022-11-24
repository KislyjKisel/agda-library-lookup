using AgdaLibraryLookup.Collections;
using AgdaLibraryLookup.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgdaLibraryLookup.Agda
{
    using ITC = ITraverseConstrainer<Token>;
    using AggregateTC = AggregateTraverseConstrainer<Token>;
    using PredicateTC = PredicateTraverseConstrainer<Token>;
    using BinPredicateTC = BinaryPredicateTraverseConstrainer<Token>;
    using NestingTC = NestingTraverseConstrainer<Token>;

    public abstract partial class Type { 
        private abstract class UnsortedType : IComparable<UnsortedType>
        {
            public abstract Type Sort();
            public abstract int CompareTo(UnsortedType? other);

            public static UnsortedType Parse(IReadOnlyList<Token> tokens)
            {
                while (tokens[tokens.Count - 1].Tag == "curlyR") 
                    tokens = new ListView<Token>(tokens, tokens[0].Tag == "curlyL" ? 1 : 0, tokens.Count - 1);
                
                while (tokens[0].Tag == "parenL" && tokens[tokens.Count - 1].Tag == "parenR")
                    tokens = new ListView<Token>(tokens, 1, tokens.Count - 1);

                for(int i = 1; i < tokens.Count; ++i)
                {
                    if(tokens[i - 1].Tag == "curlyR" && 
                       (tokens[i].Tag == "parenL" || tokens[i].Tag == "curlyL"))
                    {
                        tokens = new ConcatList<Token>(tokens.Range(..i), new ConcatList<Token>(new SingletonList<Token>(new Token("arrow", "->", default(TextRegion))), tokens.Range(i..)));
                    }
                }

                var toktrav = new ListTraverser<Token>(tokens);
                var exprs = toktrav.Split(new AggregateTC(new ITC[]{  
                    new PredicateTC(t => t.Tag == "arrow"),
                    new NestingTC(t => t.Tag == "parenL", t => t.Tag == "parenR")
                })).ToList();

                if(exprs.Count() == 1)
                    return new UnsortedExpression(exprs.First());
                
                return new UnsortedFunction(exprs.Select(er => Parse(er)).ToArray());
            }
        }
    }
}

using AgdaLibraryLookup.Lexer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgdaLibraryLookup.Agda
{
    public abstract partial class Type
    {
        private class Expression : Type
        {
            public IReadOnlyList<ExpressionUnit> Tokens { get; init; }

            public Expression(IReadOnlyList<ExpressionUnit> tokens) => Tokens = tokens;

            public override bool RelatedTo(Type other)
            {
                return other is Function otherF && otherF.Types.Any(RelatedTo) ||
                       other is Expression otherE && PointwiseCompare(otherE, (x, y) => x.Tag == y.Tag && RelatedIdentifier(x.Value, y.Value));
            }

            public override bool SameAs(Type other)
                => other is Expression otherE && PointwiseCompare(otherE, (x, y) => x.Value == y.Value && x.Tag == y.Tag);

            public override bool ContainsName(string name)
                => Tokens.Any(tok => tok.Value == name);

            public override void Serialize(BinaryWriter outp)
            {
                base.Serialize(outp);
                for(int i = 0; i < Tokens.Count; ++i)
                {
                    outp.Write((byte)1);
                    outp.Write(Tokens[i].Tag);
                    outp.Write(Tokens[i].Value);
                }
                outp.Write((byte)0);
            }

            public static new Expression Deserialize(BinaryReader inp)
            {
                List<ExpressionUnit> tokens = new();
                while(inp.ReadByte() == (byte)1)
                {
                    string tag = inp.ReadString();
                    string val = inp.ReadString();
                    tokens.Add(new(tag, val));
                }
                return new(tokens);
            }


            private bool PointwiseCompare(Expression otherE, Func<ExpressionUnit, ExpressionUnit, bool> f)
            {
                return this.Tokens.Zip(otherE.Tokens).All(tp => f(tp.First, tp.Second));
            } 

            private static bool RelatedIdentifier(string a, string b)
            {
                int ai = a.Length - 1, bi = b.Length - 1;
                while(ai >= 0 || bi >= 0)
                {
                    if(ai < 0) return b[bi] == '.';
                    if(bi < 0) return a[ai] == '.';
                    if(a[ai] != b[bi]) return ai < a.Length - 1 ? a[ai + 1] == '.' : false;
                    --ai;
                    --bi;
                }
                return true;
            }
        }
    }
}

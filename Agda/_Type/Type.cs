using AgdaLibraryLookup.Lexer;
using System;
using System.Collections.Generic;
using System.IO;

namespace AgdaLibraryLookup.Agda
{
    [Serializable]
    public abstract partial class Type
    {
        private sealed record ExpressionUnit(string Tag, string Value);

        //IReadOnlyList<(Token T, Maybe<string> Intro, Maybe<string> Dep)>

        public static Type Parse(IReadOnlyList<Token> src)
            => UnsortedType.Parse(src).Sort();

        public abstract bool RelatedTo(Type other);
        public abstract bool SameAs(Type other);
        public abstract bool ContainsName(string name);

        public virtual void Serialize(BinaryWriter outp)
            => outp.Write(this is Function);

        public static Type Deserialize(BinaryReader inp)
            => inp.ReadByte() == 0 ? Expression.Deserialize(inp) : Function.Deserialize(inp);


        private Type() { }
    }
}

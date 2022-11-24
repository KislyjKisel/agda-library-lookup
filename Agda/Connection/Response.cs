using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using static AgdaLibraryLookup.Either<string, AgdaLibraryLookup.Agda.Connection.Response>;

namespace AgdaLibraryLookup.Agda.Connection
{
    public sealed record Response(string Tag, List<string> Arguments) 
    {
        public delegate Maybe<T> Filter<T>(Response response);

        public override string ToString()
            => Arguments.Aggregate(new StringBuilder(Tag), (sb, x) => sb.Append(' ').Append(x))
                        .ToString();

        public static Either<string, Response> Deserialize(string src)
        {
            const string Prefix = "Agda2> ";
            var it = StringInfo.GetTextElementEnumerator(src);
            if (!it.MoveNext()) return Left(src);
            if (it.GetTextElement() == Prefix[0].ToString())
            {
                for (int i = 1; i < Prefix.Length; ++i)
                {
                    if (!it.MoveNext() || it.GetTextElement() != Prefix[i].ToString()) return Left(src);
                }
                if (!it.MoveNext()) return Left("");
            }
            if (it.GetTextElement() != "(") return Left(src);
            var sb = new StringBuilder();
            bool tryRead(out string? res)
            {
                res = null;
                if (it.GetTextElement() == ")") return false; //todo: inner parens: "(tag: ())" now parsed as "(tag: ()"
                bool quote = false;
                while (it.MoveNext() && (quote || it.GetTextElement() != " " && it.GetTextElement() != ")"))
                {
                    if (it.GetTextElement() == "\"")
                    {
                        if (quote)
                        {
                            it.MoveNext();
                            break;
                        }
                        quote = true;
                    }
                    else sb.Append(it.GetTextElement());
                }
                res = sb.ToString();
                sb.Clear();
                return res.Length > 0 || quote;

            }
            if (!tryRead(out string? tag)) return Left(src);
            var args = new List<string>();
            while (true)
            {
                if (!tryRead(out string? arg)) break;
                args.Add(arg!);
            }

            return new Response(tag!, args);
        }
    }
}

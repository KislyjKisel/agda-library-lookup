using System;
using System.Threading.Tasks;

namespace AgdaLibraryLookup.Model
{
    public readonly record struct LookupData(
        Maybe<Agda.Type> Type,
        Maybe<string[]> Names,
        bool StrictTypes = true,
        bool StrictNames = true
    ) {
        public static LookupData Create(QueryParams @params, Func<Maybe<Agda.Type>> genType, Func<string[]> genNames)
        {
            var dt = new Maybe<Unit>(@params.SearchTypes, Unit.Value).Bind(_ => genType());
            var dn = new Maybe<Unit>(@params.SearchNames, Unit.Value).Map(_ => genNames());
            return new(Type: dt, Names: dn, @params.StrictTypes, @params.StrictNames);
        }

        public static async Task<LookupData> CreateAsync(QueryParams @params, Func<Task<Maybe<Agda.Type>>> genType, Func<Task<string[]>> genNames)
        {
            var dt = await new Maybe<Unit>(@params.SearchTypes, Unit.Value).BindAsync(_ => genType());
            var dn = await new Maybe<Unit>(@params.SearchNames, Unit.Value).MapAsync(_ => genNames());
            return new(Type: dt, Names: dn, @params.StrictTypes, @params.StrictNames);
        }

        public bool Empty => !((bool)Type || (bool)Names);
    }
}

using System.Linq;
using System.IO;

namespace AgdaLibraryLookup.Agda
{
    public abstract partial class Type
    { 
        private class Function : Type //[a, b, c] is (a) -> (b) -> (c)
        {
            public Type[] Types { get; init; }

            public Function(Type[] types) => Types = types;

            public override bool SameAs(Type other)
            {
                if(other is Expression) return false;
                Function otherF = (other as Function)!;
                if(otherF.Types.Length != this.Types.Length) 
                    return false;
                for(int i = 0; i < Types.Length; ++i)
                    if(!this.Types[i].SameAs(otherF.Types[i])) 
                        return false;
                return true;
            }

            public override bool RelatedTo(Type other)
            {
                if(other is Expression otherE)
                {
                    for(int i = 0; i < Types.Length; ++i) 
                        if(Types[i].RelatedTo(other)) return true;

                    return false;
                }
                
                var otherF = other as Function;
                
                Function f, F;
                if(otherF.Types.Length > this.Types.Length)
                {
                    (f, F) = (this, otherF);
                }
                else
                {
                    (f, F) = (otherF, this);
                }

                int i_F = 0;
                for(int i_f = 0; i_f < f.Types.Length; ++i_f)
                {
                    while(i_F < F.Types.Length && !f.Types[i_f].RelatedTo(F.Types[i_F])) ++i_F;
                    if(i_F == F.Types.Length) return false;
                }
                return true;
            }

            public override bool ContainsName(string name)
                => Types.Any(t => t.ContainsName(name));

            public override void Serialize(BinaryWriter outp)
            {
                base.Serialize(outp);
                outp.Write((ushort)Types.Length);
                foreach(var t in Types) t.Serialize(outp);
            }

            public static new Function Deserialize(BinaryReader inp)
            {
                int typeCount = inp.ReadUInt16();
                Type[] types = new Type[typeCount];
                for(int i = 0; i < typeCount; ++i)
                {
                    types[i] = Type.Deserialize(inp);
                }
                return new Function(types);
            }
        }
    }
}

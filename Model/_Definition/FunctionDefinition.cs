using AgdaLibraryLookup.Serialization;
using System;
using System.Linq;
using System.IO;

namespace AgdaLibraryLookup.Model
{
    using AType = Agda.Type;

    public enum FunctionLikeKind : byte
    {
        Function        = 0,
        Pattern         = 1,
        DataType        = 2,
        DataConstructor = 3,
        RecordType      = 4,
        RecordField     = 5
    }

    public sealed class FunctionDefinition : Definition, IBinarySerializable<FunctionDefinition>
    {
        public string           Type             { get; init; }
        public AType            TypeNormalized   { get; init; }
        public FunctionLikeKind FunctionLikeKind { get; init; }

        public FunctionDefinition(string name, string innerPath, string type, AType typeNormalized, FunctionLikeKind kind)
            : base(name, innerPath)
            => (Type, TypeNormalized, FunctionLikeKind) = (type, typeNormalized, kind);


        #region IBinarySerializable

        public void Serialize(BinaryWriter outp)
        {
            PreSerialize(outp);
            outp.Write(Type);
            TypeNormalized.Serialize(outp);
            outp.Write((byte)FunctionLikeKind);
        }

        public static FunctionDefinition Deserialize(BinaryReader inp)
        {
            var (name, innerPath) = PreDeserialize(inp);
            string type = inp.ReadString();
            var typeNorm = Agda.Type.Deserialize(inp);
            var kind = (FunctionLikeKind)(inp.ReadByte());
            return new FunctionDefinition(name, innerPath, type, typeNorm, kind);
        }

        #endregion

        #region Definition

        public override string Title => $"{Name} {Type}";
        public override string Kind => FunctionLikeKind.ToString();

        public override bool Examine(LookupData data)
        {
            bool fits = data.Names.Map((Predicate<string[]>) (ns => ns.Contains(this.Name))).Default(false); 
            if(!fits && !data.StrictNames)
            {
                fits = this.TypeNormalized.ContainsName(this.Name);
            }       

            return fits || 
                data.Type.Map(data.StrictTypes ? TypeNormalized.SameAs : TypeNormalized.RelatedTo)
                         .Default(false);
        }

        #endregion
    }
}

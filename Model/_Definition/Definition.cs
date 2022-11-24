using AgdaLibraryLookup.Serialization;
using System.IO;

namespace AgdaLibraryLookup.Model
{
    public abstract class Definition
    {
        public string Name      { get; init; }
        public string InnerPath { get; init; }

        public Definition(string name, string innerModulePath)
            => (Name, InnerPath) = (name, innerModulePath);

        #region Serialization

        protected void PreSerialize(BinaryWriter bw)
        {
            bw.Write(Name);
            bw.Write(InnerPath);
        }

        protected static (string Name, string InnerModule) PreDeserialize(BinaryReader br)
            => (br.ReadString(), br.ReadString());

        private static readonly Lazy<VariantBinarySerialization<Definition>> _vsi 
            = new Lazy<VariantBinarySerialization<Definition>>(() => 
            {
                var b = new VariantBinarySerialization<Definition>.VariantSerializationInfoBuilder();
                b.AddVariant<FunctionDefinition>();
                b.AddVariant<RecordConstructorDefinition>();
                return b.BuildReset();
            });

        public static VariantBinarySerializer<Definition> GetSerializer(BinaryWriter bw)
            => _vsi.Value.GetSerializer(bw);

        public static VariantBinaryDeserializer<Definition> GetDeserializer(BinaryReader br)
            => _vsi.Value.GetDeserializer(br);

        #endregion

        public abstract string Title    { get; }
        public abstract string Kind     { get; }

        public abstract bool Examine(LookupData data);

        public sealed override string ToString() => Title;
    }
}

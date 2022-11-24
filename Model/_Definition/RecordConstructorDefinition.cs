using AgdaLibraryLookup.Serialization;
using System;
using System.IO;
using System.Linq;

namespace AgdaLibraryLookup.Model
{
    using AType = Agda.Type;

    // todo: record constructor type

    public class RecordConstructorDefinition : Definition, IBinarySerializable<RecordConstructorDefinition>
    {
        public RecordConstructorDefinition(string name, string innerPath)
            : base(name, innerPath) { }

        #region IBinarySerializable

        public void Serialize(BinaryWriter outp) => PreSerialize(outp);

        public static RecordConstructorDefinition Deserialize(BinaryReader inp)
        {
            var (name, innerPath) = PreDeserialize(inp);
            return new RecordConstructorDefinition(name, innerPath);
        }

        #endregion

        #region Definition

        public override string Title => $"constructor {Name}";
        public override string Kind => "RecordConstructor";

        public override bool Examine(LookupData data)
            => data.Names.Map((Predicate<string[]>)(ns => ns.Contains(Name)))
                         .Default(false);

        #endregion
    }
}

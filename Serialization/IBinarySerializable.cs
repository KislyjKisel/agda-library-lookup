using System.IO;

namespace AgdaLibraryLookup.Serialization
{
    public interface IBinarySerializable<T> where T : IBinarySerializable<T>
    {
        void Serialize(BinaryWriter binaryWriter);
        static abstract T Deserialize(BinaryReader binaryReader);
    }
}

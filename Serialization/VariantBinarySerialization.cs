using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AgdaLibraryLookup.Functional;

namespace AgdaLibraryLookup.Serialization
{
    public sealed class VariantBinarySerialization<TBase> where TBase : class
    {
        public delegate TBase Deserializator(BinaryReader binaryReader);

        private readonly List<(Type T, Deserializator Deserialize, Lazy<MethodInfo> Serialize)> _variants;

        private VariantBinarySerialization(List<(Type, Deserializator, Lazy<MethodInfo>)> variants) 
            => _variants = variants;

        public byte GetTypeIndex(Type type)
        {
            for (int i = 0; ; ++i)
            {
                if (type.Equals(_variants[i].T))
                {
                    return checked((byte)i);
                }
            }
        }

        public MethodInfo GetSerializationMethod(byte index)
            => _variants[index].Serialize;

        public Deserializator GetDeserializationFunction(byte index)
            => _variants[index].Deserialize;


        public VariantBinarySerializer<TBase> GetSerializer(BinaryWriter bw)
            => new VariantBinarySerializer<TBase>(this, bw);

        public VariantBinaryDeserializer<TBase> GetDeserializer(BinaryReader br)
            => new VariantBinaryDeserializer<TBase>(this, br);


        public sealed class VariantSerializationInfoBuilder 
        {
            private List<(Type T, Deserializator Deserialize, Lazy<MethodInfo> Serialize)> _variants = new();

            public void RemoveVariant<T>() where T : TBase, IBinarySerializable<T>
            {
                Type type = typeof(T);
                for(int i = 0; i < _variants.Count; ++i)
                {
                    if(type.Equals(_variants[i]))
                    {
                        _variants.RemoveAt(i);
                    }
                }
            }

            public void AddVariant<T>() where T : TBase, IBinarySerializable<T>
            {
                _variants.Add((typeof(T), br => T.Deserialize(br), new(() => GetSerializationMethod(typeof(T)))));
            }

            public VariantBinarySerialization<TBase> BuildReset()
            {
                var vsi = new VariantBinarySerialization<TBase>(_variants);
                _variants = new();
                return vsi;
            }

            public VariantBinarySerialization<TBase> Build()
            {
                var vsi = BuildReset();
                foreach(var v in vsi._variants)
                    _variants.Add(v);

                return vsi;
            }

            private static MethodInfo GetSerializationMethod(Type type)
            {
                Type gibs = typeof(IBinarySerializable<>);
                Type ibs = gibs.MakeGenericType(type);
                InterfaceMapping mapping = type.GetInterfaceMap(ibs);
                return mapping.TargetMethods.First(mi => mi.Name == "Serialize");
            }
        }
    }

    public sealed class VariantBinarySerializer<TBase> where TBase : class
    {
        public VariantBinarySerializer(VariantBinarySerialization<TBase> vsi, BinaryWriter binaryWriter)
            => (_vsi, _binaryWriter) = (vsi, binaryWriter);

        public void Serialize<T>(T value) where T : IBinarySerializable<T>, TBase
        { 
            _binaryWriter.Write(_vsi.GetTypeIndex(typeof(T)));
            value.Serialize(_binaryWriter);
        }

        private void Serialize(object value, object[] args)
        {
            byte ti = _vsi.GetTypeIndex(value.GetType());
            _binaryWriter.Write(ti);
            _vsi.GetSerializationMethod(ti).Invoke(value, args);
        }

        public void Serialize(object value)
            => Serialize(value, new object[] { _binaryWriter });

        public void SerializeManyPrefixed(IEnumerable<TBase> values)
        {
            var args = new object[] { _binaryWriter };
            foreach (var value in values)
            {
                _binaryWriter.Write((byte)1);
                Serialize(value, args);
            }
            _binaryWriter.Write((byte)0);
        }

        private readonly VariantBinarySerialization<TBase> _vsi;
        private readonly BinaryWriter _binaryWriter;
    }

    public sealed class VariantBinaryDeserializer<TBase> where TBase : class
    {
        public VariantBinaryDeserializer(VariantBinarySerialization<TBase> vsi, BinaryReader binaryReader) 
            => (_vsi, _binaryReader) = (vsi, binaryReader);

        public TBase Deserialize()
            => _vsi.GetDeserializationFunction(_binaryReader.ReadByte())(_binaryReader);

        public IEnumerable<TBase> DeserializeManyPrefixed()
        {
            while(_binaryReader.ReadByte() == (byte)1)
            {
                yield return Deserialize();
            }
        }

        private readonly VariantBinarySerialization<TBase> _vsi;
        private readonly BinaryReader _binaryReader;
    }
}

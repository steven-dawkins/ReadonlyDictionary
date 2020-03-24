namespace ReadonlyDictionary.Serialization
{
    using System;
    using System.IO;
    using ProtoBuf;

    public class ProtobufSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T value)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, value);

                return ms.ToArray();
            }
        }

        public T Deserialize(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return ProtoBuf.Serializer.Deserialize<T>(ms);
            }
        }

        public object GetState()
        {
            return null;
        }
    }
}

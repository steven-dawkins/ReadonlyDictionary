using NetSerializer;
using System.IO;
using System;

namespace ReadOnlyDictionary.Serialization
{
    public class NetSerializer<T> : ISerializer<T>
    {
        private Serializer ser;

        public NetSerializer()
        {
            this.ser = new Serializer(new [] { typeof(T) });
        }

        public byte[] Serialize(T value)
        {
            using (var ms = new MemoryStream())
            {
                ser.SerializeDirect(ms, value);

                return ms.ToArray();
            }
        }

        public T Deserialize(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                T value;
                ser.DeserializeDirect<T>(ms, out value);

                return value;
            }
        }

        public object GetState()
        {
            return null;
        }
    }
}

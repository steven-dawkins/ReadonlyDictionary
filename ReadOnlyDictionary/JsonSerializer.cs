using Newtonsoft.Json;
using ProtoBuf;
using System.IO;
using System.Text;

namespace ReadOnlyDictionary
{
    public interface ISerializer<T>
    {
        byte[] Serialize(T value);
        T Deserialize(byte[] bytes);
    }

    public class JsonSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        }

        public T Deserialize(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }
    }

    public class ProtobufSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T value)
        {
            using(var ms = new MemoryStream())
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
    }
}

using Newtonsoft.Json;
using ProtoBuf;
using System.IO;
using System.Text;
using System;

namespace ReadOnlyDictionary.Serialization
{

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

        public object GetState()
        {
            return null;
        }
    }

    
}

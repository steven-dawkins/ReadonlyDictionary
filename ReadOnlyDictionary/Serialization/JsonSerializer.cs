using Newtonsoft.Json;
using System.Text;

namespace ReadonlyDictionary.Serialization
{

    public class JsonSerializer<T> : ISerializer<T>
    {
        private readonly JsonSerializerSettings settings;

        public JsonSerializer(JsonSerializerSettings settings = null)
        {
            this.settings = settings ?? new JsonSerializerSettings();
        }

        public byte[] Serialize(T value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, settings));
        }

        public T Deserialize(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), settings);
        }

        public object GetState()
        {
            return null;
        }
    }

    
}

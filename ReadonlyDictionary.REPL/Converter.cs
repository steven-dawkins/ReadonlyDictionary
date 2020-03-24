using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReadonlyDictionary.Storage;
using ReadonlyDictionary.Serialization;
using System.IO;
using System.Linq;

namespace ReadonlyDictionary.REPL
{
    public class Converter : Replify.IReplCommand
    {
        public void ConvertToJson(string filename)
        {
            using (var store = FileIndexKeyValueStorageBuilder<string, JObject>.Open(filename, serializer: new JsonSerializer<JObject>()))
            {

                var everything = from key in store.GetKeys()
                                 select new { Key = key, Value = store.Get(key) };

                var everythingJson = JsonConvert.SerializeObject(everything, Formatting.Indented);

                File.WriteAllText(filename + ".keys.json", JsonConvert.SerializeObject(store.GetKeys()));
                File.WriteAllText(filename + ".json", everythingJson);
            }
        }
    }
}

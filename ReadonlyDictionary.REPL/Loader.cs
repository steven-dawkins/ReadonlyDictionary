using Newtonsoft.Json.Linq;
using ReadonlyDictionary.Storage;

namespace ReadonlyDictionary.REPL
{
    public class Loader : Replify.IReplCommand
    {
        public StorageWrapper<string, JObject> File(string filename)
        {
            var store = FileIndexKeyValueStorageBuilder<string, JObject>.Open(filename);

            return new StorageWrapper<string, JObject>(store);
        }
    }
}

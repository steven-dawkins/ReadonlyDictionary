using Newtonsoft.Json.Linq;
using ReadonlyDictionary.Storage;
using System.Collections.Generic;
using System.Linq;

namespace ReadonlyDictionary.REPL
{
    public class StorageWrapper<TKey, T>
    {
        private readonly IKeyValueStore<TKey, T> store;

        public StorageWrapper(IKeyValueStore<TKey, T> store)
        {
            this.store = store;
        }

        public TKey[] GetKeys()
        {
            return this.store.GetKeys().ToArray();
        }

        public T Get(TKey key)
        {
            return this.store.Get(key);
        }

        public IEnumerable<T> GetAll()
        {
            foreach(var key in GetKeys())
            {
                yield return Get(key);
            }
        }

        public long Count()
        {
            return this.GetAll().LongCount();
        }

        public string[] GetAdditionalDataKeys()
        {
            return this.store.GetAdditionalDataKeys().ToArray();
        }

        public JObject GetAdditionalDataObject(string key)
        {
            return this.store.GetAdditionalData<JObject>(key);
        }

        public JArray GetAdditionalDataArray(string key)
        {
            return this.store.GetAdditionalData<JArray>(key);
        }

        public override string ToString()
        {
            return store.ToString();
        }
    }
}

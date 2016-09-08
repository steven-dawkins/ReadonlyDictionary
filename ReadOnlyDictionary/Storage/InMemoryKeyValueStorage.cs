using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadonlyDictionary.Storage
{
    public class InMemoryKeyValueStorage<TKey, TValue> : IKeyValueStore<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> index;
        private readonly Dictionary<string, object> additionalMetadata;

        public InMemoryKeyValueStorage(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            this.index = values.ToDictionary(v => v.Key, v => v.Value);
            this.additionalMetadata = new Dictionary<string, object>();
        }

        public bool ContainsKey(TKey key)
        {
            return this.index.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.index.TryGetValue(key, out value);
        }

        public uint Count
        {
            get { return (uint)this.index.LongCount(); }
        }

        public IEnumerable<TKey> GetKeys()
        {
            return this.index.Keys;
        }

        public void Dispose()
        {
        }

        public T2 GetAdditionalData<T2>(string name)
        {
            if (!additionalMetadata.ContainsKey(name))
            {
                return default(T2);
            }

            return (T2)this.additionalMetadata[name];
        }

        public IEnumerable<string> GetAdditionalDataKeys()
        {
            return this.additionalMetadata.Keys;
        }
    }
}

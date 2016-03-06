using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadOnlyDictionary.Storage
{
    public class InMemoryKeyValueStorage<TKey, TValue> : IKeyValueStore<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> index;

        public InMemoryKeyValueStorage(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            this.index = values.ToDictionary(v => v.Key, v => v.Value);
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
    }
}

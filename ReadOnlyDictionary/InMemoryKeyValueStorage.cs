using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadOnlyDictionary
{
    public class InMemoryKeyValueStorage<TValue> : IKeyValueStore<TValue>
    {
        private readonly Dictionary<Guid, TValue> index;

        public InMemoryKeyValueStorage(IEnumerable<KeyValuePair<Guid, TValue>> values)
        {
            this.index = values.ToDictionary(v => v.Key, v => v.Value);
        }

        public bool ContainsKey(Guid key)
        {
            return this.index.ContainsKey(key);
        }

        public bool TryGetValue(Guid key, out TValue value)
        {
            return this.index.TryGetValue(key, out value);
        }

        public uint Count
        {
            get { return (uint)this.index.LongCount(); }
        }

        public void Dispose()
        {
        }
    }
}

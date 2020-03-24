/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadonlyDictionary
{
    public class ReadOnlyPersistantDictionaryFacade<TValue> : IDictionary<Guid, TValue>
    {
        private readonly ReadOnlyKeyValueStorage<TValue> storage;

        public ReadOnlyPersistantDictionaryFacade(ReadOnlyKeyValueStorage<TValue> storage)
        {
            this.storage = storage;
        }

        public void Add(Guid key, TValue value)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(Guid key)
        {
            return storage.ContainsKey(key);
        }

        public ICollection<Guid> Keys
        {
            get { return storage.Keys; }
        }

        public bool Remove(Guid key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Guid key, out TValue value)
        {
            return storage.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get
            {
                return this.AsEnumerable().ToArray();
            }
        }

        public TValue this[Guid key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Add(KeyValuePair<Guid, TValue> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<Guid, TValue> item)
        {
            return this.storage.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<Guid, TValue>[] array, int arrayIndex)
        {
            this.ToArray().CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return (int)storage.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<Guid, TValue> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<Guid, TValue>> GetEnumerator()
        {
            foreach (var key in storage.Keys)
            {
                TValue temp;
                if (storage.TryGetValue(key, out temp))
                {
                    yield return new KeyValuePair<Guid, TValue>(key, temp);
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.AsEnumerable().GetEnumerator();
        }

        private IEnumerable<TValue> AsEnumerable()
        {
            foreach (var key in storage.Keys)
            {
                TValue temp;
                if (storage.TryGetValue(key, out temp))
                {
                    yield return temp;
                }
            }
        }

    }
}
*/
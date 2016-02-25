using System;

namespace ReadOnlyDictionary.Storage
{
    public interface IKeyValueStore<TValue> : IDisposable
    {
        bool ContainsKey(Guid key);
        bool TryGetValue(Guid key, out TValue value);
        uint Count { get; }
    }

    public static class IKeyValueStoreExtensions
    {
        public static T Get<T>(this IKeyValueStore<T> store, Guid key)
        {
            T temp;
            if (store.TryGetValue(key, out temp))
            {
                return temp;
            }
            else
            {
                return default(T);
            }
        }
    }
}

using System;

namespace ReadOnlyDictionary.Storage
{
    public interface IKeyValueStore<TKey, TValue> : IDisposable
    {
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);
        uint Count { get; }
    }

    public static class IKeyValueStoreExtensions
    {
        public static T Get<T, TKey>(this IKeyValueStore<TKey, T> store, TKey key)
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

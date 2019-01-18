namespace ReadonlyDictionary.Storage
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public interface IKeyValueStore<TKey, TValue> : IDisposable
    {
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);
        uint Count { get; }
        IEnumerable<TKey> GetKeys();

        T2 GetAdditionalData<T2>(string name, JsonSerializerSettings settings = null);
        IEnumerable<string> GetAdditionalDataKeys();
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

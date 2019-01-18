namespace ReadonlyDictionary.Index
{
    using System.Collections.Generic;

    public interface IIndexSerializer<T>
    {
        IIndex<T> Deserialize(byte[] bytes);
        byte[] Serialize(IEnumerable<KeyValuePair<T, long>> values);
    }

    public interface IIndex<T>
    {
        long Get(T key);

        bool ContainsKey(T key);

        bool TryGetValue(T key, out long index);

        uint Count { get; }

        IEnumerable<T> Keys { get; }
    }
}

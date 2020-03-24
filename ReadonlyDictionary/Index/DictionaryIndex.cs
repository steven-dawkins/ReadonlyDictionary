using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReadonlyDictionary.Index
{
    public class DictionaryIndexSerializer<T> : IIndexSerializer<T>
    {
        public IIndex<T> Deserialize(byte[] bytes)
        {
            return new DictionaryIndex<T>(bytes);
        }

        public byte[] Serialize(IEnumerable<KeyValuePair<T, long>> values)
        {
            return new DictionaryIndex<T>(values).Serialize();
        }
    }

    public class DictionaryIndex<T> : IIndex<T>
    {
        private Dictionary<T, long> dictionary;

        public DictionaryIndex(IEnumerable<KeyValuePair<T, long>> values)
        {
            var dupes = values.GroupBy(v => v.Key).Where(g => g.Count() > 1);

            if (dupes.Count() > 1)
            {
                throw new Exception("Duplicate keys: " + String.Join(", ", dupes.Select(v => v.Key)));
            }

            this.dictionary = values.ToDictionary(v => v.Key, v => v.Value);
        }

        public DictionaryIndex(byte[] bytes)
        {
            var indexJson = Encoding.UTF8.GetString(bytes);
            if (typeof(T) == typeof(string))
            {
                this.dictionary = (Dictionary<T, long>)(object)this.FastDeserialize(indexJson);
            }
            else
            {
                this.dictionary = JsonConvert.DeserializeObject<Dictionary<T, long>>(indexJson);
            }
        }

        private Dictionary<string, long> FastDeserialize(string json)
        {
            var weight = 0.0308686625;
            var estimatedItemCount = (int)(json.Length * weight);

            var items = GetListOfItems(json, estimatedItemCount);

            var itemCount = items.Count();
            Console.WriteLine("saving " + (estimatedItemCount - itemCount));
            var result = new Dictionary<string, long>(itemCount);

            foreach (var item in items)
            {
                result.Add(item.Key, item.Value);
            }

            return result;


            //var weight = 0.0308686625;

            //var result = new Dictionary<string, long>((int)(json.Length * weight));
            //var reader = new JsonTextReader(new StringReader(json));
            //string key = null;
            //while (reader.Read())
            //{
            //    if (reader.TokenType == JsonToken.PropertyName)
            //    {
            //        key = reader.Value.ToString();
            //    }

            //    if (reader.TokenType == JsonToken.Integer)
            //    {
            //        result.Add(key, (long)reader.Value);
            //    }
            //}

            //return result;
        }

        private static IEnumerable<KeyValuePair<string, long>> GetListOfItems(string json, int estimatedItemCount)
        {
            var items = new List<KeyValuePair<string, long>>(estimatedItemCount);
            var reader = new JsonTextReader(new StringReader(json));
            string key = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    key = reader.Value.ToString();
                }

                if (reader.TokenType == JsonToken.Integer)
                {
                    yield return new KeyValuePair<string, long>(key, (long)reader.Value);
                }
            }
        }

        public byte[] Serialize()
        {
            var indexJson = JsonConvert.SerializeObject(this.dictionary); // todo: use passed in serializer (just for consistency)
            var indexBytes = Encoding.UTF8.GetBytes(indexJson);
            return indexBytes;
        }

        public void Add(T key, long index)
        {
            this.dictionary.Add(key, index);
        }

        public long Get(T key)
        {
            return this.dictionary[key];
        }


        public bool ContainsKey(T key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public bool TryGetValue(T key, out long index)
        {
            return this.dictionary.TryGetValue(key, out index);
        }

        public uint Count
        {
            get { return (uint)this.dictionary.LongCount(); }
        }

        public IEnumerable<T> Keys
        {
            get { return this.dictionary.Keys; }
        }
    }

}

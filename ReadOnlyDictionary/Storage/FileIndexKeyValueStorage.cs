using ReadonlyDictionary.Exceptions;
using ReadonlyDictionary.Format;
using ReadonlyDictionary.Index;
using ReadonlyDictionary.Storage.Stores;
using ReadOnlyDictionary.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ReadOnlyDictionary.Storage
{    
    public class FileIndexKeyValueStorage<TKey, TValue> : IKeyValueStore<TKey, TValue>, IDisposable
    {
        private readonly ISerializer<TValue> serializer;        
        
        private readonly IIndex<TKey> index;

        private IRandomAccessStore reader;
        private readonly Dictionary<string, CustomDataBlock> customBlockIndex;

        public FileIndexKeyValueStorage(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            FileInfo fi,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            IRandomAccessStore reader,
            IIndexSerializer<TKey> indexSerializer = null,
            IEnumerable<KeyValuePair<string, object>> additionalData = null)
        {
            this.serializer = serializer;
            indexSerializer = indexSerializer ?? new DictionaryIndexSerializer<TKey>();
            this.reader = reader;

            try
            {
                WriteData(values, serializer, indexSerializer, count, fi, additionalData);
            }
            catch(Exception)
            {
                // if something unexpectedly breaks during population (easy to happen externally as we are fed an IEnumerable)
                // then ensure no partially populated files are left around
                this.Dispose();
             
                File.Delete(fi.FullName);

                throw;
            }
        }

        public unsafe FileIndexKeyValueStorage(
            FileInfo fi, 
            IRandomAccessStore reader, 
            ISerializer<TValue> serializer = null, 
            IIndexSerializer<TKey> indexSerializer = null)
        {
            indexSerializer = indexSerializer ?? new DictionaryIndexSerializer<TKey>();
            this.reader = reader;


            try
            {                

                // file begins with header
                Header header = reader.Read<Header>(0);

                if (header.magic == Guid.Empty)
                {
                    throw new NoMagicException(fi.FullName);
                }
                if (header.magic != Header.expectedMagic)
                {
                    throw new InvalidMagicException(fi.FullName);
                }

                switch (header.SerializationStrategy)
                {
                    case Header.SerializationStrategyEnum.Json:
                        this.serializer = new JsonSerializer<TValue>();
                        break;
                    case Header.SerializationStrategyEnum.Protobuf:
                        this.serializer = new ProtobufSerializer<TValue>();
                        break;
                    case Header.SerializationStrategyEnum.JsonFlyWeight:
                        var stateBytes = reader.ReadArray(header.SerializerJsonStart, header.SerializerJsonLength);
                        var stateJson = Encoding.ASCII.GetString(stateBytes);
                        var state = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonFlyweightSerializer<TValue>.JsonFlyweightSerializerState>(stateJson);
                        this.serializer = new JsonFlyweightSerializer<TValue>(state);
                        break;
                    case Header.SerializationStrategyEnum.Custom:
                        if (serializer == null)
                        {
                            throw new ArgumentException("Readonlydictionary users custom serializer which was not supplied");
                        }
                        else
                        {
                            this.serializer = serializer;                            
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unexpected header.SerializationStrategy: {header.SerializationStrategy}");
                }                

                byte[] indexJsonBytes = reader.ReadArray(header.IndexPosition, header.IndexLength);

                this.index = indexSerializer.Deserialize(indexJsonBytes);

                var blocks = new List<CustomDataBlock>(header.customBlockCount);

                for(int i = 0; i < header.customBlockCount; i++)
                {
                    long customBlockPosition = GetCustomBlockPosition(header, i);
                    var block = reader.Read<CustomDataBlock>(customBlockPosition);

                    blocks.Add(block);                 
                }

                this.customBlockIndex = blocks.ToDictionary(b => new string(b.Name).Trim());
            }
            catch(Exception)
            {
                this.Dispose();
                this.reader.Dispose();
                throw;
            }
        }

        private static unsafe long GetCustomBlockPosition(Header header, int i)
        {
            return header.IndexPosition + header.IndexLength + header.SerializerJsonLength + sizeof(CustomDataBlock) * i;
        }

        public bool ContainsKey(TKey key)
        {
            return this.index.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            long index;
            if (this.index.TryGetValue(key, out index))
            {
                var serializedSize = this.reader.ReadInt32(index);
                byte[] serialized = this.reader.ReadArray(index + sizeof(Int32), serializedSize);
                value = serializer.Deserialize(serialized);
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public uint Count
        {
            get { return this.index.Count; }
        }

        public IEnumerable<TKey> GetKeys()
        {
            return this.index.Keys;
        }

        public T2 GetAdditionalData<T2>(string name)
        {
            if (!this.customBlockIndex.ContainsKey(name))
            {
                return default(T2);
            }

            var block = this.customBlockIndex[name];

            var blockBytes = reader.ReadArray(block.Position, block.Length);
            var blockJson = Encoding.ASCII.GetString(blockBytes);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T2>(blockJson);
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }
        }

        private unsafe void WriteData(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            ISerializer<TValue> serializer,
            IIndexSerializer<TKey> indexSerializer,
            long count,
            FileInfo filename,
            IEnumerable<KeyValuePair<string, object>> additionalBlocks)
        {
            additionalBlocks = additionalBlocks ?? new KeyValuePair<string, object>[] { };
            var header = new Header();

            // allocate space for index
            long position = Marshal.SizeOf(typeof(Header));
            header.DataPosition = position;

            var indexValues = new List<KeyValuePair<TKey, long>>();
            foreach (var item in values)
            {
                var value = item.Value;
                var serialized = serializer.Serialize(value);

                if (position + serialized.Length + sizeof(Int32) > reader.Capacity)
                {
                    this.reader.Resize(reader.Capacity * 2);
                }

                reader.Write(position, serialized.Length);
                reader.WriteArray(position + sizeof(Int32), serialized);

                indexValues.Add(new KeyValuePair<TKey, long>(item.Key, position));

                position += serialized.Length + sizeof(Int32);
            }

            var indexBytes = indexSerializer.Serialize(indexValues);

            header.IndexPosition = position;
            header.IndexLength = indexBytes.Length;

            if (serializer is JsonSerializer<TValue>)
            {
                header.SerializationStrategy = Header.SerializationStrategyEnum.Json;
            }
            else if (serializer is ProtobufSerializer<TValue>)
            {
                header.SerializationStrategy = Header.SerializationStrategyEnum.Protobuf;
            }
            else if (serializer is JsonFlyweightSerializer<TValue>)
            {
                header.SerializationStrategy = Header.SerializationStrategyEnum.JsonFlyWeight;
            }
            else
            {
                header.SerializationStrategy = Header.SerializationStrategyEnum.Custom;
            }

            header.Version = Header.CurrentVersion;

            var serializerJsonBytes = Encoding.ASCII.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(serializer.GetState()));

            header.SerializerJsonStart = header.IndexPosition + indexBytes.Length;
            header.SerializerJsonLength = serializerJsonBytes.Length;

            // Resize to include index
            this.reader.Resize(header.IndexPosition + header.IndexLength + header.SerializerJsonLength);

            reader.WriteArray(header.IndexPosition, indexBytes);
            reader.WriteArray(header.IndexPosition + indexBytes.Length, serializerJsonBytes);

            var c = additionalBlocks.Select(a => new
            {
                Name = a.Key,
                Bytes = Encoding.ASCII.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(a.Value))
            }).ToArray();

            var blocks = c.Select(content => ToCustomDataBlock(content.Name, content.Bytes)).ToArray();        

            // Resize down to include custom blocks
            this.reader.Resize(header.IndexPosition + header.IndexLength + header.SerializerJsonLength + blocks.Length * sizeof(CustomDataBlock) + blocks.Sum(b => b.Length));

            var customDataPosition = header.IndexPosition + header.IndexLength + header.SerializerJsonLength + blocks.Length * sizeof(CustomDataBlock);
            
            for (int i = 0; i < c.Length; i++)
            {                
                blocks[i].Position = customDataPosition;                
                this.reader.WriteArray(blocks[i].Position, c[i].Bytes);                
                this.reader.Write(GetCustomBlockPosition(header, i), ref blocks[i]);                

                customDataPosition += c[i].Bytes.Length;
            }
       
            // store header in file
            header.customBlockCount = blocks.Length;
            header.Count = count;
            header.magic = Header.expectedMagic;
            reader.Write(0, ref header);

            reader.Flush();
        }

        private unsafe CustomDataBlock ToCustomDataBlock(string name, byte[] customContentBytes)
        {            
            var result = new CustomDataBlock();
            
            result.Length = customContentBytes.Length;

            for (int i = 0; i < name.Length; i++)
            {
                result.Name[i] = name[i];
            }

            return result;
        }

        public override string ToString()
        {
            return $"Serializer: {serializer} Count: {index.Count} Reader: {reader}";
        }
    }
}

﻿namespace ReadonlyDictionary.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using Newtonsoft.Json;
    using ReadonlyDictionary.Exceptions;
    using ReadonlyDictionary.Format;
    using ReadonlyDictionary.Index;
    using ReadonlyDictionary.Serialization;
    using ReadonlyDictionary.Storage.Stores;

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
            IEnumerable<KeyValuePair<string, object>> additionalData = null,
            JsonSerializerSettings additionalDataSerializerSettings = null)
        {
            this.serializer = serializer;
            indexSerializer = indexSerializer ?? new DictionaryIndexSerializer<TKey>();
            this.reader = reader;

            try
            {
                this.WriteData(values, serializer, indexSerializer, count, fi, additionalData, additionalDataSerializerSettings);
            }
            catch (Exception)
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

                if (serializer == null)
                {
                    this.serializer = this.ReadSerializerFromHeader(header);
                }
                else
                {
                    this.serializer = serializer;
                }

                byte[] indexJsonBytes = reader.ReadArray(header.IndexPosition, header.IndexLength);

                this.index = indexSerializer.Deserialize(indexJsonBytes);

                var blocks = new List<CustomDataBlock>(header.customBlockCount);

                for (int i = 0; i < header.customBlockCount; i++)
                {
                    long customBlockPosition = GetCustomBlockPosition(header, i);
                    var block = reader.Read<CustomDataBlock>(customBlockPosition);

                    blocks.Add(block);
                }

                this.customBlockIndex = blocks.ToDictionary(b => new string(b.Name).Trim());
            }
            catch (Exception)
            {
                this.Dispose();

                throw;
            }
        }

        private ISerializer<TValue> ReadSerializerFromHeader(Header header)
        {
            switch (header.SerializationStrategy)
            {
                case Header.SerializationStrategyEnum.Json:
                    return new JsonSerializer<TValue>();
                case Header.SerializationStrategyEnum.Protobuf:
                    return new ProtobufSerializer<TValue>();
                case Header.SerializationStrategyEnum.JsonFlyWeight:
                    var stateBytes = this.reader.ReadArray(header.SerializerJsonStart, header.SerializerJsonLength);
                    var stateJson = Encoding.ASCII.GetString(stateBytes);
                    var state = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonFlyweightSerializer<TValue>.JsonFlyweightSerializerState>(stateJson);
                    return new JsonFlyweightSerializer<TValue>(state);
                case Header.SerializationStrategyEnum.Custom:
                    throw new ArgumentException("Readonlydictionary uses custom serializer which was not supplied");
                default:
                    throw new ArgumentException($"Unexpected header.SerializationStrategy: {header.SerializationStrategy}");
            }
        }

        private static unsafe long GetCustomBlockPosition(Header header, int i)
        {
            return header.IndexPosition + header.IndexLength + header.SerializerJsonLength + sizeof(CustomDataBlock) * i;
        }

        public bool ContainsKey(TKey key)
        {
            lock (this.mutex)
            {
                return this.index.ContainsKey(key);
            }
        }

        private readonly object mutex = new object();

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this.mutex)
            {
                long index;
                if (this.index.TryGetValue(key, out index))
                {
                    var serializedSize = this.reader.ReadInt32(index);
                    byte[] serialized = this.reader.ReadArray(index + sizeof(Int32), serializedSize);
                    value = this.serializer.Deserialize(serialized);
                    return true;
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
        }

        public uint Count
        {
            get
            {
                lock (this.mutex)
                {
                    return this.index.Count;
                }
            }
        }

        public IEnumerable<TKey> GetKeys()
        {
            lock (this.mutex)
            {
                return this.index.Keys;
            }
        }

        public T2 GetAdditionalData<T2>(string name, JsonSerializerSettings settings = null)
        {
            try
            {
                return this.UnzipAndDeserialize<T2>(name, settings, true);
            }
            catch (Exception)
            {
                return this.UnzipAndDeserialize<T2>(name, settings, false);
            }
        }

        private T2 UnzipAndDeserialize<T2>(string name, JsonSerializerSettings settings, bool unzip)
        {
            var json = this.GetAdditionalDataJson(name, unzip);

            if (json == null)
            {
                return default(T2);
            }

            return JsonConvert.DeserializeObject<T2>(json, settings ?? new JsonSerializerSettings());
        }

        public string GetAdditionalDataJson(string name, bool unzip)
        {
            lock (this.mutex)
            {
                if (!this.customBlockIndex.ContainsKey(name))
                {
                    return null;
                }

                var block = this.customBlockIndex[name];

                var blockBytes = this.reader.ReadArray(block.Position, block.Length);

                var blockJson = BytesToJson(blockBytes, unzip);

                return blockJson;
            }
        }

        public IEnumerable<string> GetAdditionalDataKeys()
        {
            lock (this.mutex)
            {
                return this.customBlockIndex.Keys;
            }
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
            IEnumerable<KeyValuePair<string, object>> additionalBlocks,
            JsonSerializerSettings additionalDataSerializerSettings)
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

                if (position + serialized.Length + sizeof(Int32) > this.reader.Capacity)
                {
                    this.reader.Resize(this.reader.Capacity * 2);
                }

                this.reader.Write(position, serialized.Length);
                this.reader.WriteArray(position + sizeof(Int32), serialized);

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

            this.reader.WriteArray(header.IndexPosition, indexBytes);
            this.reader.WriteArray(header.IndexPosition + indexBytes.Length, serializerJsonBytes);

            var c = additionalBlocks.Select(a => new
            {
                Name = a.Key,
                Bytes = GetMetadataBytes(a),
            }).ToArray();

            var blocks = c.Select(content => this.ToCustomDataBlock(content.Name, content.Bytes)).ToArray();

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
            this.reader.Write(0, ref header);

            this.reader.Flush();
        }

        private static byte[] GetMetadataBytes(KeyValuePair<string, object> a)
        {
            try
            {
                // return Encoding.ASCII.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(a.Value));
                return ZipStr(JsonConvert.SerializeObject(a.Value));
            }
            catch (Exception e)
            {
                throw new Exception($"Error serializing metadata: {a.Key}", e);
            }
        }

        private static byte[] ZipStr(string str)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream gzip =
                  new DeflateStream(output, CompressionLevel.Optimal))
                {
                    using (StreamWriter writer =
                      new StreamWriter(gzip, System.Text.Encoding.UTF8))
                    {
                        writer.Write(str);
                        writer.Flush();
                    }

                    output.Flush();
                }

                return output.ToArray();
            }
        }

        private static string BytesToJson(byte[] blockBytes, bool unzip)
        {
            if (unzip)
            {
                var blockJson = UnZip(blockBytes);
                return blockJson;
            }
            else
            {
                var blockJson = Encoding.ASCII.GetString(blockBytes);
                return blockJson;
            }
        }

        private static string UnZip(byte[] bytes)
        {
            using (MemoryStream input = new MemoryStream(bytes, false))
            {
                using (DeflateStream gzip =
                  new DeflateStream(input, CompressionMode.Decompress))
                {
                    using (StreamReader reader =
                      new StreamReader(gzip, System.Text.Encoding.UTF8))
                    {
                        input.Flush();
                        return reader.ReadToEnd();
                    }
                }
            }
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
            return $"Serializer: {this.serializer} Count: {this.index.Count} Reader: {this.reader}";
        }
    }
}
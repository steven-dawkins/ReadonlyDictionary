using ReadonlyDictionary.Exceptions;
using ReadonlyDictionary.Format;
using ReadonlyDictionary.Index;
using ReadonlyDictionary.Storage.Stores;
using ReadOnlyDictionary.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ReadOnlyDictionary.Storage
{
    public class FileIndexKeyValueStorage<TKey, TValue> : IKeyValueStore<TKey, TValue>, IDisposable
    {
        private readonly ISerializer<TValue> serializer;
        private readonly IIndexFactory<TKey> indexFactory;
        
        private readonly IIndex<TKey> index;

        private IRandomAccessStore reader;

        public enum AccessStrategy { Streams, MemoryMapped }

        public static FileIndexKeyValueStorage<TKey, TValue> CreateOrOpen(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            AccessStrategy strategy = AccessStrategy.MemoryMapped,
            IIndexFactory<TKey> indexFactory = null)
        {
            var fi = new FileInfo(filename);

            IRandomAccessStore reader = GetCreateReaderForStrategy(initialSize, strategy, fi);

            if (fi.Exists)
            {
                try
                {
                    return new FileIndexKeyValueStorage<TKey, TValue>(fi, serializer, reader, indexFactory);
                }
                catch(NoMagicException)
                {
                    // no magic almost certainly means the file was partially written as the header is written last
                    return new FileIndexKeyValueStorage<TKey, TValue>(values, fi, initialSize, serializer, count, reader, indexFactory);
                }
            }
            else
            {
                return new FileIndexKeyValueStorage<TKey, TValue>(values, fi, initialSize, serializer, count, reader, indexFactory);
            }
        }

        private static IRandomAccessStore GetCreateReaderForStrategy(long initialSize, AccessStrategy strategy, FileInfo fi)
        {
            IRandomAccessStore reader;
            switch (strategy)
            {
                case AccessStrategy.MemoryMapped:
                    reader = new MemoryMappedStore(fi, initialSize);
                    break;
                case AccessStrategy.Streams:
                    reader = new StreamStore(fi, initialSize);
                    break;
                default:
                    throw new Exception("Unexpected access strategy: " + strategy);
            }
            return reader;
        }

        private static IRandomAccessStore GetReaderForStrategy(AccessStrategy strategy, FileInfo fi)
        {
            IRandomAccessStore reader;
            switch (strategy)
            {
                case AccessStrategy.MemoryMapped:
                    reader = new MemoryMappedStore(fi);
                    break;
                case AccessStrategy.Streams:
                    reader = new StreamStore(fi);
                    break;
                default:
                    throw new Exception("Unexpected access strategy: " + strategy);
            }
            return reader;
        }

        public static FileIndexKeyValueStorage<TKey, TValue> Create(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            AccessStrategy strategy = AccessStrategy.MemoryMapped,
            IIndexFactory<TKey> indexFactory = null)
        {
            var fi = new FileInfo(filename);
            var reader = GetCreateReaderForStrategy(initialSize, strategy, fi);
            return new FileIndexKeyValueStorage<TKey, TValue>(values, fi, initialSize, serializer, count, reader, indexFactory);
        }

        public static FileIndexKeyValueStorage<TKey, TValue> Open(
            string filename, 
            ISerializer<TValue> serializer, 
            AccessStrategy strategy = AccessStrategy.MemoryMapped,
            IIndexFactory<TKey> indexFactory = null)
        {
            var fi = new FileInfo(filename);
            var reader = GetReaderForStrategy(strategy, fi);
            return new FileIndexKeyValueStorage<TKey, TValue>(fi, serializer, reader, indexFactory);
        }

        public FileIndexKeyValueStorage(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            FileInfo fi,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            IRandomAccessStore reader,
            IIndexFactory<TKey> indexFactory = null)
        {
            this.serializer = serializer;
            this.indexFactory = indexFactory ?? new DictionaryIndexFactory<TKey>();
            this.reader = reader;

            try
            {
                WriteData(values, serializer, count, fi);
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

        public FileIndexKeyValueStorage(FileInfo fi, ISerializer<TValue> serializer, IRandomAccessStore reader, IIndexFactory<TKey> indexFactory = null)
        {
            try
            {
                this.serializer = serializer;
                this.indexFactory = indexFactory ?? new DictionaryIndexFactory<TKey>();
                this.reader = reader;

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

                byte[] indexJsonBytes = reader.ReadArray(header.IndexPosition, header.IndexLength);

                this.index = this.indexFactory.Deserialize(indexJsonBytes);
            }
            catch(Exception)
            {
                this.Dispose();
                this.reader.Dispose();
                throw;
            }
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

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }
        }

        private void WriteData(IEnumerable<KeyValuePair<TKey, TValue>> values, ISerializer<TValue> serializer, long count, FileInfo filename)
        {
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

            var indexBytes = this.indexFactory.Serialize(indexValues);
            
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
            else
            {
                header.SerializationStrategy = Header.SerializationStrategyEnum.Custom;
            }

            header.Version = Header.CurrentVersion;

            // Resize down to minimum size
            this.reader.Resize(header.IndexPosition + header.IndexLength);

            reader.WriteArray(header.IndexPosition, indexBytes);

            // store header in file
            header.Count = count;
            header.magic = Header.expectedMagic;
            reader.Write(0, ref header);

            reader.Flush();
        }
    }
}

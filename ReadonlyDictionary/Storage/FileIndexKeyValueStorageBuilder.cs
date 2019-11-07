namespace ReadonlyDictionary.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Polly;
    using ReadonlyDictionary.Exceptions;
    using ReadonlyDictionary.Index;
    using ReadonlyDictionary.Serialization;
    using ReadonlyDictionary.Storage;
    using ReadonlyDictionary.Storage.Stores;

    public class FileIndexKeyValueStorageBuilder<TKey, TValue>
    {
        public enum AccessStrategy
        {
            Streams,
            MemoryMapped,
        }

        public static FileIndexKeyValueStorage<TKey, TValue> CreateOrOpen(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            long count,
            ISerializer<TValue> serializer = null,
            AccessStrategy strategy = AccessStrategy.MemoryMapped,
            IIndexSerializer<TKey> indexSerializer = null,
            IEnumerable<KeyValuePair<string, object>> additionalMetadata = null)
        {
            var fi = new FileInfo(filename);

            IRandomAccessStore reader = GetCreateReaderForStrategy(initialSize, strategy, fi);

            if (fi.Exists)
            {
                try
                {
                    return new FileIndexKeyValueStorage<TKey, TValue>(fi, reader, serializer, indexSerializer);
                }
                catch (NoMagicException)
                {
                    // no magic almost certainly means the file was partially written as the header is written last
                    return new FileIndexKeyValueStorage<TKey, TValue>(values, fi, initialSize, serializer, count, reader, indexSerializer, additionalData: additionalMetadata);
                }
            }
            else
            {
                return new FileIndexKeyValueStorage<TKey, TValue>(values, fi, initialSize, serializer, count, reader, indexSerializer, additionalData: additionalMetadata);
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
            IIndexSerializer<TKey> indexFactory = null,
            IEnumerable<KeyValuePair<string, object>> additionalMetadata = null,
            JsonSerializerSettings additionalDataSerializerSettings = null
            )
        {
            var fi = new FileInfo(filename);
            var reader = GetCreateReaderForStrategy(initialSize, strategy, fi);
            return new FileIndexKeyValueStorage<TKey, TValue>(values, fi, initialSize, serializer, count, reader, indexFactory, additionalMetadata, additionalDataSerializerSettings);
        }

        public static FileIndexKeyValueStorage<TKey, TValue> Open(
            string filename,
            AccessStrategy strategy = AccessStrategy.MemoryMapped,
            ISerializer<TValue> serializer = null,
            IIndexSerializer<TKey> indexFactory = null,
            Policy policy = null)
        {
            var fi = new FileInfo(filename);
            var reader = GetReaderForStrategy(strategy, fi);

            if (policy != null)
            {
                reader = new PolicyStore(reader, policy);
            }

            return new FileIndexKeyValueStorage<TKey, TValue>(fi, reader, serializer, indexFactory);
        }
    }
}

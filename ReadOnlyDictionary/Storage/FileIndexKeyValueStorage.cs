using ReadOnlyDictionary.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace ReadOnlyDictionary.Storage
{
    public class FileIndexKeyValueStorage<TValue> : IKeyValueStore<TValue>, IDisposable
    {
        private readonly Dictionary<Guid, long> index;
        private readonly MemoryMappedFile mmf;
        private readonly MemoryMappedViewAccessor accessor;
        private readonly ISerializer<TValue> serializer;

        public FileIndexKeyValueStorage(
            IEnumerable<KeyValuePair<Guid, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count)
        {
            this.serializer = serializer;

            this.index = new Dictionary<Guid, long>();

            var fi = new FileInfo(filename);
            if (fi.Exists)
            {
                fi.Delete();
            }

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.CreateNew, filename, initialSize);

            this.accessor = mmf.CreateViewAccessor();

            var guidBytes = Guid.NewGuid().ToByteArray().Length;
            // allocate space for index
            long indexSize = count * (guidBytes + sizeof(long));
            long position = indexSize + sizeof(long);

            foreach (var item in values)
            {
                var value = item.Value;
                var serialized = serializer.Serialize(value);
                accessor.Write(position, serialized.Length);
                accessor.WriteArray(position + sizeof(Int32), serialized, 0, serialized.Length);

                index.Add(item.Key, position);
                position += serialized.Length + sizeof(Int32);
            }

            // store count in file
            accessor.Write(0, count);

            // write index after count
            long indexPosition = sizeof(long);
            foreach (var item in index)
            {
                var keyBytes = item.Key.ToByteArray();
                accessor.Write(indexPosition, item.Value);
                indexPosition += sizeof(long);
                accessor.WriteArray(indexPosition, keyBytes, 0, keyBytes.Length);
                indexPosition += keyBytes.Length;
            }

            this.accessor.Flush();
        }

        private static FileInfo PrepareFileForWriting(string filename)
        {
            var fi = new FileInfo(filename);
            if (fi.Exists)
            {
                fi.Delete();
            }

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            return fi;
        }

        public FileIndexKeyValueStorage(string filename, ISerializer<TValue> serializer)
        {
            this.serializer = serializer;
            var fi = new FileInfo(filename);
            this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.Open);
            this.accessor = mmf.CreateViewAccessor();
            this.index = new Dictionary<Guid, long>();

            // file begins with count
            var count = accessor.ReadInt64(0);

            // read index after count
            long indexPosition = sizeof(long);
            var guidBytes = Guid.NewGuid().ToByteArray().Length;

            for (int i = 0; i < count; i++)
            {
                var offset = accessor.ReadInt64(indexPosition);
                indexPosition += sizeof(long);
                byte[] keyBytes = new byte[guidBytes];
                accessor.ReadArray(indexPosition, keyBytes, 0, guidBytes);
                indexPosition += keyBytes.Length;

                index.Add(new Guid(keyBytes), offset);
            }
        }

        public bool ContainsKey(Guid key)
        {
            return this.index.ContainsKey(key);
        }

        public bool TryGetValue(Guid key, out TValue value)
        {
            long index;
            if (this.index.TryGetValue(key, out index))
            {
                var serializedSize = this.accessor.ReadInt32(index);
                byte[] serialized = new byte[serializedSize];
                this.accessor.ReadArray(index + sizeof(Int32), serialized, 0, serializedSize);
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
            get { return (uint)this.index.LongCount(); }
        }

        public void Dispose()
        {
            this.accessor.Flush();
            this.accessor.Dispose();
            this.mmf.Dispose();
        }
    }
}

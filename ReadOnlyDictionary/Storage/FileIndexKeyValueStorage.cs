using Newtonsoft.Json;
using ReadOnlyDictionary.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace ReadOnlyDictionary.Storage
{
    internal struct Header
    {
        public Guid magic; // magic guids, why not?
        public long Count;
        public long IndexPosition;
        public long DataPosition;
        public int IndexLength;

        public static Guid expectedMagic = Guid.Parse("22E809B7-7EFD-4D83-936C-1F3F7780B615");
    }

    public class FileIndexKeyValueStorage<TKey, TValue> : IKeyValueStore<TKey, TValue>, IDisposable
    {
        private readonly Dictionary<TKey, long> index;
        private readonly MemoryMappedFile mmf;
        private readonly MemoryMappedViewAccessor accessor;
        private readonly ISerializer<TValue> serializer;

        public FileIndexKeyValueStorage(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count)
        {
            this.serializer = serializer;

            this.index = new Dictionary<TKey, long>();

            var fi = new FileInfo(filename);
            if (fi.Exists)
            {  
                fi.Delete();
            }

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.CreateNew, fi.Name, initialSize);

            this.accessor = mmf.CreateViewAccessor();

            try
            {
                WriteData(values, serializer, count);
            }
            catch(Exception e)
            {
                // if something unexpectedly breaks during population (easy to happen externally as we are fed an IEnumerable)
                // then ensure no partially populated files are left around
                this.Dispose();
                this.accessor = null;
                this.mmf = null;
                File.Delete(fi.FullName);

                throw e;
            }
        }

        private void WriteData(IEnumerable<KeyValuePair<TKey, TValue>> values, ISerializer<TValue> serializer, long count)
        {
            var header = new Header();

            // allocate space for index
            long position = Marshal.SizeOf(typeof(Header));
            header.DataPosition = position;

            foreach (var item in values)
            {
                var value = item.Value;
                var serialized = serializer.Serialize(value);
                accessor.Write(position, serialized.Length);
                accessor.WriteArray(position + sizeof(Int32), serialized, 0, serialized.Length);

                index.Add(item.Key, position);
                position += serialized.Length + sizeof(Int32);
            }

            header.IndexPosition = position;
            var indexJson = JsonConvert.SerializeObject(this.index);
            header.IndexLength = indexJson.Length;
            accessor.WriteArray(position, indexJson.ToCharArray(), 0, indexJson.Length);

            // store header in file
            header.Count = count;
            header.magic = Header.expectedMagic;
            accessor.Write(0, ref header);

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

            // file begins with header
            Header header;
            accessor.Read<Header>(0, out header);

            if (header.magic != Header.expectedMagic)
            {
                this.Dispose();
                this.accessor = null;
                this.mmf = null;
                throw new Exception("unexpected magic number in FileIndexKeyValueStorage file: " + filename);
            }

            char[] indexJsonCharacters = new char[header.IndexLength];
            accessor.ReadArray(header.IndexPosition, indexJsonCharacters, 0, header.IndexLength);
            var indexJson = new string(indexJsonCharacters);
            this.index = JsonConvert.DeserializeObject<Dictionary<TKey, long>>(indexJson);
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
            if (this.accessor != null)
            {
                this.accessor.Flush();
                this.accessor.Dispose();
            }
            if (this.mmf != null)
            {
                this.mmf.Dispose();
            }
        }
    }
}

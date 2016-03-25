using ReadonlyDictionary.Storage.MemoryMappedFileIndex;
using ReadOnlyDictionary.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
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

    public interface IMemoryMappedFileIndexFactory<T>
    {
        IMemoryMappedFileIndex<T> Deserialize(byte[] bytes);
        byte[] Serialize(IEnumerable<KeyValuePair<T, long>> values);
    }

    public interface IMemoryMappedFileIndex<T>
    {
        long Get(T key);

        bool ContainsKey(T key);

        bool TryGetValue(T key, out long index);

        uint Count { get; }

        IEnumerable<T> Keys { get; }
    }

    
    public class FileIndexKeyValueStorage<TKey, TValue> : IKeyValueStore<TKey, TValue>, IDisposable
    {
        private class NoMagicException : Exception
        {
            public NoMagicException(string filename)
                : base("zeroed magic number in FileIndexKeyValueStorage file: " + filename)
            {
            }
        }

        private class InvalidMagicException : Exception
        {
            public InvalidMagicException(string filename)
                : base("unexpected magic number in FileIndexKeyValueStorage file: " + filename)
            {
            }
        }

        private readonly ISerializer<TValue> serializer;
        private readonly IMemoryMappedFileIndexFactory<TKey> indexFactory;

        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        private readonly IMemoryMappedFileIndex<TKey> index;

        public static FileIndexKeyValueStorage<TKey, TValue> CreateOrOpen(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            IMemoryMappedFileIndexFactory<TKey> indexFactory = null)
        {
            if (File.Exists(filename))
            {
                try
                {
                    return new FileIndexKeyValueStorage<TKey, TValue>(filename, serializer, indexFactory);
                }
                catch(NoMagicException)
                {
                    // no magic almost certainly means the file was partially written as the header is written last
                    return new FileIndexKeyValueStorage<TKey, TValue>(values, filename, initialSize, serializer, count, indexFactory);
                }
            }
            else
            {
                return new FileIndexKeyValueStorage<TKey, TValue>(values, filename, initialSize, serializer, count, indexFactory);
            }
        }

        public static FileIndexKeyValueStorage<TKey, TValue> Create(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            IMemoryMappedFileIndexFactory<TKey> indexFactory = null)
        {
            return new FileIndexKeyValueStorage<TKey, TValue>(values, filename, initialSize, serializer, count, indexFactory);
        }

        public static FileIndexKeyValueStorage<TKey, TValue> Open(
            string filename, 
            ISerializer<TValue> serializer, 
            IMemoryMappedFileIndexFactory<TKey> indexFactory = null)
        {
            return new FileIndexKeyValueStorage<TKey, TValue>(filename, serializer, indexFactory);
        }

        public FileIndexKeyValueStorage(
            IEnumerable<KeyValuePair<TKey, TValue>> values,
            string filename,
            long initialSize,
            ISerializer<TValue> serializer,
            long count,
            IMemoryMappedFileIndexFactory<TKey> indexFactory = null)
        {
            this.serializer = serializer;
            this.indexFactory = indexFactory ?? new DictionaryMemoryMappedFileIndexFactory<TKey>();

            PrepareFileForWriting(filename);

            this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.CreateNew, fi.Name, initialSize);

            this.accessor = mmf.CreateViewAccessor();

            try
            {
                WriteData(values, serializer, count, fi);
            }
            catch(Exception)
            {
                // if something unexpectedly breaks during population (easy to happen externally as we are fed an IEnumerable)
                // then ensure no partially populated files are left around
                this.Dispose();
                this.accessor = null;
                this.mmf = null;
                File.Delete(fi.FullName);

                throw;
            }
        }

        public FileIndexKeyValueStorage(string filename, ISerializer<TValue> serializer, IMemoryMappedFileIndexFactory<TKey> indexFactory = null)
        {
            try
            {
                this.serializer = serializer;
                this.indexFactory = indexFactory ?? new DictionaryMemoryMappedFileIndexFactory<TKey>();
                var fi = new FileInfo(filename);
                this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.Open);
                this.accessor = mmf.CreateViewAccessor();

                // file begins with header
                Header header;
                accessor.Read<Header>(0, out header);

                if (header.magic == Guid.Empty)
                {
                    throw new NoMagicException(filename);
                }
                if (header.magic != Header.expectedMagic)
                {
                    throw new InvalidMagicException(filename);
                }

                byte[] indexJsonBytes = new byte[header.IndexLength];
                accessor.ReadArray(header.IndexPosition, indexJsonBytes, 0, header.IndexLength);

                this.index = this.indexFactory.Deserialize(indexJsonBytes);
            }
            catch(Exception)
            {
                this.Dispose();
                this.accessor = null;
                this.mmf = null;
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
            get { return this.index.Count; }
        }

        public IEnumerable<TKey> GetKeys()
        {
            return this.index.Keys;
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

                if (position + serialized.Length + sizeof(Int32) > accessor.Capacity)
                {
                    ResizeMemoryMappedFile(accessor.Capacity * 2, filename);
                }

                accessor.Write(position, serialized.Length);
                accessor.WriteArray(position + sizeof(Int32), serialized, 0, serialized.Length);

                indexValues.Add(new KeyValuePair<TKey, long>(item.Key, position));
                
                position += serialized.Length + sizeof(Int32);
            }

            var indexBytes = this.indexFactory.Serialize(indexValues);
            
            header.IndexPosition = position;
            header.IndexLength = indexBytes.Length;

            // Resize down to minimum size
            ResizeMemoryMappedFile(header.IndexPosition + header.IndexLength, filename);

            accessor.WriteArray(header.IndexPosition, indexBytes, 0, header.IndexLength);

            // store header in file
            header.Count = count;
            header.magic = Header.expectedMagic;
            accessor.Write(0, ref header);

            this.accessor.Flush();
        }

        private void ResizeMemoryMappedFile(long newSize, FileInfo fileInfo)
        {
            var fi = new FileInfo(fileInfo.FullName + "_" + newSize);

            if (fi.Exists)
            {
                fi.Delete();
            }

            try
            {
                using (var newMMF = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.CreateNew, fi.Name, newSize))
                using (var newAccessor = newMMF.CreateViewAccessor(0, newSize))
                {
                    if (newAccessor.Capacity != newSize)
                    {
                        throw new Exception("expected capacity: " + newSize + " actual: " + newAccessor.Capacity);
                    }

                    // todo: write in blocks
                    for (long i = 0; i < Math.Min(accessor.Capacity, newSize); i++)
                    {
                        newAccessor.Write(i, accessor.ReadByte(i));
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    // cleanup temporary file
                    fi.Delete();
                }
                catch(Exception deleteException)
                {
                    // todo: log deleteException
                }
                throw;
            }

            Dispose();

            fileInfo.Delete();
            File.Move(fi.FullName, fileInfo.FullName);
            this.mmf = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.Open);
            this.accessor = this.mmf.CreateViewAccessor();
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
    }
}

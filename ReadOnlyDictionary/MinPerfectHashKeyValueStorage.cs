namespace ReadonlyDictionary {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MPHTest.MPH;
    using ReadonlyDictionary.Serialization;
    using ReadonlyDictionary.Storage;

    //public class MinPerfectHashKeyValueStorage<TValue> : IKeyValueStore<TValue>
    //{
    //    private readonly MinPerfectHash hashFunction;
    //    private readonly uint count;
    //    private readonly Func<byte[], TValue> deserializer;
    //    private MemoryMappedFile mmf;
    //    private MemoryMappedViewAccessor accessor;

    //    public MinPerfectHashKeyValueStorage(
    //        IEnumerable<KeyValuePair<Guid, TValue>> values,
    //        uint count,
    //        string filename,
    //        long initialSize,
    //        Func<TValue, byte[]> serializer,
    //        Func<byte[], TValue> deserializer)
    //    {
    //        var keyGenerator = new MphKeySource<Guid>(values.Select(v => v.Key), count);

    //        this.hashFunction = MinPerfectHash.Create(keyGenerator, 1);
    //        this.count = count;

    //        this.deserializer = deserializer;

    //        var fi = PrepareFileForWriting(filename);

    //        this.mmf = MemoryMappedFile.CreateFromFile(fi.FullName, FileMode.CreateNew, filename, initialSize);

    //        this.accessor = mmf.CreateViewAccessor();

    //        var guidBytes = Guid.NewGuid().ToByteArray().Length;
    //        // allocate space for index
    //        long indexSize = count * (guidBytes + sizeof(long));
    //        long position = indexSize + sizeof(long);

    //        this.index = new Dictionary<Guid, long>();

    //        foreach (var item in values)
    //        {
    //            var value = item.Value;
    //            var serialized = serializer(value);
    //            accessor.Write(position, serialized.Length);
    //            accessor.WriteArray(position + sizeof(Int32), serialized, 0, serialized.Length);

    //            index.Add(item.Key, position);
    //            position += serialized.Length + sizeof(Int32);
    //        }

    //        // store count in file
    //        accessor.Write(0, count);

    //        // write index after count
    //        long indexPosition = sizeof(long);
    //        foreach (var item in index)
    //        {
    //            var keyBytes = item.Key.ToByteArray();
    //            accessor.Write(indexPosition, item.Value);
    //            indexPosition += sizeof(long);
    //            accessor.WriteArray(indexPosition, keyBytes, 0, keyBytes.Length);
    //            indexPosition += keyBytes.Length;
    //        }

    //        this.accessor.Flush();
    //    }

    //    public bool ContainsKey(Guid key)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public bool TryGetValue(Guid key, out TValue value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public uint Count { get { return count; } }

    //    public void Dispose()
    //    {
    //        this.accessor.Flush();
    //        this.accessor.Dispose();
    //        this.mmf.Dispose();
    //    }

    //    private static FileInfo PrepareFileForWriting(string filename)
    //    {
    //        var fi = new FileInfo(filename);
    //        if (fi.Exists)
    //        {
    //            fi.Delete();
    //        }

    //        if (!fi.Directory.Exists)
    //        {
    //            fi.Directory.Create();
    //        }
    //        return fi;
    //    }
    //}
}

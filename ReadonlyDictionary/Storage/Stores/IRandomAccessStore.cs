namespace ReadonlyDictionary.Storage.Stores
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IRandomAccessStore : IDisposable
    {
        T Read<T>(long position) where T : struct;

        byte[] ReadArray(long position, int length);

        int ReadInt32(long index);

        long Capacity { get; }

        void Write<T>(long position, ref T data) where T : struct;

        void WriteArray(long position, byte[] bytes);

        void Write(long position, int value);

        void Resize(long newSize);

        void Flush();
    }
}

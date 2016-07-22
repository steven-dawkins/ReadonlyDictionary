using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ReadonlyDictionary.Storage.Stores
{
    public class StreamStore : IRandomAccessStore
    {
        private FileStream stream;

        public StreamStore(FileInfo fileInfo)
        {
            this.stream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public StreamStore(FileInfo fileInfo, long initialSize)
        {
            PrepareFileForWriting(fileInfo);
            this.stream = File.Open(fileInfo.FullName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            this.stream.SetLength(initialSize);
        }

        public T Read<T>(long position) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var bytes = ReadArray(position, size);
            var result = default(T);
            ByteArrayToStructure(bytes, ref result);
            return result;
        }

        public byte[] ReadArray(long position, int length)
        {
            var result = new byte[length];
            stream.Seek(position, SeekOrigin.Begin);
            var readBytes = this.stream.Read(result, 0, length);

            if (readBytes != length)
            {
                throw new Exception(String.Format("Failed to read data at position: {0} read {1} bytes, expected {2} bytes", position, readBytes, length));
            }

            return result;
        }

        public int ReadInt32(long index)
        {
            var bytes = this.ReadArray(index, sizeof(int));

            return BitConverter.ToInt32(bytes, 0);
        }

        public long Capacity
        {
            get { return this.stream.Length; }
        }

        public void Write<T>(long position, ref T data) where T : struct
        {
            var bytes = StructureToByteArray(data);
            WriteArray(position, bytes);
        }

        public void WriteArray(long position, byte[] bytes)
        {
            stream.Seek(position, SeekOrigin.Begin);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Write(long position, int value)
        {
            WriteArray(position, BitConverter.GetBytes(value));
        }

        public void Resize(long newSize)
        {
            stream.SetLength(newSize);
        }

        public void Flush()
        {
            this.stream.Flush();
        }

        public void Dispose()
        {
            if (this.stream != null)
            {
                this.stream.Dispose();
            }

            this.stream = null;
        }

        private byte[] StructureToByteArray<T>(T obj)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        private void ByteArrayToStructure<T>(byte[] bytearray, ref T obj)
        {
            int len = Marshal.SizeOf(obj);

            IntPtr i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 0, i, len);

            obj = (T)Marshal.PtrToStructure(i, obj.GetType());

            Marshal.FreeHGlobal(i);
        }


        private static FileInfo PrepareFileForWriting(FileInfo fileInfo)
        {
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            return fileInfo;
        }
    }
}

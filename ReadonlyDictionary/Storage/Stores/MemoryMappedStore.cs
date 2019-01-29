namespace ReadonlyDictionary.Storage.Stores
{
    using Polly;
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;

    public class MemoryMappedStore : IRandomAccessStore
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        private readonly FileInfo fileInfo;

        public MemoryMappedStore(FileInfo fileInfo, long initialSize)
        {
            PrepareFileForWriting(fileInfo);
            this.fileInfo = fileInfo;
            this.mmf = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.CreateNew, fileInfo.Name, initialSize);

            this.accessor = this.mmf.CreateViewAccessor();
        }

        public MemoryMappedStore(FileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
            this.mmf = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.Open);
            this.accessor = this.mmf.CreateViewAccessor();
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

            this.accessor = null;
            this.mmf = null;
        }

        public T Read<T>(long position) where T : struct
        {
            T header;
            this.accessor.Read<T>(position, out header);
            return header;
        }

        public byte[] ReadArray(long position, int length)
        {
            byte[] bytes = new byte[length];
            this.accessor.ReadArray(position, bytes, 0, length);
            return bytes;
        }

        public long Capacity
        {
            get
            {
                return this.accessor.Capacity;
            }
        }

        public void WriteArray(long position, byte[] bytes)
        {
            this.accessor.WriteArray(position, bytes, 0, bytes.Length);
        }

        public void Write<T>(long position, ref T data) where T : struct
        {
            this.accessor.Write(position, ref data);
        }

        public void Write(long position, int value)
        {
            this.accessor.Write(position, value);
        }

        public void Flush()
        {
            this.accessor.Flush();
        }



        public void Resize(long newSize)
        {
            var fi = new FileInfo(this.fileInfo.FullName + "_" + newSize);

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
                    for (long i = 0; i < Math.Min(this.accessor.Capacity, newSize); i++)
                    {
                        newAccessor.Write(i, this.accessor.ReadByte(i));
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
                catch (Exception deleteException)
                {
                    // todo: log deleteException
                }

                throw;
            }

            this.Dispose();

            this.fileInfo.Delete();
            File.Move(fi.FullName, this.fileInfo.FullName);

            this.mmf = MemoryMappedFile.CreateFromFile(this.fileInfo.FullName, FileMode.Open);
            this.accessor = this.mmf.CreateViewAccessor();
        }


        public int ReadInt32(long index)
        {
            return this.accessor.ReadInt32(index);
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

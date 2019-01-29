namespace ReadonlyDictionary.Storage.Stores
{
    using Polly;

    public class PolicyStore : IRandomAccessStore
    {
        private readonly IRandomAccessStore source;
        private readonly Policy policy;

        public PolicyStore(IRandomAccessStore source, Policy policy)
        {
            this.source = source;
            this.policy = policy;
        }

        public long Capacity
        {
            get
            {
                return this.policy.Execute(() => this.source.Capacity);
            }
        }

        public void Dispose()
        {
            this.policy.Execute(() => this.source.Dispose());
        }

        public void Flush()
        {
            this.policy.Execute(() => this.source.Flush());
        }

        public T Read<T>(long position) where T : struct
        {
            return this.policy.Execute(() => this.source.Read<T>(position));
        }

        public byte[] ReadArray(long position, int length)
        {
            return this.policy.Execute(() => this.source.ReadArray(position, length));
        }

        public int ReadInt32(long index)
        {
            return this.policy.Execute(() => this.source.ReadInt32(index));
        }

        public void Resize(long newSize)
        {
            this.policy.Execute(() => this.source.Resize(newSize));
        }

        public void Write<T>(long position, ref T data)
            where T : struct
        {
            this.source.Write(position, ref data);
        }

        public void Write(long position, int value)
        {
            this.policy.Execute(() => this.source.Write(position, value));
        }

        public void WriteArray(long position, byte[] bytes)
        {
            this.policy.Execute(() => this.source.WriteArray(position, bytes));
        }
    }
}

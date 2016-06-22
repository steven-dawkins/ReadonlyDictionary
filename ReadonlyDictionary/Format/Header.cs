using System;

namespace ReadonlyDictionary.Format
{
    internal struct Header
    {
        public Guid magic; // magic guids, why not?
        public long Count;
        public long IndexPosition;
        public long DataPosition;
        public int IndexLength;
        public int Version;
        public SerializationStrategyEnum SerializationStrategy;

        public enum SerializationStrategyEnum { Custom = 0, Json, Protobuf };

        public static int CurrentVersion = 1;
        public static Guid expectedMagic = Guid.Parse("22E809B7-7EFD-4D83-936C-1F3F7780B615");
    }
}

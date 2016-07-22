using System;

namespace ReadonlyDictionary.Format
{
    internal struct Header
    {
        public enum SerializationStrategyEnum { Custom = 0, Json, Protobuf, JsonFlyWeight };

        public static int CurrentVersion = 2; // V2 adds SerializerJsonStart / SerializerJsonLength
        public static Guid expectedMagic = Guid.Parse("22E809B7-7EFD-4D83-936C-1F3F7780B615");

        public Guid magic; // magic guids, why not?
        public long Count;
        public long IndexPosition;
        public long DataPosition;
        public int IndexLength;
        public int Version;
        public SerializationStrategyEnum SerializationStrategy;
        public long SerializerJsonStart;
        public int SerializerJsonLength;

        //public int customBlockCount;
    }

    //unsafe internal struct CustomDataBlock
    //{
    //    public fixed char Name[256];
    //    public long Position;
    //    public int Length;
    //}

}

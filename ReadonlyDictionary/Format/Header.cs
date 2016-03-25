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

        public static Guid expectedMagic = Guid.Parse("22E809B7-7EFD-4D83-936C-1F3F7780B615");
    }
}

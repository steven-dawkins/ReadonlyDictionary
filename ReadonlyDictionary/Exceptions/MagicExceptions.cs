namespace ReadonlyDictionary.Exceptions
{
    using System;

    public class NoMagicException : Exception
    {
        public NoMagicException(string filename)
            : base("zeroed magic number in FileIndexKeyValueStorage file: " + filename)
        {
        }
    }

    public class InvalidMagicException : Exception
    {
        public InvalidMagicException(string filename)
            : base("unexpected magic number in FileIndexKeyValueStorage file: " + filename)
        {
        }
    }
}

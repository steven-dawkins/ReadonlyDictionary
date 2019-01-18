namespace ReadonlyDictionary.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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

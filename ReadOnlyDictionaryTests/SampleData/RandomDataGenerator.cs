namespace ReadonlyDictionaryTests.SampleData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RandomDataGenerator
    {
        public static readonly KeyValuePair<Guid, Book> theHobbit = new KeyValuePair<Guid, Book>(Guid.Parse("69CA35FD-FF92-4797-9E27-C875544E9D97"), new Book("The Hobbit", "", ""));
        public static readonly KeyValuePair<Guid, Book> theLordOfTheRings = new KeyValuePair<Guid, Book>(Guid.Parse("0B1A41BA-03B2-4293-8DCB-8494F3353668"), new Book("The Lord of the Rings", "", ""));

        public static IEnumerable<KeyValuePair<Guid, Book>> SampleData()
        {
            yield return theHobbit;
            yield return theLordOfTheRings;
        }

        public static IEnumerable<KeyValuePair<Guid, Book>> RandomData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new KeyValuePair<Guid, Book>(Guid.NewGuid(), new Book("Book - " + i, "Name2:" + i, "Name3:" + i));
            }
        }
    }

}

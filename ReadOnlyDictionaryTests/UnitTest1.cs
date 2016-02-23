using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadOnlyDictionary;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ReadOnlyDictionaryTests.SampleData;
using Newtonsoft.Json;
using System.Text;


namespace ReadOnlyDictionaryTests
{
    public abstract class TestBase
    {
        protected readonly KeyValuePair<Guid, Book> theHobbit = new KeyValuePair<Guid, Book>(Guid.Parse("69CA35FD-FF92-4797-9E27-C875544E9D97"), new Book("The Hobbit"));
        protected readonly KeyValuePair<Guid, Book> theLordOfTheRings = new KeyValuePair<Guid, Book>(Guid.Parse("0B1A41BA-03B2-4293-8DCB-8494F3353668"), new Book("The Lord of the Rings"));

        protected IEnumerable<KeyValuePair<Guid, Book>> SampleData()
        {
            yield return theHobbit;
            yield return theLordOfTheRings;
        }

        protected IEnumerable<KeyValuePair<Guid, Book>> RandomData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new KeyValuePair<Guid, Book>(Guid.NewGuid(), new Book("Book - " + i));
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            randomData = RandomData(100000).ToArray();
            StorageInialize();
        }

        public abstract void StorageInialize();

        protected KeyValuePair<Guid, Book>[] randomData;
        protected IKeyValueStore<Book> storage;

        [TestMethod]
        public void ExerciseRandomData()
        {
            Assert.AreEqual((uint)randomData.Length, storage.Count);

            for (int i = 0; i < randomData.Length; i++)
            {
                var item = randomData[i];
                Assert.AreEqual(item.Value, storage.Get(item.Key));
                Assert.AreEqual(null, storage.Get(Guid.NewGuid()));
            }
        }
    }

    [TestClass]
    public class DictionaryReadOnly : TestBase
    {
        [TestMethod]
        public void StaticDataTests()
        {
            var storage = new DictionaryReadOnlyKeyValueStorage<Book>(SampleData());

            Assert.AreEqual(storage.Count, (uint)2);
            Assert.AreEqual(storage.Get(theHobbit.Key), theHobbit.Value);
            Assert.AreEqual(storage.Get(theLordOfTheRings.Key), theLordOfTheRings.Value);
            Assert.AreEqual(storage.Get(Guid.NewGuid()), null);
        }

        public override void StorageInialize()
        {
            storage = new DictionaryReadOnlyKeyValueStorage<Book>(randomData);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorage : TestBase
    {
        public override void StorageInialize()
        {
            Func<Book, byte[]> serializer = book => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(book));
            Func<byte[], Book> deserializer = bytes => JsonConvert.DeserializeObject<Book>(Encoding.UTF8.GetString(bytes));

            storage = new FileIndexKeyValueStorage<Book>(randomData, "temp.raw", 100 * 1024 * 1024, serializer, deserializer, randomData.LongLength);
        }
    }
}

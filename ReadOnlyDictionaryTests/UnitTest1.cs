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
    public class RandomDataGenerator
    {
        public static readonly KeyValuePair<Guid, Book> theHobbit = new KeyValuePair<Guid, Book>(Guid.Parse("69CA35FD-FF92-4797-9E27-C875544E9D97"), new Book("The Hobbit"));
        public static readonly KeyValuePair<Guid, Book> theLordOfTheRings = new KeyValuePair<Guid, Book>(Guid.Parse("0B1A41BA-03B2-4293-8DCB-8494F3353668"), new Book("The Lord of the Rings"));

        public static IEnumerable<KeyValuePair<Guid, Book>> SampleData()
        {
            yield return theHobbit;
            yield return theLordOfTheRings;
        }

        public static IEnumerable<KeyValuePair<Guid, Book>> RandomData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new KeyValuePair<Guid, Book>(Guid.NewGuid(), new Book("Book - " + i));
            }
        }
    }

    public abstract class TestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            randomData = RandomDataGenerator.RandomData(100000).ToArray();
            StorageInitalize();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.storage.Dispose();
        }

        public abstract void StorageInitalize();

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
            var storage = new InMemoryKeyValueStorage<Book>(RandomDataGenerator.SampleData());

            Assert.AreEqual(storage.Count, (uint)2);
            Assert.AreEqual(storage.Get(RandomDataGenerator.theHobbit.Key), RandomDataGenerator.theHobbit.Value);
            Assert.AreEqual(storage.Get(RandomDataGenerator.theLordOfTheRings.Key), RandomDataGenerator.theLordOfTheRings.Value);
            Assert.AreEqual(storage.Get(Guid.NewGuid()), null);
        }

        public override void StorageInitalize()
        {
            storage = new InMemoryKeyValueStorage<Book>(randomData);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageJson : TestBase
    {
        public override void StorageInitalize()
        {
            var serializer = new ReadOnlyDictionary.JsonSerializer<Book>();

            using (var temp = new FileIndexKeyValueStorage<Book>(randomData, "temp.raw", 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            storage = new FileIndexKeyValueStorage<Book>("temp.raw", serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageProtobuf : TestBase
    {
        public override void StorageInitalize()
        {
            var serializer = new ReadOnlyDictionary.ProtobufSerializer<Book>();

            using (var temp = new FileIndexKeyValueStorage<Book>(randomData, "temp.raw", 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            storage = new FileIndexKeyValueStorage<Book>("temp.raw", serializer);
        }
    }
}

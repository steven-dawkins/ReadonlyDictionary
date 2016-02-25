using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadOnlyDictionary;
using System.Collections.Generic;
using System.Linq;
using ReadOnlyDictionaryTests.SampleData;
using ReadOnlyDictionary.Serialization;
using ReadOnlyDictionary.Storage;


namespace ReadOnlyDictionaryTests
{
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
        protected IKeyValueStore<Guid, Book> storage;

        [TestMethod]
        public void ExerciseRandomData()
        {
            Assert.AreEqual((uint)randomData.Length, storage.Count);

            for (int i = 0; i < randomData.Length; i++)
            {
                var item = randomData[i];
                Assert.AreEqual(item.Value, storage.Get(item.Key));
                Assert.AreEqual(default(Book), storage.Get(Guid.NewGuid()));
            }
        }
    }

    [TestClass]
    public class InMemoryStorageTests : TestBase
    {
        [TestMethod]
        public void StaticDataTests()
        {
            var storage = new InMemoryKeyValueStorage<Guid, Book>(RandomDataGenerator.SampleData());

            Assert.AreEqual(storage.Count, (uint)2);
            Assert.AreEqual(storage.Get(RandomDataGenerator.theHobbit.Key), RandomDataGenerator.theHobbit.Value);
            Assert.AreEqual(storage.Get(RandomDataGenerator.theLordOfTheRings.Key), RandomDataGenerator.theLordOfTheRings.Value);
            Assert.AreEqual(storage.Get(Guid.NewGuid()), default(Book));
        }

        public override void StorageInitalize()
        {
            storage = new InMemoryKeyValueStorage<Guid, Book>(randomData);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageJson : TestBase
    {
        public override void StorageInitalize()
        {
            var serializer = new JsonSerializer<Book>();

            using (var temp = new FileIndexKeyValueStorage<Guid, Book>(randomData, "temp.raw", 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            storage = new FileIndexKeyValueStorage<Guid, Book>("temp.raw", serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageProtobuf : TestBase
    {
        public override void StorageInitalize()
        {
            var serializer = new ProtobufSerializer<Book>();

            using (var temp = new FileIndexKeyValueStorage<Guid, Book>(randomData, "temp.raw", 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            storage = new FileIndexKeyValueStorage<Guid, Book>("temp.raw", serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageNetSerializer : TestBase
    {
        public override void StorageInitalize()
        {
            var serializer = new NetSerializer<Book>();

            using (var temp = new FileIndexKeyValueStorage<Guid, Book>(randomData, "temp.raw", 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            storage = new FileIndexKeyValueStorage<Guid, Book>("temp.raw", serializer);
        }
    }
}

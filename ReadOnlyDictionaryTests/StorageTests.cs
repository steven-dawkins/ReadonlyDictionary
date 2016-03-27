using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReadOnlyDictionary;
using System.Collections.Generic;
using System.Linq;
using ReadOnlyDictionaryTests.SampleData;
using ReadOnlyDictionary.Serialization;
using ReadOnlyDictionary.Storage;
using System.IO;
using System.Diagnostics;


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

            var timer = new Stopwatch();
            timer.Start();

            for (int i = 0; i < randomData.Length; i++)
            {
                var item = randomData[i];
                Assert.AreEqual(item.Value, storage.Get(item.Key));
                Assert.AreEqual(default(Book), storage.Get(Guid.NewGuid()));
            }

            Console.WriteLine(timer.ElapsedMilliseconds + "ms");
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

    public abstract class FileIndexKeyValueStorageBase : TestBase
    {
        protected void WriteStorage(
            ISerializer<Book> serializer, 
            FileIndexKeyValueStorage<Guid, Book>.AccessStrategy strategy = FileIndexKeyValueStorage<Guid,Book>.AccessStrategy.MemoryMapped)
        {
            using (var temp = FileIndexKeyValueStorage<Guid, Book>.Create(randomData, "temp.raw", 1 * 1024 * 1024, serializer, randomData.LongLength, strategy))
            {

            }

            storage = FileIndexKeyValueStorage<Guid, Book>.Open("temp.raw", serializer);
        }

    }

    [TestClass]
    public class FileIndexKeyValueStorageJson : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {
            var serializer = new JsonSerializer<Book>();

            WriteStorage(serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageProtobuf : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {
            var serializer = new ProtobufSerializer<Book>();

            WriteStorage(serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageProtobufStream : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {
            var serializer = new ProtobufSerializer<Book>();

            WriteStorage(serializer, FileIndexKeyValueStorage<Guid,Book>.AccessStrategy.Streams);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageNetSerializer : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {
            var serializer = new NetSerializer<Book>();

            WriteStorage(serializer);
        }

        private class TestException : Exception
        {

        }

        private IEnumerable<KeyValuePair<Guid, Book>> ExceptionalData()
        {
            yield return this.randomData[0];
            throw new TestException();
        }

        [TestMethod]
        public void TestExpandingPopulate()
        {
            var serializer = new NetSerializer<Book>();

            using (var temp = FileIndexKeyValueStorage<Guid, Book>.Create(RandomDataGenerator.RandomData(100000), "temp_expanding.raw", 1 * 1024, serializer, 100000))
            {

            }

            Assert.IsTrue(new FileInfo("temp_expanding.raw").Length > 1 * 1024);
        }

        [TestMethod]
        public void TestInterruptedPopulate()
        {
            var serializer = new NetSerializer<Book>();

            try
            {
                using (var temp = FileIndexKeyValueStorage<Guid, Book>.Create(ExceptionalData(), "temp_exceptional.raw", 1 * 1024 * 1024, serializer, 2))
                {

                }
            }
            catch (TestException)
            {
                Assert.IsFalse(File.Exists("temp_exceptional.raw"));
            }
        }
    }
}

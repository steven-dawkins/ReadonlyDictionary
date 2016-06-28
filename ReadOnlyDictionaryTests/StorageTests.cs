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
using Newtonsoft.Json;

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

            storage = FileIndexKeyValueStorage<Guid, Book>.Open("temp.raw", serializer: serializer);
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
    public class FileIndexKeyValueStorageJsonFlyweight : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {            
            var serializer = new JsonFlyweightSerializer<Book>();

            WriteStorage(serializer);
        }

        [TestMethod]
        public void TestFlyweightSerialization()
        {
            var serializer = new JsonFlyweightSerializer<Book>();
            var strategy = FileIndexKeyValueStorage<Guid, Book>.AccessStrategy.Streams;

            var data = RandomDataGenerator.RandomData(10000).ToArray();

            using (var temp = FileIndexKeyValueStorage<Guid, Book>.Create(data, "temp2.raw", 1 * 1024, serializer, 100000))
            {

            }
            
            using (var reader = FileIndexKeyValueStorage<Guid, Book>.Open("temp2.raw", strategy, null))            
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var item = data[i];
                    Assert.AreEqual(item.Value, reader.Get(item.Key));                    
                }
            }
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
        private static ProtobufSerializer<Book> serializer = new ProtobufSerializer<Book>();

        public override void StorageInitalize()
        {
            WriteStorage(serializer, FileIndexKeyValueStorage<Guid,Book>.AccessStrategy.Streams);
        }

        [TestMethod]
        public void TestDualRead()
        {
            var serializer = new NetSerializer<Book>();

            var data = RandomDataGenerator.RandomData(10000).ToArray();

            using (var temp = FileIndexKeyValueStorage<Guid, Book>.Create(data, "temp2.raw", 1 * 1024, serializer, 100000))
            {

            }

            using (var temp1 = FileIndexKeyValueStorage<Guid, Book>.Open("temp2.raw", FileIndexKeyValueStorage<Guid, Book>.AccessStrategy.Streams, serializer))
            using (var temp2 = FileIndexKeyValueStorage<Guid, Book>.Open("temp2.raw", FileIndexKeyValueStorage<Guid, Book>.AccessStrategy.Streams, serializer))
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var item = data[i];
                    Assert.AreEqual(item.Value, temp1.Get(item.Key));
                    Assert.AreEqual(item.Value, temp2.Get(item.Key));
                }
            }
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

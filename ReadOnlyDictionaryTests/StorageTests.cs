namespace ReadonlyDictionaryTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ReadonlyDictionary.Serialization;
    using ReadonlyDictionary.Storage;
    using ReadonlyDictionaryTests.SampleData;
    using BookStorage = ReadonlyDictionary.Storage.FileIndexKeyValueStorageBuilder<System.Guid, ReadonlyDictionaryTests.SampleData.Book>;

    public abstract class TestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            this.randomData = RandomDataGenerator.RandomData(100000).ToArray();
            this.StorageInitalize();
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
            Assert.AreEqual((uint)this.randomData.Length, this.storage.Count);

            var timer = new Stopwatch();
            timer.Start();

            // warmup pass
            for (int i = 0; i < this.randomData.Length; i++)
            {
                var item = this.randomData[i];
                Assert.AreEqual(item.Value, this.storage.Get(item.Key));
                Assert.AreEqual(default(Book), this.storage.Get(Guid.NewGuid()));
            }

            Console.WriteLine(this.GetType().Name + " pass1 " + timer.ElapsedMilliseconds + "ms");

            timer.Restart();

            for (int i = 0; i < this.randomData.Length; i++)
            {
                var item = this.randomData[i];
                Assert.AreEqual(item.Value, this.storage.Get(item.Key));
                Assert.AreEqual(default(Book), this.storage.Get(Guid.NewGuid()));
            }

            Console.WriteLine(this.GetType().Name + "pass2 " + timer.ElapsedMilliseconds + "ms");
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
            this.storage = new InMemoryKeyValueStorage<Guid, Book>(this.randomData);
        }
    }

    public abstract class FileIndexKeyValueStorageBase : TestBase
    {
        protected void WriteStorage(
            ISerializer<Book> serializer,
            BookStorage.AccessStrategy strategy = BookStorage.AccessStrategy.MemoryMapped)
        {
            using (var temp = BookStorage.Create(this.randomData, "temp.raw", 1 * 1024 * 1024, serializer, this.randomData.LongLength, strategy))
            {
            }

            this.storage = BookStorage.Open("temp.raw", serializer: serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageJson : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {
            var serializer = new JsonSerializer<Book>();

            this.WriteStorage(serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageJsonFlyweight : FileIndexKeyValueStorageBase
    {
        public override void StorageInitalize()
        {
            var serializer = new JsonFlyweightSerializer<Book>();

            this.WriteStorage(serializer);
        }

        [TestMethod]
        public void TestFlyweightSerialization()
        {
            var serializer = new JsonFlyweightSerializer<Book>();
            var strategy = BookStorage.AccessStrategy.Streams;

            var data = RandomDataGenerator.RandomData(10000).ToArray();

            using (var temp = BookStorage.Create(data, "temp2.raw", 1 * 1024, serializer, 100000))
            {
            }

            using (var reader = BookStorage.Open("temp2.raw", strategy, null))
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

            this.WriteStorage(serializer);
        }
    }

    [TestClass]
    public class FileIndexKeyValueStorageProtobufStream : FileIndexKeyValueStorageBase
    {
        private static ProtobufSerializer<Book> serializer = new ProtobufSerializer<Book>();

        public override void StorageInitalize()
        {
            this.WriteStorage(serializer, BookStorage.AccessStrategy.Streams);
        }

        [TestMethod]
        public void TestMetadata()
        {
            var additionalMetadata = new KeyValuePair<string, object>[]
            {
                new KeyValuePair<string, object>("Lorem", "Ipsum"),
                new KeyValuePair<string, object>("A", new Book("The Hobbit", "", "")),
            };

            var values = RandomDataGenerator.RandomData(1000);

            using (var temp = BookStorage.Create(values, "temp_metadata.raw", 1 * 1024, serializer, 100000, additionalMetadata: additionalMetadata))
            {
            }

            using (var temp = BookStorage.Open("temp_metadata.raw", serializer: serializer))
            {
                Assert.AreEqual(additionalMetadata[0].Value, temp.GetAdditionalData<string>(additionalMetadata[0].Key));
                Assert.AreEqual(additionalMetadata[1].Value, temp.GetAdditionalData<Book>(additionalMetadata[1].Key));
            }
        }
    }
}

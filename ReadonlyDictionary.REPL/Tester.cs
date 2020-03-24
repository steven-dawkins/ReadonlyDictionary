namespace ReadonlyDictionary.REPL
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using ReadonlyDictionary.Serialization;
    using ReadonlyDictionary.Storage;
    using ReadonlyDictionaryTests.SampleData;
    using Serilog;
    using BookStorage = ReadonlyDictionary.Storage.FileIndexKeyValueStorageBuilder<System.Guid, ReadonlyDictionaryTests.SampleData.Book>;

    public class Tester
    {
        private readonly ILogger logger;

        public Tester()
        {
            this.logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
        }

        public enum Mode
        {
            InMemory,
            FileIndexKeyValueStorageNewtonsoft,
            FileIndexKeyValueStorageProtobuf,
            FileIndexKeyValueStorageNetSerializer,
            FileIndexKeyValueStorageMarshal,
        }

        public void RandomData(string modeString, int count)
        {
            var randomData = RandomDataGenerator.RandomData(count).ToArray();

            Mode mode = (Mode)Enum.Parse(typeof(Mode), modeString);

            var store = this.CreateStore(randomData, mode);

            this.ExerciseStore(randomData, store, mode);
        }

        private IKeyValueStore<Guid, Book> CreateStore(KeyValuePair<Guid, Book>[] randomData, Mode mode)
        {
            using (this.logger.BeginTimedOperation("Create store with " + randomData.Length + " items", mode.ToString()))
            {
                switch (mode)
                {
                    case Mode.FileIndexKeyValueStorageNewtonsoft:
                        {
                            var serializer = new JsonSerializer<Book>();

                            return CreateFileIndexKeyValueStorage(randomData, serializer, "temp.raw");
                        }

                    case Mode.FileIndexKeyValueStorageProtobuf:
                        {
                            var serializer = new ProtobufSerializer<Book>();

                            return CreateFileIndexKeyValueStorage(randomData, serializer, "temp2.raw");
                        }

                    case Mode.FileIndexKeyValueStorageMarshal:
                        {
                            var serializer = new MarshalSerializer<Book>();

                            return CreateFileIndexKeyValueStorage(randomData, serializer, "temp4.raw");
                        }

                    case Mode.InMemory:
                        {
                            return new InMemoryKeyValueStorage<Guid, Book>(randomData);
                        }

                    default:
                        throw new Exception("Unexpected storage mode: " + mode);
                }
            }
        }

        public void RandomDataAll(int count)
        {
            var randomData = RandomDataGenerator.RandomData(count).ToArray();

            foreach (Mode mode in Enum.GetValues(typeof(Mode)))
            {
                var store = this.CreateStore(randomData, mode);

                this.ExerciseStore(randomData, store, mode);
            }
        }

        private void ExerciseStore(KeyValuePair<Guid, Book>[] randomData, IKeyValueStore<Guid, Book> store, Mode mode)
        {
            using (this.logger.BeginTimedOperation("Exercise store with " + randomData.Length + " items", mode.ToString()))
            {
                for (int i = 0; i < randomData.Length; i++)
                {
                    var item = randomData[i];

                    if (!item.Value.Equals(store.Get(item.Key)))
                    {
                        throw new Exception("Failed to retried: " + item.Key);
                    }
                }
            }
        }

        private static IKeyValueStore<Guid, Book> CreateFileIndexKeyValueStorage(KeyValuePair<Guid, Book>[] randomData, ISerializer<Book> serializer, string filename)
        {
            IKeyValueStore<Guid, Book> store;
            using (var temp = BookStorage.Create(randomData, filename, 100 * 1024 * 1024, serializer, randomData.LongLength))
            {
            }

            store = BookStorage.Open(filename);
            return store;
        }
    }
}

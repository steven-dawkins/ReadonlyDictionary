using ReadOnlyDictionary;
using ReadOnlyDictionary.Serialization;
using ReadOnlyDictionary.Storage;
using ReadOnlyDictionaryTests;
using ReadOnlyDictionaryTests.SampleData;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadonlyDictionary.REPL
{
    public class Tester
    {
        private readonly ILogger logger;

        public Tester()
        {
            this.logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.ColoredConsole()
                .CreateLogger();
        }

        public enum Mode { InMemory, FileIndexKeyValueStorageNewtonsoft, FileIndexKeyValueStorageProtobuf, FileIndexKeyValueStorageNetSerializer, FileIndexKeyValueStorageMarshal }

        public void RandomData(string modeString, int count)
        {
            var randomData = RandomDataGenerator.RandomData(count).ToArray();

            Mode mode = (Mode)Enum.Parse(typeof(Mode), modeString);

            var store = CreateStore(randomData, mode);

            ExerciseStore(randomData, store, mode);
        }

        private IKeyValueStore<Guid, Book> CreateStore(KeyValuePair<Guid, Book>[] randomData, Mode mode)
        {
            using (logger.BeginTimedOperation("Create store with " + randomData.Length + " items", mode.ToString()))
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
                    case Mode.FileIndexKeyValueStorageNetSerializer:
                        {
                            var serializer = new NetSerializer<Book>();

                            return CreateFileIndexKeyValueStorage(randomData, serializer, "temp3.raw");
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
                var store = CreateStore(randomData, mode);

                ExerciseStore(randomData, store, mode);
            }
        }

        private void ExerciseStore(KeyValuePair<Guid, Book>[] randomData, IKeyValueStore<Guid, Book> store, Mode mode)
        {
            using (logger.BeginTimedOperation("Exercise store with " + randomData.Length + " items", mode.ToString()))
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
            using (var temp = new FileIndexKeyValueStorage<Guid, Book>(randomData, filename, 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            store = new FileIndexKeyValueStorage<Guid, Book>(filename, serializer);
            return store;
        }
    }

    class Program
    {
        public class StorageWrapper<TKey, T>
        {
            private readonly IKeyValueStore<TKey, T> store;

            public StorageWrapper(IKeyValueStore<TKey, T> store)
            {
                this.store = store;
            }
        }

        static void Main(string[] args)
        {
            var repl = new Replify.ClearScriptRepl();

            var randomData = RandomDataGenerator.RandomData(100000).ToArray();
            var storage = new InMemoryKeyValueStorage<Guid, Book>(randomData);

            repl.AddHostObject("inmemory", storage);
            repl.AddHostObject("Mode", typeof(Tester.Mode));
            repl.AddHostObject("test", new Tester());

            repl.StartReplLoop(args);
        }
    }
}

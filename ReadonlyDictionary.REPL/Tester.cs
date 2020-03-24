using ReadonlyDictionary.Storage;
using ReadonlyDictionary.Serialization;
using ReadonlyDictionaryTests.SampleData;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

using BookStorage = ReadonlyDictionary.Storage.FileIndexKeyValueStorageBuilder<System.Guid, ReadonlyDictionaryTests.SampleData.Book>;
using BookStorageString = ReadonlyDictionary.Storage.FileIndexKeyValueStorageBuilder<System.String, ReadonlyDictionaryTests.SampleData.Book>;
using System.Diagnostics;
using System.Threading;

namespace ReadonlyDictionary.REPL
{
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

        public enum Mode { InMemory, FileIndexKeyValueStorageNewtonsoft, FileIndexKeyValueStorageProtobuf, FileIndexKeyValueStorageNetSerializer, FileIndexKeyValueStorageMarshal }

        public void RandomData(string modeString, int count)
        {
            var randomData = RandomDataGenerator.RandomDataStringyKeys(count).ToArray();

            Mode mode = (Mode)Enum.Parse(typeof(Mode), modeString);

            var store = CreateStore(randomData, mode);

            ExerciseStore(randomData, store, mode);
        }

        private IKeyValueStore<TKey, Book> CreateStore<TKey>(KeyValuePair<TKey, Book>[] randomData, Mode mode)
        {
            var sw = new Stopwatch();
            sw.Start();
            GC.Collect(3, GCCollectionMode.Forced, true, true);
            var originalMemoryUsage = Process.GetCurrentProcess().PrivateMemorySize64;

            using (logger.BeginTimedOperation("Create store with " + randomData.Length + " items", mode.ToString()))
            {
                try
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
                                return new InMemoryKeyValueStorage<TKey, Book>(randomData);
                            }
                        default:
                            throw new Exception("Unexpected storage mode: " + mode);
                    }
                }
                finally
                {
                    GC.Collect(3, GCCollectionMode.Forced, true, true);

                    Thread.Sleep(10000);

                    GC.Collect(3, GCCollectionMode.Forced, true, true);

                    var currentMemory = Process.GetCurrentProcess().PrivateMemorySize64;
                    var allocatedMemoryMb = (currentMemory - originalMemoryUsage) / (1024 * 1024);
                    logger.Information("{timingName} allocated {AllocatedMb}mb in {ElapsedMilliseconds}ms", "createStore", allocatedMemoryMb, sw.ElapsedMilliseconds);
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

        private void ExerciseStore<TKey>(KeyValuePair<TKey, Book>[] randomData, IKeyValueStore<TKey, Book> store, Mode mode)
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

        private static IKeyValueStore<TKey, Book> CreateFileIndexKeyValueStorage<TKey>(KeyValuePair<TKey, Book>[] randomData, ISerializer<Book> serializer, string filename)
        {
            IKeyValueStore<TKey, Book> store;
            using (var temp = FileIndexKeyValueStorageBuilder<TKey, Book>.Create(
                
                randomData, filename, 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            store = FileIndexKeyValueStorageBuilder<TKey, Book>.Open(filename);
            return store;
        }
    }
}

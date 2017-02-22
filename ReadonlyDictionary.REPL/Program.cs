using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReadonlyDictionary.Storage;
using ReadonlyDictionary.Serialization;
using ReadonlyDictionaryTests.SampleData;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BookStorage = ReadonlyDictionary.Storage.FileIndexKeyValueStorageBuilder<System.Guid, ReadonlyDictionaryTests.SampleData.Book>;

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
            using (var temp = BookStorage.Create(randomData, filename, 100 * 1024 * 1024, serializer, randomData.LongLength))
            {

            }

            store = BookStorage.Open(filename);
            return store;
        }
    }

    public class Loader : Replify.IReplCommand
    {
        public StorageWrapper<string, JObject> File(string filename)
        {
            var store = FileIndexKeyValueStorageBuilder<string, JObject>.Open(filename);

            return new StorageWrapper<string, JObject>(store);
        }
    }

    public class Converter : Replify.IReplCommand
    {
        public void ConvertToJson(string filename)
        {
            using (var store = FileIndexKeyValueStorageBuilder<string, JObject>.Open(filename, serializer: new JsonSerializer<JObject>()))
            {

                var everything = from key in store.GetKeys()
                                 select new { Key = key, Value = store.Get(key) };

                var everythingJson = JsonConvert.SerializeObject(everything, Formatting.Indented);

                File.WriteAllText(filename + ".keys.json", JsonConvert.SerializeObject(store.GetKeys()));
                File.WriteAllText(filename + ".json", everythingJson);
            }
        }
    }
    


    public class StorageWrapper<TKey, T>
    {
        private readonly IKeyValueStore<TKey, T> store;

        public StorageWrapper(IKeyValueStore<TKey, T> store)
        {
            this.store = store;
        }

        public TKey[] GetKeys()
        {
            return this.store.GetKeys().ToArray();
        }

        public T Get(TKey key)
        {
            return this.store.Get(key);
        }

        public IEnumerable<T> GetAll()
        {
            foreach(var key in GetKeys())
            {
                yield return Get(key);
            }
        }

        public long Count()
        {
            return this.GetAll().LongCount();
        }

        public string[] GetAdditionalDataKeys()
        {
            return this.store.GetAdditionalDataKeys().ToArray();
        }

        public JObject GetAdditionalDataObject(string key)
        {
            return this.store.GetAdditionalData<JObject>(key);
        }

        public JArray GetAdditionalDataArray(string key)
        {
            return this.store.GetAdditionalData<JArray>(key);
        }

        public override string ToString()
        {
            return store.ToString();
        }
    }

    class Program
    {
        //Load.File("C:\Temp\MandolineCache\IND_DB\aug136fdbin_mandoline.db.fcst.rawdic");
        

        static void Main(string[] args)
        {
            var repl = new Replify.ClearScriptRepl();

            var randomData = RandomDataGenerator.RandomData(100000).ToArray();
            var storage = new InMemoryKeyValueStorage<Guid, Book>(randomData);

            repl.AddHostObject("inmemory", storage);
            repl.AddHostObject("Mode", typeof(Tester.Mode));
            repl.AddHostObject("test", new Tester());
            repl.AddHostObject("Load", new Loader());

            repl.StartReplLoop(args);
        }
    }
}

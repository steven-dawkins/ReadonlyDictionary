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
            foreach (var key in this.GetKeys())
            {
                yield return this.Get(key);
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
            return this.store.ToString();
        }
    }

    internal class Program
    {
        // Load.File("C:\Temp\MandolineCache\IND_DB\aug136fdbin_mandoline.db.fcst.rawdic");

        private static void Main(string[] args)
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

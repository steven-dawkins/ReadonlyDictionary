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

    public class Loader : Replify.IReplCommand
    {
        public StorageWrapper<string, JObject> File(string filename)
        {
            var store = FileIndexKeyValueStorageBuilder<string, JObject>.Open(filename);

            return new StorageWrapper<string, JObject>(store);
        }
    }
}

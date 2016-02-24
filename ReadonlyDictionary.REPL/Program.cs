using ReadOnlyDictionary;
using ReadOnlyDictionaryTests;
using ReadOnlyDictionaryTests.SampleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadonlyDictionary.REPL
{
    class Program
    {
        static void Main(string[] args)
        {
            var repl = new Replify.ClearScriptRepl();

            var randomData = RandomDataGenerator.RandomData(100000).ToArray();
            var storage = new InMemoryKeyValueStorage<Book>(randomData);

            repl.AddHostObject("storage", storage);

            repl.StartReplLoop();
        }
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ReadonlyDictionary.Storage;
using ReadonlyDictionaryTests.SampleData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReadonlyDictionary.REPL
{
    [MemoryDiagnoser]
    public class TestBenchmark
    {
        public class TaskDelayMethods
        {
            [Benchmark]
            public void Task1()
            {
                var l = new List<Object>();
                for(int i = 0; i < 10000;i ++)
                {
                    var x = new Object();
                    l.Add(x);
                }
            }
        }
    }

    class Program
    {
        //Load.File("C:\Temp\MandolineCache\IND_DB\aug136fdbin_mandoline.db.fcst.rawdic");
        

        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TestBenchmark>();
            Console.WriteLine(summary.BenchmarksCases.Count());
            Console.ReadLine();
            return;

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

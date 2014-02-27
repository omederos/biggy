using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.SQLServer;

namespace Biggy.Perf.SQLDocument {
  class Benchmark {

    public static void Run() {
      Console.WriteLine("Connecting to SQL Document Store...");
      var monkies = new SQLDocumentList<Monkey>("northwind");
      monkies.Clear();
      var sw = new Stopwatch();

      sw.Start();
      var addRange = new List<Monkey>();
      for (int i = 0; i < 10000; i++) {
        addRange.Add(new Monkey { ID = i, Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      var inserted = monkies.AddRange(addRange);
      sw.Stop();
      Console.WriteLine("Just inserted {0} as documents in {1} ms", inserted, sw.ElapsedMilliseconds);


      Console.WriteLine("Loading 100,000 documents");
      sw.Reset();
      addRange.Clear();
      monkies.Clear();
      sw.Start();
      for (int i = 0; i < 100000; i++) {
        addRange.Add(new Monkey { ID = i, Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      inserted = monkies.AddRange(addRange);
      sw.Stop();
      Console.WriteLine("Just inserted {0} as documents in {1} ms", inserted, sw.ElapsedMilliseconds);


      //use a DB that has an int PK
      sw.Reset();
      sw.Start();
      Console.WriteLine("Loading {0}...", inserted);
      monkies.Reload();
      sw.Stop();
      Console.WriteLine("Loaded {0} documents from SQL Server in {1}ms", inserted, sw.ElapsedMilliseconds);

      sw.Reset();
      sw.Start();
      Console.WriteLine("Querying Middle 100 Documents");
      var found = monkies.Where(x => x.ID > 100 && x.ID < 500);
      sw.Stop();
      Console.WriteLine("Queried {0} documents in {1}ms", found.Count(), sw.ElapsedMilliseconds);


    }
  }
}

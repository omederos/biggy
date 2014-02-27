using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;
using Biggy.SQLServer;

namespace Biggy.Perf.SQLList {
  class Benchmarks {

    public static void Run() {
      var sw = new Stopwatch();
      Console.WriteLine("Loading up tracks from Chinook...");
      sw.Start();
      //use the dvds db
      var films = new SQLServerList<Track>("chinook","track","trackid");
      sw.Stop();
      Console.WriteLine("Loaded {0} records in {1}ms", films.Count(), sw.ElapsedMilliseconds);
    }

  }
}

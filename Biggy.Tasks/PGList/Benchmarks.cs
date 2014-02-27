using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;

namespace Biggy.Perf.PGList {
  class Benchmarks {

    public static void Run() {
      var sw = new Stopwatch();
      Console.WriteLine("Loading up films From Postgres Store");
      sw.Start();
      //use the dvds db
      var films = new PGList<Film>("dvds","film","film_id");
      sw.Stop();
      Console.WriteLine("Loaded {0} records in {1}ms", films.Count(), sw.ElapsedMilliseconds);

      sw.Reset();
      sw.Start();
      Console.WriteLine("Querying Middle 100 Documents");
      var found = films.Where(x => x.Film_ID > 10 && x.Film_ID < 500);
      sw.Stop();
      Console.WriteLine("Queried {0} records in {1}ms", found.Count(), sw.ElapsedMilliseconds);


    }

  }
}

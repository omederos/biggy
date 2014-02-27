using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Biggy.Extensions;
using System.Diagnostics;
using Biggy.SQLServer;
using Biggy.JSON;
using System.IO;
using Biggy.Postgres;

namespace Biggy.Perf {



  class Program {
    static void Main(string[] args) {

      PGDocuments.Benchmarks.Run();
      PGList.Benchmarks.Run();
      SQLDocument.Benchmark.Run();
      SQLList.Benchmarks.Run();
      Console.Read();

    }

  }
}

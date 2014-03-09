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
      FileStore.Benchmarks.Run();
      Console.WriteLine("**********************************************");
      PGDocuments.Benchmarks.Run();
      Console.WriteLine("**********************************************");
      PGList.Benchmarks.Run();
      Console.WriteLine("**********************************************");
      SQLDocument.Benchmarks.Run();
      Console.WriteLine("**********************************************");
      SQLList.Benchmarks.Run();
      Console.WriteLine("**********************************************");
      Console.WriteLine("DONE");
      Console.Read();

    }

  }
}

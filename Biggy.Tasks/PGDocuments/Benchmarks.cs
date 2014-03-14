using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Postgres;

namespace Biggy.Perf.PGDocuments {
  class Benchmarks {

    static string _connectionStringName = "chinookPG";

    public static void Run() {
      Console.WriteLine("===========================================================");
      Console.WriteLine("POSTGRES - LOAD A BUNCH OF DOCUMENTS INTO A TABLE");
      Console.WriteLine("===========================================================");

      Console.WriteLine("Connecting to Postgres Document Store...");

      // Start clean and fresh . . .
      if (Benchmarks.TableExists("clientdocuments"))
      {
        Benchmarks.DropTable("clientdocuments");
      }

      var _clientDocuments = new PGDocumentList<ClientDocument>(_connectionStringName);
      _clientDocuments.Clear();
      var sw = new Stopwatch();

      var addRange = new List<ClientDocument>();
      for (int i = 0; i < 10000; i++)
      {
        addRange.Add(new ClientDocument
        {
          LastName = "Conery " + i,
          FirstName = "Rob",
          Email = "rob@tekpub.com"
        });
      }
      sw.Start();
      var inserted = _clientDocuments.AddRange(addRange);
      sw.Stop();
      Console.WriteLine("\t Just inserted {0} as documents in {1} ms", inserted, sw.ElapsedMilliseconds);

      // Start clean and fresh again . . .
      _clientDocuments.Clear();
      addRange.Clear();
      Benchmarks.DropTable("clientdocuments");
      _clientDocuments = new PGDocumentList<ClientDocument>(_connectionStringName);
      sw.Reset();

      Console.WriteLine("Loading 100,000 documents");
      for (int i = 0; i < 100000; i++)
      {
        addRange.Add(new ClientDocument
        {
          LastName = "Conery " + i,
          FirstName = "Rob",
          Email = "rob@tekpub.com"
        });
      }
      sw.Start();
      inserted = _clientDocuments.AddRange(addRange);
      sw.Stop();
      Console.WriteLine("\t Just inserted {0} as documents in {1} ms", inserted, sw.ElapsedMilliseconds);


      //use a DB that has an int PK
      sw.Reset();
      Console.WriteLine("Loading {0}...", inserted);
      sw.Start();
      _clientDocuments.Reload();
      sw.Stop();
      Console.WriteLine("\t Loaded {0} documents from Postgres in {1} ms", inserted, sw.ElapsedMilliseconds);

      sw.Reset();
      Console.WriteLine("Querying Middle 100 Documents");
      sw.Start();
      var found = _clientDocuments.Where(x => x.ClientDocumentId > 100 && x.ClientDocumentId < 500);
      sw.Stop();
      Console.WriteLine("\t Queried {0} documents in {1}ms", found.Count(), sw.ElapsedMilliseconds);


      sw.Reset();
      Console.WriteLine("Adds Items in a loop, follows by Bulk Insert");
      if (TableExists("items")) {
        DropTable("items");
      }
      sw.Start();
      var list = new List<Item>();
      for (int i = 1; i < 6; i++) {
        list.Add(new Item() {
          Name = "Item no " + i
        });
      }
      var items = new PGDocumentList<Item>("chinookPG");
      // 1. Add items in a loop
      foreach (var item in list) {
        items.Add(item);
      }
      // 2. Add items using AddRange...
      items.AddRange(list);
      sw.Stop();
      Console.WriteLine("\t Added {0} items in a loop, then added same items as bullk insert in {1}", list.Count(), sw.ElapsedMilliseconds);
    }


    class Item
    {
      public int Id { get; set; }
      public string Name { get; set; }
    }

    static void DropTable(string tableName) {
      string sql = string.Format("DROP TABLE {0}", tableName);
      var Model = new PGTable<dynamic>(_connectionStringName);
      Model.Execute(sql);
    }


    static bool TableExists(string tableName) {
      bool exists = false;
      string select = ""
          + "SELECT * FROM information_schema.tables "
          + "WHERE table_schema = 'public' "
          + "AND  table_name = '{0}'";
      string sql = string.Format(select, tableName);
      var Model = new PGTable<dynamic>(_connectionStringName);
      var query = Model.Query<dynamic>(sql);
      if (query.Count() > 0) {
        exists = true;
      }
      return exists;
    }
  }
}

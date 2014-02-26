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

namespace Biggy.Tasks {

  //this is here as a bit of a playground... playing with the API etc...
  class NWProduct {
    public Guid Sku { get; set; }
    public String ProductName { get; set; }
    public Decimal UnitPrice { get; set; }

    public override bool Equals(object obj) {
      var p1 = (Product)obj;
      return this.Sku.Equals(p1.Sku);
    }

  }
  class OrderDetail {
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int OrderDetailID { get; set; }
  }

  class Product {
    public String Sku { get; set; }
    public String Name { get; set; }
    public Decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product() {
      this.CreatedAt = DateTime.Now;
    }

    public override bool Equals(object obj) {
      var p1 = (Product)obj;
      return this.Sku == p1.Sku;
    }

  }

  class Actor {
    public int Actor_ID { get; set; }
    public string First_Name { get; set; }
    public string Last_Name { get; set; }
    public string FullName {
      get {
        return this.First_Name + " " + this.Last_Name;
      }
    }
  }

  class Monkey {
    [PrimaryKey]
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [PGFullText]
    public string Description { get; set; }
  }

  class Program {
    static void Main(string[] args) {

      //RunBenchmarks();
      //TalkToPG();
      TalkToPGDocs();
      Console.Read();

    }

    static void TalkToPGDocs() {
      var monkies = new PGDocumentList<Monkey>("northwindPG");
      monkies.Clear();
      monkies.Add(new Monkey { Name = "CHUCKLES", Birthday = DateTime.Today, Description = "A Fine Young Monkey" });
      var m = monkies.First();
      Console.WriteLine(m.Name);
      m.Birthday = DateTime.Today.AddDays(12);
      m.Description = "Straight outta COMPTON";
      Console.WriteLine("Updated : {0}", monkies.Update(m));

      var sw = new Stopwatch();
      sw.Start();
      var addRange = new List<Monkey>();
      for (int i = 0; i < 1000; i++) {
        addRange.Add(new Monkey { Name = "MONKEY " + i, Birthday = DateTime.Today, Description = "The Monkey on my back" });
      }
      var inserted = monkies.AddRange(addRange);
      sw.Stop();
      Console.WriteLine("Just inserted {0} as documents in {1} ms", inserted, sw.ElapsedMilliseconds);
    }

    static void TalkToPG() {

      var actors = new PGList<Actor>("dvds", "actor", "actor_id");
      foreach (var actor in actors) {
        Console.WriteLine(actor.FullName);
      }

      var a1 = actors.FirstOrDefault(x => x.FullName == "Thora Temple");
      Console.WriteLine("****{0}*****",a1.FullName);


      var newActor = new Actor { First_Name = "Rob", Last_Name = "Conery" };
      actors.Add(newActor);

      var me = actors.FirstOrDefault(x => x.FullName == "Rob Conery");
      me.First_Name = "Steve";
      actors.Update(me);

      actors.Remove(me);

    }


    static void RunBenchmarks() {
      Console.WriteLine("Writing 1000 records sync");
      var products = new BiggyList<Product>();
      products.Clear();
      //File.Delete(products.DbPath);
      //products.Reload();
      //1000 writes?
      var sw = new Stopwatch();
      sw.Start();

      for (int i = 0; i < 1000; i++) {
        var p = new Product { Sku = "SKU" + i, Name = "Steve", Price = 12.00M };
        products.Add(p);
      }
      sw.Stop();

      Console.WriteLine("{0}ms", sw.ElapsedMilliseconds);
      sw.Reset();

      Console.WriteLine("Reading from records");
      sw.Start();
      var p2 = products.Where(x => x.Sku == "SKU22").FirstOrDefault();
      Console.WriteLine(p2.Sku);
      sw.Stop();
      Console.WriteLine("{0}ms", sw.ElapsedMilliseconds);



      Console.Read();
    }
  }
}

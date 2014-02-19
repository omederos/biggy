using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Biggy.Extensions;

namespace Biggy.Tasks {

  //this is here as a bit of a playground... playing with the API etc...


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

  class Program {
    static void Main(string[] args) {

      var p = new Product { Sku = "asdasd", Name = "Steve", Price = 12.00M };


      var products = new BiggyList<Product>();
      products.Saved += products_Saved;
      //products.Remove(p);

      products.Add(p);
      products.Save();


      dynamic db = new BiggyDB();

      db.Klonks.Add(p);
      db.Klonks.Save();

      //using (dynamic db = new BiggyDB()) {
      //  db.Floofs.Add(p);
      //  db.Floofs.Save();
      //}

      //var db = new BiggyDB();
      //db.Insert(thing);
      Console.Read();
    }

    static void products_Saved(object sender, EventArgs e) {
      var biggyEvents = (BiggyEventArgs<Product>)e;
      Console.WriteLine("Hey we have {0}", biggyEvents.Items.Count);
    }
  }
}

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

      Console.WriteLine("Writing 1000 records");
      var started = DateTime.Now;
      var products = new BiggyList<Product>();
      //1000 writes?
      for (int i = 0; i < 1000; i++) {
        var p = new Product { Sku = "SKU"+ i, Name = "Steve", Price = 12.00M };
        products.Add(p);
      }
      products.Save();

      var ended = DateTime.Now;
      TimeSpan ts = ended - started;
      Console.Write(ts.TotalMilliseconds);


      Console.WriteLine("Reading from records");
      started = DateTime.Now;
      var p2 = products.Where(x => x.Sku == "SKU22").FirstOrDefault();
      Console.WriteLine(p2.Sku);
      ended = DateTime.Now;
      ts = ended - started;
      Console.Write(ts.TotalMilliseconds);
      Console.Read();

    }
  }
}

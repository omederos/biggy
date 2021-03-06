﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Biggy.Extensions;
using System.Diagnostics;

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

      Console.WriteLine("Writing 1000 records sync");
      var sw = new Stopwatch();
      var products = new BiggyList<Product>();
      //1000 writes?
      for (int i = 0; i < 1000; i++) {
        var p = new Product { Sku = "SKU"+ i, Name = "Steve", Price = 12.00M };
        products.Add(p);
      }
      sw.Start();
      products.Save();
      sw.Stop();

      Console.WriteLine("{0}ms", sw.ElapsedMilliseconds);
      sw.Reset();

      Console.WriteLine("Resetting...");
      products.ClearAndSave();

      Console.WriteLine("Writing 1000 records async");
      
      //1000 writes?
      for (int i = 0; i < 1000; i++) {
        var p = new Product { Sku = "SKUDDY" + i, Name = "Steve", Price = 12.00M };
        products.Add(p);
      }
      sw.Start();
      products.SaveAsync();
      sw.Stop();

      Console.WriteLine("{0}ms",sw.ElapsedMilliseconds);
      sw.Reset();


      //1000 writes?
      Console.WriteLine("Writing 1000 records with write happening in a loop");

      sw.Start();
      for (int i = 0; i < 1000; i++) {
        var p = new Product { Sku = "SKU" + i, Name = "Steve", Price = 12.00M };
        products.Add(p);
        //bad idea... run it and see why
        products.Save();
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

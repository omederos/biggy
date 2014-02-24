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


  class Program {
    static void Main(string[] args) {

      RunBenchmarks();
      Console.Read();

    }
    static void TalkToPG() {
      //var table = new PGTable<Product>("northwindPG", "products", "productid");
      //var list = table.All<Product>();
      //foreach (var p in list) {
      //  Console.WriteLine(p.productid);
      //}
    }
    //static void WhatWhat() {

    //  var sw = new Stopwatch();
    //  sw.Start();
    //  var products = new MassiveList<NWProduct>(connectionStringName: "northwind", tableName: "products", primaryKeyName: "productid");
    //  sw.Stop();
    //  Console.WriteLine("Read " + products.Count + " into memory in " + sw.ElapsedMilliseconds + "ms");
    //  foreach (var p in products) {
    //    Console.WriteLine(p.Sku);
    //  }

    //  sw.Reset();
    //  sw.Start();
    //  var readOne = products.FirstOrDefault(x => x.ProductName == "Product24");
    //  Console.WriteLine(readOne.ProductName);
    //  sw.Stop();

    //  Console.WriteLine("Read single in " + sw.ElapsedMilliseconds + "ms");
    //  sw.Reset();
    //  sw.Start();
    //  var details = new MassiveList<OrderDetail>(connectionStringName: "northwind", tableName: "orderdetails", primaryKeyName: "orderdetailid");
    //  sw.Stop();

    //  Console.WriteLine("Read " + details.Count + " into memory in " + sw.ElapsedMilliseconds + "ms");


    //  Console.Read();
    //}

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

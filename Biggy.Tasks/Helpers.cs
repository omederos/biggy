using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy.Perf {

  class Track {
    public int TrackID { get; set; }
    public string Name { get; set; }
    public string Composer { get; set; }
    public decimal UnitPrice { get; set; }
  }

  public class Film {
    [PrimaryKey]
    public int Film_ID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime ReleaseYear { get; set; }
    public int Length { get; set; }

    [FullText]
    public string FullText { get; set; }
  }

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
    [PrimaryKey]
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
    public int ID { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    [FullText]
    public string Description { get; set; }
  }
}

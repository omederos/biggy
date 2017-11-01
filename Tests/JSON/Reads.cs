using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Xunit;
using Biggy.JSON;

namespace Tests {
  [Trait("Reading from disk","")]
  public class Reads {

    BiggyList<Product> _products;
    Product test1;

    public Reads() {
      var list1 = new BiggyList<Product>();
      test1 = new Product { Sku = "XXX", Name = "Steve's Stuffs", Price = 100.00M };
      list1.Add(test1);
      list1.Save();

      //this should read from the file
      _products = new BiggyList<Product>();
    }

    [Fact(DisplayName = "Will read from disk")]
    public void WillReadFromDisk() {
      Assert.False(_products.Count == 0);
    }
    [Fact(DisplayName = "Contains deserialized products")]
    public void ContainsDeserializedProducts() {
      Assert.True(_products.First() is Product);

    }
  }
}

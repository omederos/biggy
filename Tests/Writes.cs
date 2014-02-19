using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy;
using Xunit;

namespace Tests
{
  [Trait("Saving","")]
  public class Writes {

    BiggyList<Product> _products;
    Product test1;
    string dbPath;
    bool saveCalled = false;
    bool itemAddedCalled = false;
    
    public Writes() {
      //clear out the list
      _products = new BiggyList<Product>();

      //empty the db
      _products.ClearAndSave();


      test1 = new Product { Sku = "XXX", Name = "Steve's Stuffs", Price = 100.00M };
      _products.Saved += _products_Saved;
      _products.ItemAdded += _products_ItemAdded;
      _products.Add(test1);
      _products.Save();
    }

    void _products_ItemAdded(object sender, EventArgs e) {
      itemAddedCalled = true;
    }

    void _products_Saved(object sender, EventArgs e) {
      saveCalled = true;
    }

    [Fact(DisplayName = "Creates a DB called 'products'")]
    public void DbNameIsProducts() {
      Assert.True(_products.DbName == "products");
    }
    [Fact(DisplayName = "Writes to disk")]
    public void WritesToDisk() {
      Assert.True(File.Exists(_products.DbPath));
    }
    [Fact(DisplayName = "Fires a Saved event")]
    public void SaveEventCalled() {
      Assert.True(saveCalled);
    }
    [Fact(DisplayName = "Fires an item added event")]
    public void ItemAddedCalled() {
      Assert.True(itemAddedCalled);
    }

    [Fact(DisplayName = "Won't duplicate same item as determined by Equals")]
    public void WontDuplicate() {
      _products.ClearAndSave();
      _products.Add(test1);

      Assert.True(_products.Count == 1);

      //Product has Equals set to compare SKU
      var test2 = new Product { Sku = "XXX", Name = "Other Kine Stuffs", Price = 100.00M };
      _products.Add(test2);

      Assert.True(_products.Count == 1);
      //now be sure it's the updated product

      Assert.True(_products.First().Name == "Other Kine Stuffs");
    }

    [Fact(DisplayName = "Updating just works")]
    public void UpdatingJustWorks() {
      _products.ClearAndSave();
      _products.Add(test1);

      var p = _products.First();
      p.Name = "Flagrant craziness";
      _products.Save();

      //reload completely
      _products.Reload();

      Assert.Equal(_products.First().Name, "Flagrant craziness");

    }

  }
}

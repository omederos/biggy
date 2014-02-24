using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Biggy;
using Biggy.JSON;

namespace Web.Models {
  
  public static class DB {

    static BiggyList<Product> _products;
    static BiggyList<Customer> _customers;

    public static void Load(string appPath) {
      _products = new BiggyList<Product>(dbPath : appPath);
      _customers = new BiggyList<Customer>(dbPath: appPath);
    }

    public static BiggyList<Product> Products {
      get {
        return _products;
      }
    }
    public static BiggyList<Customer> Customers {
      get {
        return _customers;
      }
    }
  }
}
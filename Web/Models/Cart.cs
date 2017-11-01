using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.Models {
  public class Cart {

    public Product Item { get; set; }
    public int Quantity { get; set; }
    public DateTime DateAdded { get; set; }
  }
}
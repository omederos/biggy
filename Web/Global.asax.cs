using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Biggy;
using Biggy.JSON;
using Biggy.Postgres;
using Web.Models;

namespace Web {

  public class StoreDB {

    public BiggyList<Product> Products;
    public BiggyList<Customer> Customers;

    public StoreDB() {
      Products = new BiggyList<Product>(dbPath: HttpRuntime.AppDomainAppPath);
      Customers = new BiggyList<Customer>(dbPath: HttpRuntime.AppDomainAppPath);
    }
  }


  public class MvcApplication : System.Web.HttpApplication {

    public static StoreDB StoreDB { get; set; }
    
    protected void Application_Start() {
      MvcApplication.StoreDB = new StoreDB();
      AreaRegistration.RegisterAllAreas();
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);
      
    }
  }
}

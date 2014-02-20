using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Web.Models;

namespace Web {
  public class DbConfig {

    public static void LoadDb() {
      var currentDir = HttpRuntime.AppDomainAppPath;
      DB.Load(currentDir);
    }

  }
}
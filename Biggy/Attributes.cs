using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  public class FullTextAttribute : Attribute {

  }
  public class PrimaryKeyAttribute : Attribute {

  }

  public class DbColumnNameAttribute : Attribute {
    public string Name { get; protected set; }
    public DbColumnNameAttribute(string name) {
      this.Name = name;
    }
  }
}

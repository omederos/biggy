using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  public class PGFullTextAttribute : Attribute {

    public string ColumnName { get; set; }
    public PGFullTextAttribute(string columnName) {
      this.ColumnName = columnName;
    }

  }
}

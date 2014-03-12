using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Biggy
{
  public class DbColumnMapping
  {
    public DbColumnMapping(string columnName, string PropertyName, string delimitedColumnName) {
      this.ColumnName = columnName;
      this.PropertyName = PropertyName;
      this.DelimitedColumnName = delimitedColumnName;
    }
    public bool IsAutoIncementing { get; set; }
    public bool IsPrimaryKey { get; set; }
    public Type DataType { get; set; }
    public string ColumnName { get; protected set; }
    public string PropertyName { get; protected set; }
    public string DelimitedColumnName { get; protected set; }

  }
}

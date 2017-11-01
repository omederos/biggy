﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy
{
  public class DbColumnMappingLookup {
    Dictionary<string, DbColumnMapping> ByProperty;
    Dictionary<string, DbColumnMapping> ByColumn;
    string _delimiterFormatString = "{0}";

    public DbColumnMappingLookup(string NameDelimiterFormatString) {
      _delimiterFormatString = NameDelimiterFormatString;
      this.ByProperty = new Dictionary<string, DbColumnMapping>(StringComparer.InvariantCultureIgnoreCase);
      this.ByColumn = new Dictionary<string, DbColumnMapping>(StringComparer.InvariantCultureIgnoreCase);
    }

    public int Count()
    {
      return this.ByProperty.Count();
    }

    public DbColumnMapping Add(string columnName, string propertyName) {
      string delimited = string.Format(_delimiterFormatString, columnName);  
      var mapping = new DbColumnMapping(columnName, propertyName, delimited);

      // add the same instance to both dictionaries:
      this.ByColumn.Add(mapping.ColumnName, mapping);
      this.ByProperty.Add(mapping.PropertyName, mapping);
      return mapping;
    }

    public DbColumnMapping FindByColumn(string columnName) {
      DbColumnMapping mapping;
      this.ByColumn.TryGetValue(columnName, out mapping);
      return mapping;
    }

    public DbColumnMapping FindByProperty(string propertyName) {
      DbColumnMapping mapping;
      this.ByProperty.TryGetValue(propertyName, out mapping);
      return mapping;
    }

    public bool ContainsPropertyName(string propertyName)
    {
      return this.ByProperty.ContainsKey(propertyName);
    }

    public bool ContainsColumnName(string columnName)
    {
      return this.ByColumn.ContainsKey(columnName);
    }
  }
}

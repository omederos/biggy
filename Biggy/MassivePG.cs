using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Biggy.Extensions;

namespace Biggy.Massive {

  public class PGTable : DBTable {

    public PGTable(string connectionStringName,
      string tableName = "",
      string primaryKeyField = "",
      string descriptorField = "",
      bool pkIsIdentityColumn = true)
      : base(connectionStringName, tableName, primaryKeyField, descriptorField, pkIsIdentityColumn) { }


    public override System.Data.Common.DbConnection OpenConnection() {
      var result = new NpgsqlConnection(this.ConnectionString);
      result.Open();
      return result;
    }

    protected override string BuildSelect(string where, string orderBy, int limit) {
      string sql = "SELECT {0} FROM {1} ";
      if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : " WHERE " + where;
      }
      if (!String.IsNullOrEmpty(orderBy)) {
        sql += orderBy.Trim().StartsWith("order by", StringComparison.OrdinalIgnoreCase) ? orderBy : " ORDER BY " + orderBy;
      }

      if (limit > 0) {
        sql += " LIMIT " + limit;
      }

      return sql;
    }

    public override dynamic Insert(object o) {
      var ex = o.ToExpando();
      if (!IsValid(ex)) {
        throw new InvalidOperationException("Can't insert: " + String.Join("; ", Errors.ToArray()));
      }
      if (BeforeSave(ex)) {
        using (dynamic conn = OpenConnection()) {
          var cmd = CreateInsertCommand(ex);
          cmd.Connection = conn;
          cmd.ExecuteNonQuery();
          if (PkIsIdentityColumn) {
            cmd.CommandText = " RETURNING " + this.PrimaryKeyField + " as newId";
            // Work with expando as dictionary:
            var d = ex as IDictionary<string, object>;
            // Set the new identity/PK:
            d[PrimaryKeyField] = (int)cmd.ExecuteScalar();

            // If a non-anonymous type was passed, see if we can just assign
            // the new ID to the reference originally passed in:
            var props = o.GetType().GetProperties();
            if (props.Any(p => p.Name == PrimaryKeyField)) {
              var idField = props.First(p => p.Name == PrimaryKeyField);
              idField.SetValue(o, d[PrimaryKeyField]);
            }
          }
          Inserted(ex);
        }
        return ex;
      } else {
        return null;
      }
    }



  }
}

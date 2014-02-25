using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Biggy.Extensions;
using System.Data.Common;

namespace Biggy.Postgres {

  public class PGTable<T> : DBTable<T> where T : new() {


    public PGTable(string connectionStringName,string primaryKeyField)
      : base(connectionStringName, primaryKeyField) { }


    public PGTable(string connectionStringName,
      string tableName = "",
      string primaryKeyField = "id",
      bool pkIsIdentityColumn = true)
      : base(connectionStringName, tableName, primaryKeyField, pkIsIdentityColumn) { }


    protected override DbConnection OpenConnection() {
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
    protected override string GetSingleSelect(string where) {
      return string.Format("SELECT * FROM {0} WHERE {1} LIMIT 1", TableName, where);
    }
    public override string GetInsertReturnValueSQL() {
      return " RETURNING " + this.PrimaryKeyField + " as newId";
    }

  }
}

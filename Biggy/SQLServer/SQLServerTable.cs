using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Biggy.SQLServer {
  public class SQLServerTable<T> : DBTable<T> where T: new() {

    public SQLServerTable(string connectionStringName, string primaryKeyField)
      : base(connectionStringName, primaryKeyField) { }

    public SQLServerTable(string connectionStringName,
      string tableName = "",
      string primaryKeyField = "",
      bool pkIsIdentityColumn = true)
      : base(connectionStringName, tableName, primaryKeyField, pkIsIdentityColumn) { }

    protected override System.Data.Common.DbConnection OpenConnection() {
      var conn = new SqlConnection(this.ConnectionString);
      conn.Open();
      return conn;
    }

    protected override string BuildSelect(string where, string orderBy, int limit) {
      string sql = limit > 0 ? "SELECT TOP " + limit + " {0} FROM {1} " : "SELECT {0} FROM {1} ";
      if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : " WHERE " + where;
      }
      if (!String.IsNullOrEmpty(orderBy)) {
        sql += orderBy.Trim().StartsWith("order by", StringComparison.OrdinalIgnoreCase) ? orderBy : " ORDER BY " + orderBy;
      }
      return sql;
    }

    public override string GetInsertReturnValueSQL() {
      return "SELECT SCOPE_IDENTITY() as newID";
    }
  }
}

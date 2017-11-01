using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Dynamic;

namespace Biggy.SQLServer {
  public class SQLServerTable<T> : DBTable<T> where T: new() {

    public SQLServerTable(string connectionStringName)
      : base(connectionStringName) { }

    public SQLServerTable(string connectionStringName,
      string tableName = "",
      string primaryKeyField = "",
      bool pkIsIdentityColumn = true)
      : base(connectionStringName, tableName) { }

    internal override System.Data.Common.DbConnection OpenConnection() {
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
    protected override string GetSingleSelect(string where) {
      return string.Format("SELECT TOP 2 * FROM {0} WHERE {1}", TableName, where);
    }
    public override string GetInsertReturnValueSQL() {
      return "; SELECT SCOPE_IDENTITY() as newID";
    }

    /// <summary>
    /// Returns a dynamic PagedResult. Result properties are Items, TotalPages, and TotalRecords.
    /// </summary>
    public virtual dynamic Paged(string where = "", string orderBy = "", string columns = "*", int pageSize = 20, int currentPage = 1, params object[] args) {
      return BuildPagedResult(where: where, orderBy: orderBy, columns: columns, pageSize: pageSize, currentPage: currentPage, args: args);
    }

    public virtual dynamic Paged(string sql, string primaryKey, string where = "", string orderBy = "", string columns = "*", int pageSize = 20, int currentPage = 1, params object[] args) {
      return BuildPagedResult(sql, primaryKey, where, orderBy, columns, pageSize, currentPage, args);
    }

    private dynamic BuildPagedResult(string sql = "", string primaryKeyField = "", string where = "", string orderBy = "", string columns = "*", int pageSize = 20, int currentPage = 1, params object[] args) {
      dynamic result = new ExpandoObject();
      var countSQL = "";
      if (!string.IsNullOrEmpty(sql))
        countSQL = string.Format("SELECT COUNT({0}) FROM ({1}) AS PagedTable", primaryKeyField, sql);
      else
        countSQL = string.Format("SELECT COUNT({0}) FROM {1}", this.PrimaryKeyMapping.ColumnName, TableName);

      if (String.IsNullOrEmpty(orderBy)) {
        orderBy = string.IsNullOrEmpty(primaryKeyField) ? this.PrimaryKeyMapping.ColumnName : primaryKeyField;
      }

      if (!string.IsNullOrEmpty(where)) {
        if (!where.Trim().StartsWith("where", StringComparison.CurrentCultureIgnoreCase)) {
          where = " WHERE " + where;
        }
      }

      var query = "";
      if (!string.IsNullOrEmpty(sql))
        query = string.Format("SELECT {0} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {2}) AS Row, {0} FROM ({3}) AS PagedTable {4}) AS Paged ", columns, pageSize, orderBy, sql, where);
      else
        query = string.Format("SELECT {0} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {2}) AS Row, {0} FROM {3} {4}) AS Paged ", columns, pageSize, orderBy, TableName, where);

      var pageStart = (currentPage - 1) * pageSize;
      query += string.Format(" WHERE Row > {0} AND Row <={1}", pageStart, (pageStart + pageSize));
      countSQL += where;
      result.TotalRecords = Scalar(countSQL, args);
      result.TotalPages = result.TotalRecords / pageSize;
      if (result.TotalRecords % pageSize > 0)
        result.TotalPages += 1;
      result.Items = Query(string.Format(query, columns, TableName), args);
      return result;
    }


    protected override string DbDelimiterFormatString {
      get { return "[{0}]"; }
    }

    protected override bool columnIsAutoIncrementing(string columnName) {
      var select = "SELECT columnproperty(object_id('{0}'),'{1}','IsIdentity') AS IsIdentity";
      var sql = string.Format(select, this.TableName, columnName);
      var result = this.Scalar(sql);
      try {
        int value = Convert.ToInt32(result);
        if (value > 0) {
          return true;
        }
      } 
      catch (Exception) {
        return false;
      }
      return false;
    }
  }
}

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


    internal override DbConnection OpenConnection() {
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
      return string.Format("SELECT * FROM {0} WHERE {1} LIMIT 1", this.DelimitedTableName, where);
    }
    public override string GetInsertReturnValueSQL() {
      return " RETURNING " + this.DelimitedPkColumnName + " as newId";
    }

    public IEnumerable<T> FullTextOnTheFly(string query, params string[] columns) {
      var columnList = String.Join(" || ", columns);
      var sql = string.Format("SELECT * FROM {0} WHERE to_tsvector('english', {1}) @@ to_tsquery(@0);", this.DelimitedTableName, columnList);
      return Query<T>(sql, query);
    }

    public IEnumerable<T> FullText(string query) {

      //var sql = string.Format("SELECT * FROM {0} WHERE to_tsvector('english', {1}) @@ to_tsquery(@0);", TableName, columnList);
      //find the column
      var item = new T();
      var props = item.GetType().GetProperties();
      //we're looking for a PGFullText attribute
      string columnName = null;

      //HACK: This is horrible... but it works. Attribute.GetCustomAttribute doesn't work for some reason
      //I think it's because of assembly issues?
      foreach (var prop in props) {
        foreach (var att in prop.CustomAttributes) {
          if(att.AttributeType == typeof(FullTextAttribute)){
            columnName = prop.Name;
            break;
          }
        }
      }
      
      if(columnName == null){
        throw new InvalidOperationException("Can't find a PGFullText attribute on " + typeof(T).Name + " - please be sure to add that");
      }
      var sql = string.Format("select *, ts_rank_cd({2},to_tsquery(@0)) as rank from {1} where {2} @@ to_tsquery(@0) order by rank DESC;",query,this.DelimitedTableName,columnName);
      return Query<T>(sql, query);

    }

    protected override string DbDelimiterFormatString {
      get { return "\"{0}\""; }
    }
  }
}

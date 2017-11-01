using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;
using Newtonsoft.Json;

namespace Biggy.SQLServer {
  public class SQLDocumentList<T> : DBDocumentList<T> where T : new() {

    public SQLDocumentList(string connectionStringName) : base(connectionStringName) { }

    internal override string GetBaseName() {
      return base.GetBaseName();
    }

    public override void SetModel() {
      this.Model = new SQLServerTable<dynamic>(this.ConnectionStringName,tableName:this.TableName);
    }

    /// <summary>
    /// Drops all data from the table - BEWARE
    /// </summary>
    public override void Clear() {
      this.Model.Execute("DELETE FROM " + Model.DelimitedTableName);
      base.Clear();
    }

    internal override void TryLoadData() {
      try {
        this.Reload();
      } catch (System.Data.SqlClient.SqlException x) {
        if (x.Message.Contains("Invalid object name")) {

          //create the table
          var idType = Model.PrimaryKeyMapping.DataType == typeof(int) ? " int identity(1,1)" : "nvarchar(255)";
          string fullTextColumn = "";
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search nvarchar(MAX)";
          }
          var sql = string.Format("CREATE TABLE {0} ({1} {2} primary key not null, body nvarchar(MAX) not null {3});", Model.DelimitedTableName, Model.PrimaryKeyMapping.DelimitedColumnName, idType, fullTextColumn);
          this.Model.Execute(sql);
          //if (this.FullTextFields.Length > 0) {
          //  var indexSQL = string.Format("CREATE FULL TEXT INDEX ON {0}({1})",this.TableName,string.Join(",",this.FullTextFields));
          //  this.Model.Execute(indexSQL);
          //}
          TryLoadData();
        } else {
          throw;
        }
      }
    }
    /// <summary>
    /// Adds a single T item to the database
    /// </summary>
    /// <param name="item"></param>
    public override void Add(T item) {
      this.addItem(item);
      if (Model.PrimaryKeyMapping.IsAutoIncementing) {
        //// Sync the JSON ID with the serial PK:
        //var ex = this.SetDataForDocument(item);
        this.Update(item);
      }
    }

    internal void addItem(T item) {
      var expando = SetDataForDocument(item);
      var dc = expando as IDictionary<string, object>;
      var vals = new List<string>();
      var args = new List<object>();
      var index = 0;

      var keyColumn = dc.FirstOrDefault(x => x.Key.Equals(Model.PrimaryKeyMapping.PropertyName, StringComparison.OrdinalIgnoreCase));
      if (this.Model.PrimaryKeyMapping.IsAutoIncementing) {
        //don't update the Primary Key
        dc.Remove(keyColumn);
      }
      foreach (var key in dc.Keys) {
        vals.Add(string.Format("@{0}", index));
        args.Add(dc[key]);
        index++;
      }
      var sb = new StringBuilder();
      sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2}); SELECT SCOPE_IDENTITY() as newID;", Model.DelimitedTableName, string.Join(",", dc.Keys), string.Join(",", vals));
      var sql = sb.ToString();
      var newKey = this.Model.Scalar(sql, args.ToArray());
      //set the key
      this.Model.SetPrimaryKey(item, newKey);
      base.Add(item);
    }

    /// <summary>
    /// A high-performance bulk-insert that can drop 10,000 documents in about 500ms
    /// </summary>
    public override int AddRange(List<T> items) {
      // These are SQL Server Max values:
      const int MAGIC_SQL_PARAMETER_LIMIT = 2100;
      const int MAGIC_SQL_ROW_VALUE_LIMIT = 1000;
      int rowsAffected = 0;

      var first = items.First();

      string insertClause = "";
      var sbSql = new StringBuilder("");

      using (var connection = Model.OpenConnection()) {
        using (var tdbTransaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)) {
          var commands = new List<DbCommand>();
          // Lock the table, so nothing will disrupt the pk sequence:
          string lockTableSQL = string.Format("SELECT 1 FROM {0} WITH(TABLOCKX) ", Model.DelimitedTableName);
          DbCommand dbCommand = Model.CreateCommand(lockTableSQL, connection);
          dbCommand.Transaction = tdbTransaction;
          dbCommand.ExecuteNonQuery();

          int nextSerialPk = 0;
          if(Model.PrimaryKeyMapping.IsAutoIncementing) {
            // Now get the next Identity Id. ** Need to do this within the transaction/table lock scope **:
            // NOTE: The application must have ownership permission on the table to do this!!
            var sql_get_seq = string.Format("SELECT IDENT_CURRENT ('{0}' )", Model.DelimitedTableName);
            dbCommand.CommandText = sql_get_seq;
            // if this is a fresh sequence, the "seed" value is returned. We will assume 1:
            nextSerialPk = Convert.ToInt32(dbCommand.ExecuteScalar());
            if(nextSerialPk > 1) {
              nextSerialPk++;
            }
          }

          var paramCounter = 0;
          var rowValueCounter = 0;
          foreach (var item in items) {
            // Set the soon-to-be inserted serial int value:
            if (Model.PrimaryKeyMapping.IsAutoIncementing) {
              var props = item.GetType().GetProperties();
              var pk = props.First(p => p.Name == Model.PrimaryKeyMapping.PropertyName);
              pk.SetValue(item, nextSerialPk);
              nextSerialPk++;
            }
            // Set the JSON object, including the interpolated serial PK
            var itemEx = SetDataForDocument(item);
            var itemSchema = itemEx as IDictionary<string, object>;
            var sbParamGroup = new StringBuilder();
            if (itemSchema.ContainsKey(Model.PrimaryKeyMapping.PropertyName) && Model.PrimaryKeyMapping.IsAutoIncementing) {
              itemSchema.Remove(Model.PrimaryKeyMapping.PropertyName);
            }

            if (ReferenceEquals(item, first)) {
              var sbFieldNames = new StringBuilder();
              foreach (var field in itemSchema) {
                string mappedColumnName = Model.PropertyColumnMappings.FindByProperty(field.Key).DelimitedColumnName;
                sbFieldNames.AppendFormat("{0},", mappedColumnName);
              }
              var delimitedColumnNames = sbFieldNames.ToString().Substring(0, sbFieldNames.Length - 1);
              string stub = "INSERT INTO {0} ({1}) VALUES ";
              insertClause = string.Format(stub, Model.DelimitedTableName, string.Join(", ", delimitedColumnNames));
              sbSql = new StringBuilder(insertClause);
            }
            foreach (var key in itemSchema.Keys) {
              if (paramCounter + itemSchema.Count >= MAGIC_SQL_PARAMETER_LIMIT || rowValueCounter >= MAGIC_SQL_ROW_VALUE_LIMIT) {
                dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
                commands.Add(dbCommand);
                sbSql = new StringBuilder(insertClause);
                paramCounter = 0;
                rowValueCounter = 0;
                dbCommand = Model.CreateCommand("", connection);
                dbCommand.Transaction = tdbTransaction;
              }
              // FT SEARCH STUFF SHOULD GO HERE
              sbParamGroup.AppendFormat("@{0},", paramCounter.ToString());
              dbCommand.AddParam(itemSchema[key]);
              paramCounter++;
            }
            // Add the row params to the end of the sql:
            sbSql.AppendFormat("({0}),", sbParamGroup.ToString().Substring(0, sbParamGroup.Length - 1));
            rowValueCounter++;
          }

          dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
          commands.Add(dbCommand);
          try {
            foreach (var cmd in commands) {
              rowsAffected += cmd.ExecuteNonQuery();
            }
            tdbTransaction.Commit();
          }
          catch (Exception) {
            tdbTransaction.Rollback();
          }
        }
      }
      this.Reload();
      return rowsAffected;
    }


    /// <summary>
    /// Updates a single T item
    /// </summary>
    public override int Update(T item) {
      var expando = SetDataForDocument(item);
      var dc = expando as IDictionary<string, object>;
      //this.Model.Insert(expando);
      var index = 0;
      var sb = new StringBuilder();
      var args = new List<object>();
      sb.AppendFormat("UPDATE {0} SET ", Model.DelimitedTableName);
      foreach (var key in dc.Keys) {
        var stub = string.Format("{0}=@{1},", key, index);
        args.Add(dc[key]);
        index++;
        if (index == dc.Keys.Count)
          stub = stub.Substring(0, stub.Length - 1);
        sb.Append(stub);
      }
      sb.Append(";");
      var sql = sb.ToString();
      //this.Model.Execute(sql, args.ToArray());
      base.Update(item);
      return this.Model.Update(expando);
    }
  }
}

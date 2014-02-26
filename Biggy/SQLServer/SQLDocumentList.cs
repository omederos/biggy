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
      return base.GetBaseName().ToLower();
    }

    public override void SetModel() {
      this.Model = new SQLServerTable<dynamic>(this.ConnectionStringName,tableName:this.TableName,primaryKeyField:this.PrimaryKeyField, pkIsIdentityColumn: this.PKIsIdentity);
    }

    /// <summary>
    /// Drops all data from the table - BEWARE
    /// </summary>
    public override void Clear() {
      this.Model.Execute("DELETE FROM " + TableName);
      base.Clear();
    }

    internal override void TryLoadData() {
      try {
        this.Reload();
      } catch (System.Data.SqlClient.SqlException x) {
        if (x.Message.Contains("Invalid object name")) {

          //create the table
          var idType = this.PrimaryKeyType == typeof(int) ? " int identity(1,1)" : "nvarchar(255)";
          string fullTextColumn = "";
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search nvarchar(MAX)";
          }
          var sql = string.Format("CREATE TABLE {0} ({1} {2} primary key not null, body nvarchar(MAX) not null {3});", this.TableName, this.PrimaryKeyField, idType, fullTextColumn);
          this.Model.Execute(sql);
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
      var expando = SetDataForDocument(item);
      var dc = expando as IDictionary<string, object>;
      var vals = new List<string>();
      var args = new List<object>();
      var index = 0;

      var keyColumn = dc.FirstOrDefault(x => x.Key.Equals(this.PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
      if (this.Model.PkIsIdentityColumn) {
        //don't update the Primary Key
        dc.Remove(keyColumn);
      }
      foreach (var key in dc.Keys) {
        vals.Add(string.Format("@{0}", index));
        args.Add(dc[key]);
        index++;
      }
      var sb = new StringBuilder();
      sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2}); SELECT SCOPE_IDENTITY() as newID;", this.TableName, string.Join(",", dc.Keys), string.Join(",", vals));
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
      //HACK: Refactor this to be prettier and also use a Transaction
      const int MAGIC_PG_PARAMETER_LIMIT = 2100;

      // ?? Unknown. Set this arbitrarily for now, haven't run into a limit yet. 
      const int MAGIC_PG_ROW_VALUE_LIMIT = 1000; 

      string stub = "INSERT INTO {0} ({1}) VALUES ";
      var first = items.First();
      var expando = this.SetDataForDocument(first);
      var schema = expando as IDictionary<string, object>;

      var keyColumn = schema.FirstOrDefault(x => x.Key.Equals(this.PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
      
      //HACK: I don't like this duplication here and below... we'll refactor at some point :)
      if (this.Model.PkIsIdentityColumn) {
        //don't update the Primary Key
        schema.Remove(keyColumn);
      }


      var insertClause = string.Format(stub, this.TableName, string.Join(", ", schema.Keys));
      var sbSql = new StringBuilder(insertClause);

      var paramCounter = 0;
      var rowValueCounter = 0;
      var commands = new List<DbCommand>();
      
      var conn = Model.OpenConnection();

      // Use the SAME connection, don't hit the pool for each command. 
      DbCommand dbCommand = Model.CreateCommand("", conn);

      foreach (var item in items) {
        var itemEx = SetDataForDocument(item);
        var itemSchema = itemEx as IDictionary<string, object>;
        var sbParamGroup = new StringBuilder();

        if (this.Model.PkIsIdentityColumn) {
          //don't update the Primary Key
          itemSchema.Remove(keyColumn);
        }

        foreach (var key in itemSchema.Keys) {
          // Things explode if you exceed the param limit for pg:
          if (paramCounter + schema.Count >= MAGIC_PG_PARAMETER_LIMIT || rowValueCounter >= MAGIC_PG_ROW_VALUE_LIMIT) {
            // Add the current command to the list, then start over with another:
            dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
            commands.Add(dbCommand);
            sbSql = new StringBuilder(insertClause);
            paramCounter = 0;
            rowValueCounter = 0;
            dbCommand = Model.CreateCommand("", conn);
          }
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

      int rowsAffected = 0;
      foreach (var cmd in commands) {
        rowsAffected += Model.Execute(cmd);
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
      sb.AppendFormat("UPDATE {0} SET ", this.TableName);
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
      this.Model.Execute(sql, args.ToArray());
      base.Update(item);
      return this.Model.Update(expando);
    }

  }
}

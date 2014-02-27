using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Biggy.Extensions;
using System.Data.Common;
using System.Collections.Specialized;


namespace Biggy.Postgres {

  /// <summary>
  /// A Document Store using Postgres' json data type. Has FullText support as well.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class PGDocumentList<T> : DBDocumentList<T> where T : new(){

    public PGDocumentList(string connectionStringName) : base(connectionStringName) { }

    internal override string GetBaseName() {
      return base.GetBaseName().ToLower();
    }

    public override void SetModel() {
      this.Model = new PGTable<dynamic>(this.ConnectionStringName,tableName:this.TableName,primaryKeyField:this.PrimaryKeyField, pkIsIdentityColumn: this.PKIsIdentity);
    }
    internal override void TryLoadData() {
      try {
        this.Reload();
      } catch (Npgsql.NpgsqlException x) {
        if (x.Message.Contains("does not exist")) {

          //create the table
          var idType = this.PrimaryKeyType == typeof(int) ? " serial" : "varchar(255)";
          string fullTextColumn = "";
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search tsvector";
          }
          var sql = string.Format("CREATE TABLE {0} ({1} {2} primary key not null, body json not null {3});", this.TableName, this.PrimaryKeyField, idType, fullTextColumn);
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
      var expando = base.SetDataForDocument(item);
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
        if (key == "search") {
          vals.Add(string.Format("to_tsvector(@{0})", index));
        } else {
          vals.Add(string.Format("@{0}", index));
        }
        args.Add(dc[key]);
        index++;
      }
      var sb = new StringBuilder();
      sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2}) RETURNING {3} as newID;", this.TableName, string.Join(",", dc.Keys), string.Join(",", vals), this.PrimaryKeyField);
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
        keyColumn = itemSchema.FirstOrDefault(x => x.Key.Equals(this.PrimaryKeyField, StringComparison.OrdinalIgnoreCase));

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
          if (key == "search") {
            sbParamGroup.AppendFormat("to_tsvector(@{0}),", paramCounter.ToString());
          } else {
            sbParamGroup.AppendFormat("@{0},", paramCounter.ToString());
          }
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
        if (key == "search") {
          stub = string.Format("{0}=to_tsvector(@{1}),", key, index);
        }
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

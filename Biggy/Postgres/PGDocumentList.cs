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
      this.Model = new PGTable<dynamic>(this.ConnectionStringName,tableName:this.TableName);
    }
    internal override void TryLoadData() {
      try {
        this.Reload();
      } catch (Npgsql.NpgsqlException x) {
        if (x.Message.Contains("does not exist")) {

          //create the table
          var idType = Model.PrimaryKeyMapping.DataType == typeof(int) ? " serial" : "varchar(255)";
          string fullTextColumn = "";
          if (this.FullTextFields.Length > 0) {
            fullTextColumn = ", search tsvector";
          }
          var sql = string.Format("CREATE TABLE {0} ({1} {2} primary key not null, body json not null {3});", Model.DelimitedTableName, Model.PrimaryKeyMapping.DelimitedColumnName, idType, fullTextColumn);
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
      this.addItem(item);
      if(Model.PrimaryKeyMapping.IsAutoIncementing) {
        //// Sync the JSON ID with the serial PK:
        //var ex = this.SetDataForDocument(item);
        this.Update(item);
      }
    }

    internal void addItem(T item) {
      var expando = base.SetDataForDocument(item);
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
        if (key == "search") {
          vals.Add(string.Format("to_tsvector(@{0})", index));
        } else {
          vals.Add(string.Format("@{0}", index));
        }
        args.Add(dc[key]);
        index++;
      }
      var sb = new StringBuilder();
      sb.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2}) RETURNING {3} as newID;", Model.DelimitedTableName, string.Join(",", dc.Keys), string.Join(",", vals), Model.PrimaryKeyMapping.DelimitedColumnName);
      var sql = sb.ToString();
      var newKey = this.Model.Scalar(sql, args.ToArray());
      //set the key
      this.Model.SetPrimaryKey(item, newKey);
      base.Add(item);
    }

    /// <summary>
    /// A high-performance bulk-insert that can drop 10,000 documents in about 900 ms
    /// </summary>
    public override int AddRange(List<T> items) {
      const int MAGIC_PG_PARAMETER_LIMIT = 2100;
      const int MAGIC_PG_ROW_VALUE_LIMIT = 1000;
      int rowsAffected = 0;

      var first = items.First();
      string insertClause = "";
      var sbSql = new StringBuilder("");

      using (var connection = Model.OpenConnection()) {
        using (var tdbTransaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead)) {
          var commands = new List<DbCommand>();
          // Lock the table, so nothing will disrupt the pk sequence:
          string lockTableSQL = string.Format("LOCK TABLE {0} in ACCESS EXCLUSIVE MODE", Model.DelimitedTableName);
          DbCommand dbCommand = Model.CreateCommand(lockTableSQL, connection);
          dbCommand.Transaction = tdbTransaction;
          dbCommand.ExecuteNonQuery();

          int nextSerialPk = 0;
          if(Model.PrimaryKeyMapping.IsAutoIncementing) {
            // Now get the next serial Id. ** Need to do this within the transaction/table lock scope **:
            string sequence = string.Format("\"{0}_{1}_seq\"", this.TableName, Model.PrimaryKeyMapping.ColumnName);
            var sql_get_seq = string.Format("SELECT last_value FROM {0}", sequence);
            dbCommand.CommandText = sql_get_seq;
            // if this is a fresh sequence, the "seed" value is returned. We will assume 1:
            nextSerialPk = Convert.ToInt32(dbCommand.ExecuteScalar());
            // If this is not a fresh sequence, increment:
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
              if (paramCounter + itemSchema.Count >= MAGIC_PG_PARAMETER_LIMIT || rowValueCounter >= MAGIC_PG_ROW_VALUE_LIMIT) {
                dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
                commands.Add(dbCommand);
                sbSql = new StringBuilder(insertClause);
                paramCounter = 0;
                rowValueCounter = 0;
                dbCommand = Model.CreateCommand("", connection);
                dbCommand.Transaction = tdbTransaction;
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
          try {
            foreach (var cmd in commands) {
              rowsAffected += cmd.ExecuteNonQuery();
            }
            tdbTransaction.Commit();
          } catch (Exception) {
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
      //this.Model.Execute(sql, args.ToArray());
      base.Update(item);
      return this.Model.Update(expando);
    }

  }
}

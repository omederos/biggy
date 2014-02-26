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
  public class PGDocumentList<T> : InMemoryList<T> where T : new(){

    public string TableName { get; set; }
    public PGTable<dynamic> Model { get; set; }
    public string PrimaryKeyField { get; set; }
    public Type PrimaryKeyType { get; set; }
    public string[] FullTextFields { get; set; }
    string BaseName {
      get {
        return typeof(T).Name.ToLower();
      }
    }

    public PGDocumentList(string connectionStringName) {
      DecideTableName();
      AssureKeyForT();
      SetFullTextColumns();
      var pkIsIdentity = this.PrimaryKeyType == typeof(int);
      this.Model = new PGTable<dynamic>(connectionStringName, this.TableName, this.PrimaryKeyField, pkIsIdentity);
      TryLoadData();
    }

    void DecideTableName() {
      //use the type name
      this.TableName = Inflector.Inflector.Pluralize(BaseName).ToLower();
    }

    void SetFullTextColumns() {
      var foundProps = new T().LookForCustomAttribute(typeof(PGFullTextAttribute));
      this.FullTextFields = foundProps.Select(x => x.Name).ToArray();
    }

    void AssureKeyForT() {
      var acceptableKeys = new string[]{"ID", this.BaseName + "ID"};
      var props = typeof(T).GetProperties();
      var conventionalKey = props.FirstOrDefault(x => x.Name.Equals("id",StringComparison.OrdinalIgnoreCase)) ??
         props.FirstOrDefault(x => x.Name.Equals(this.BaseName + "ID" ,StringComparison.OrdinalIgnoreCase));

      if (conventionalKey == null) {
        //HACK: This is horrible... but it works. Attribute.GetCustomAttribute doesn't work for some reason
        //I think it's because of assembly issues?
        var foundProp = new T().LookForCustomAttribute(typeof(PrimaryKeyAttribute)).FirstOrDefault();
        if(foundProp != null){
          this.PrimaryKeyField = foundProp.Name;
          this.PrimaryKeyType = foundProp.PropertyType;
        }
      } else {
        this.PrimaryKeyType = typeof(int);
        
      }

      if (String.IsNullOrWhiteSpace(this.PrimaryKeyField)) {
        throw new InvalidOperationException("Can't tell what the primary key is. You can use ID, " + BaseName + "ID, or specify with the PrimaryKey attribute");
      }

    }

    /// <summary>
    /// Drops all data from the table - BEWARE
    /// </summary>
    public override void Clear() {
      this.Model.Execute("DELETE FROM " + TableName);
      base.Clear();
    }

    void TryLoadData() {
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
    /// Reloads the internal memory list
    /// </summary>
    public void Reload() {
      _items = this.Model.All<T>().ToList();
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
    public int AddRange(List<T> items) {
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


    ExpandoObject SetDataForDocument(T item) {
      var json = JsonConvert.SerializeObject(item);
      var key = this.Model.GetPrimaryKey(item);
      var expando = new ExpandoObject();
      var dict = expando as IDictionary<string, object>;
      
      dict[PrimaryKeyField] = key;
      dict["body"] = json;

      if (this.FullTextFields.Length > 0) {
        //get the data from the item passed in
        var itemdc = item.ToDictionary();
        var vals = new List<string>();
        foreach (var ft in this.FullTextFields) {
          var val = itemdc[ft] == null ? "" : itemdc[ft].ToString();
          vals.Add(val);
        }
        dict["search"] = string.Join(",", vals);
      }
      return expando;
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

    /// <summary>
    /// Removes a document from the database
    /// </summary>
    public override bool Remove(T item) {
      var key = this.Model.GetPrimaryKey(item);
      this.Model.Delete(key);
      return base.Remove(item);
    }
  }
}

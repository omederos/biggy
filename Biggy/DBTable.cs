using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Biggy.Extensions;

namespace Biggy
{


  /// <summary>
  /// A class that wraps your database table in Dynamic Funtime
  /// </summary>
  public abstract class DBTable<T> where T: new() {
    
    protected string ConnectionString;


    public DBTable(string connectionStringName, string primaryKeyField) {
      var thingyType = this.GetType().GenericTypeArguments[0].Name;
      this.TableName = Inflector.Inflector.Pluralize(thingyType);
      this.PkIsIdentityColumn = true;
      this.PrimaryKeyField = primaryKeyField;
      ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;

    }

    public DBTable(string connectionStringName, string tableName = "",
      string primaryKeyField = "", bool pkIsIdentityColumn = true)
    {
      TableName = tableName == "" ? this.GetType().Name : tableName;
      PrimaryKeyField = string.IsNullOrEmpty(primaryKeyField) ? "ID" : primaryKeyField;
      PkIsIdentityColumn = pkIsIdentityColumn;
      ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
    }

    /// <summary>
    /// Conventionally introspects the object passed in for a field that 
    /// looks like a PK. If you've named your PrimaryKeyField, this becomes easy
    /// </summary>
    public bool HasPrimaryKey(object o)
    {
      return o.ToDictionary().ContainsKey(PrimaryKeyField);
    }

    /// <summary>
    /// If the object passed in has a property with the same name as your PrimaryKeyField
    /// it is returned here.
    /// </summary>
    public object GetPrimaryKey(object o)
    {
      object result = null;
      var lookup = o.ToDictionary();
      var found = lookup.FirstOrDefault(x => x.Key.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
      result = found.Value;
      return result;
    }

    public void SetPrimaryKey(T item, object value) {
      var props = item.GetType().GetProperties();
      if (item is ExpandoObject) {
        var d = item as IDictionary<string, object>;
        d[PrimaryKeyField] = value;
      } else {
        var pkProp = props.FirstOrDefault(x => x.Name.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
        var converted = Convert.ChangeType(value, pkProp.PropertyType);
        pkProp.SetValue(item, converted);

      }
    }

    public virtual string PrimaryKeyField { get; set; }
    public virtual bool PkIsIdentityColumn { get; set; }
    public virtual string TableName { get; set; }
    public string DescriptorField { get; protected set; }


    public IEnumerable<T> Where(string where, params object[] args) {
      var sql = BuildSelect(where, "", -1);
      var formatted = string.Format(sql, "*", this.TableName);
      return Query<T>(formatted, args);
    }

    /// <summary>
    /// Enumerates the reader yielding the result - thanks to Jeroen Haegebaert
    /// </summary>
    public IEnumerable<dynamic> Query(string sql, params object[] args) {
      using (var conn = OpenConnection()) {
        var rdr = CreateCommand(sql, conn, args).ExecuteReader();
        while (rdr.Read()) {
          var expando = rdr.RecordToExpando();
          yield return expando;
        }
      }
    }

    /// <summary>
    /// Enumerates the reader yielding the result - thanks to Jeroen Haegebaert
    /// </summary>
    public IEnumerable<T> Query<T>(string sql, params object[] args) where T : new() {
      using (var conn = OpenConnection()) {
        var rdr = CreateCommand(sql, conn, args).ExecuteReader();
        while (rdr.Read()) {
          yield return rdr.ToSingle<T>();
        }
      }
    }

    public IEnumerable<T> Query<T>(string sql, DbConnection connection, params object[] args) where T : new() {
      using (var rdr = CreateCommand(sql, connection, args).ExecuteReader()) {
        while (rdr.Read()) {
          yield return rdr.ToSingle<T>();
        }
      }
    }


    /// <summary>
    /// Returns a single result
    /// </summary>
    public object Scalar(string sql, params object[] args) {
      object result = null;
      using (var conn = OpenConnection()) {
        result = CreateCommand(sql, conn, args).ExecuteScalar();
      }
      return result;
    }

    /// <summary>
    /// Creates a DBCommand that you can use for loving your database.
    /// </summary>
    public DbCommand CreateCommand(string sql, DbConnection conn, params object[] args) {
      conn = conn ?? OpenConnection();
      var result = (DbCommand)conn.CreateCommand();
      result.CommandText = sql;
      if (args.Length > 0) {
        result.AddParams(args);
      }
      return result;
    }

    /// <summary>
    /// Returns and OpenConnection
    /// </summary>
    internal abstract DbConnection OpenConnection();

    /// <summary>
    /// Builds a set of Insert and Update commands based on the passed-on objects.
    /// These objects can be POCOs, Anonymous, NameValueCollections, or Expandos. Objects
    /// With a PK property (whatever PrimaryKeyField is set to) will be created at UPDATEs
    /// </summary>
    public List<DbCommand> BuildCommands(params T[] things) {
      var commands = new List<DbCommand>();
      foreach (var item in things) {
        if (HasPrimaryKey(item)) {
          commands.Add(CreateUpdateCommand(item));
        } else {
          commands.Add(CreateInsertCommand(item.ToExpando()));
        }
      }
      return commands;
    }

    public int Execute(DbCommand command) {
      return Execute(new DbCommand[] { command });
    }

    public int Execute(string sql, params object[] args) {
      return Execute(CreateCommand(sql, null, args));
    }

    /// <summary>
    /// Executes a series of DBCommands in a transaction
    /// </summary>
    public int Execute(IEnumerable<DbCommand> commands) {
      var result = 0;
      using (var conn = OpenConnection()) {
        using (var tx = conn.BeginTransaction()) {
          foreach (var cmd in commands) {
            cmd.Connection = conn;
            cmd.Transaction = tx;
            result += cmd.ExecuteNonQuery();
          }
          tx.Commit();
        }
      }
      return result;
    }

    /// <summary>
    /// Returns all records complying with the passed-in WHERE clause and arguments, 
    /// ordered as specified, limited (TOP) by limit.
    /// </summary>

    public IEnumerable<T> All<T>(string where = "", string orderBy = "", int limit = 0, string columns = "*", params object[] args) where T : new() {
      string sql = BuildSelect(where, orderBy, limit);
      var formatted = string.Format(sql, columns, TableName);
      return Query<T>(formatted, args);
    }

    protected abstract string BuildSelect(string where, string orderBy="", int limit=0);

    protected abstract string GetSingleSelect(string where);
    /// <summary>
    /// Returns a single row from the database
    /// </summary>
    public T FirstOrDefault<T>(string where, params object[] args) where T: new() {
      var result = new T();
      var sql = GetSingleSelect(where);
      return Query<T>(sql, args).FirstOrDefault();
    }

    /// <summary>
    /// Returns a single row from the database
    /// </summary>
    public T Find<T>(object key) where T : new() {
      var result = new T();
      var sql = GetSingleSelect(PrimaryKeyField + "=@0");
      return Query<T>(sql, key).FirstOrDefault();
    }


    /// <summary>
    /// This will return an Expando as a Dictionary
    /// </summary>
    IDictionary<string, object> ItemAsDictionary(ExpandoObject item){
      return (IDictionary<string, object>)item;
    }

    //Checks to see if a key is present based on the passed-in value
    bool ItemContainsKey(string key, ExpandoObject item) {
      var dc = ItemAsDictionary(item);
      return dc.ContainsKey(key);
    }

    /// <summary>
    /// Executes a set of objects as Insert or Update commands based on their property settings, within a transaction.
    /// These objects can be POCOs, Anonymous, NameValueCollections, or Expandos. Objects
    /// With a PK property (whatever PrimaryKeyField is set to) will be created at UPDATEs
    /// </summary>
    public virtual int Save(params T[] things) {
      var commands = BuildCommands(things);
      return Execute(commands);
    }

    DbCommand CreateInsertCommand(T insertItem) {
      DbCommand result = null;
      var expando = insertItem.ToExpando();
      var settings = (IDictionary<string, object>)expando;
      var sbKeys = new StringBuilder();
      var sbVals = new StringBuilder();
      var stub = "INSERT INTO {0} ({1}) \r\n VALUES ({2})";
      result = CreateCommand(stub, null);
      int counter = 0;
      if (PkIsIdentityColumn) {
        var col = settings.FirstOrDefault(x => x.Key.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
        settings.Remove(col);
      }
      foreach (var item in settings) {
        sbKeys.AppendFormat("{0},", item.Key);

        //this is a special case for a search directive
        if (item.Value.ToString().StartsWith("to_tsvector")) {
          sbVals.AppendFormat("{0},", item.Value);
        } else {
          sbVals.AppendFormat("@{0},", counter.ToString());
          result.AddParam(item.Value);
        }
        counter++;

      }
      if (counter > 0) {
        var keys = sbKeys.ToString().Substring(0, sbKeys.Length - 1);
        var vals = sbVals.ToString().Substring(0, sbVals.Length - 1);
        var sql = string.Format(stub, TableName, keys, vals);
        result.CommandText = sql;
      }
      else throw new InvalidOperationException("Can't parse this object to the database - there are no properties set");
      return result;
    }

    internal List<DbCommand> CreateInsertBatchCommands<T>(List<T> newRecords) {
      // The magic SQL Server Parameter Limit:
      var MAGIC_PARAMETER_LIMIT = 2100;
      var MAGIC_ROW_VALUE_LIMIT = 1000;
      int paramCounter = 0;
      int rowValueCounter = 0;
      var commands = new List<DbCommand>();

      // We need a sample to grab the object schema:
      var first = newRecords.First().ToExpando();
      var schema = (IDictionary<string, object>)first;

      // Remove identity column - "can't touch this..."
      if (PkIsIdentityColumn) {
        var col = schema.FirstOrDefault(x => x.Key.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
        schema.Remove(col);
      }

      var sbFieldNames = new StringBuilder();
      foreach (var field in schema) {
        sbFieldNames.AppendFormat("{0},", field.Key);
      }
      var keys = sbFieldNames.ToString().Substring(0, sbFieldNames.Length - 1);

      // Get the core of the INSERT statement, then append each set of field params per record:
      var sqlStub = string.Format("INSERT INTO {0} ({1}) VALUES ", TableName, keys);
      var sbSql = new StringBuilder(sqlStub);
      var dbCommand = CreateCommand("", null);

      foreach (var item in newRecords) {
        // Things explode if you exceed the param limit for SQL Server:
        if (paramCounter + schema.Count >= MAGIC_PARAMETER_LIMIT || rowValueCounter >= MAGIC_ROW_VALUE_LIMIT) {
          // Add the current command to the list, then start over with another:
          dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
          commands.Add(dbCommand);
          sbSql = new StringBuilder(sqlStub);
          paramCounter = 0;
          rowValueCounter = 0;
          dbCommand = CreateCommand("", null);
        }
        var ex = item.ToExpando();

        // Can't insert against an Identity field:
        var itemSchema = (IDictionary<string, object>)ex;
        if (PkIsIdentityColumn) {
          var col = itemSchema.FirstOrDefault(x => x.Key.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase));
          itemSchema.Remove(col);
        }
        var sbParamGroup = new StringBuilder();
        foreach (var fieldValue in itemSchema.Values) {
          sbParamGroup.AppendFormat("@{0},", paramCounter.ToString());
          dbCommand.AddParam(fieldValue);
          paramCounter++;
        }
        // Make a whole record to insert (we are inserting like this - (@0,@1,@2), (@3,@4,@5), (etc, etc, etc) . . .
        sbSql.AppendFormat("({0}),", sbParamGroup.ToString().Substring(0, sbParamGroup.Length - 1));
        rowValueCounter++;
      }
      dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
      commands.Add(dbCommand);
      return commands;
    }

    /// <summary>
    /// Creates a command for use with transactions - internal stuff mostly, but here for you to play with
    /// </summary>
    DbCommand CreateUpdateCommand(T updateItem) {
      var expando = updateItem.ToExpando();
      var key = GetPrimaryKey(updateItem);
      var settings = (IDictionary<string, object>)expando;
      var sbKeys = new StringBuilder();
      var stub = "UPDATE {0} SET {1} WHERE {2} = @{3}";
      var args = new List<object>();
      var result = CreateCommand(stub, null);
      int counter = 0;
      foreach (var item in settings) {
        var val = item.Value;
        if (!item.Key.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase) && item.Value != null) {
          result.AddParam(val);
          sbKeys.AppendFormat("{0} = @{1}, \r\n", item.Key, counter.ToString());
          counter++;
        }
      }
      if (counter > 0)
      {
        //add the key
        result.AddParam(key);
        //strip the last commas
        var keys = sbKeys.ToString().Substring(0, sbKeys.Length - 4);
        result.CommandText = string.Format(stub, TableName, keys, PrimaryKeyField, counter);
      } else {
        throw new InvalidOperationException("No parsable object was sent in - could not divine any name/value pairs");
      }
      return result;
    }

    /// <summary>
    /// Removes one or more records from the DB according to the passed-in WHERE
    /// </summary>
    DbCommand CreateDeleteCommand(string where = "", object key = null, params object[] args) {
      var sql = string.Format("DELETE FROM {0} ", TableName);
      if (key != null) {
        sql += string.Format("WHERE {0}=@0", PrimaryKeyField);
        args = new object[] { key };
      }
      else if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : "WHERE " + where;
      }
      return CreateCommand(sql, null, args);
    }

    //Temporary holder for error messages
    public IList<string> Errors = new List<string>();

    public abstract string GetInsertReturnValueSQL();

    public T Insert (T item) {
      //var ex = item.ToExpando();

      if (BeforeSave(item)) {
        using (var conn = OpenConnection()) {
          var cmd = (DbCommand)CreateInsertCommand(item);
          cmd.CommandText += this.GetInsertReturnValueSQL();
          var newId = cmd.ExecuteScalar();
          this.SetPrimaryKey(item, newId);
        }
      }
      return item;

    }

    /// <summary>
    /// Inserts a large range - does not check for existing entires, and assumes all 
    /// included records are new records. Order of magnitude more performant than standard
    /// Insert method for multiple sequential inserts. 
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public int BulkInsert(List<T> items) {
      var first = items.First();
      var ex = first.ToExpando();
      var itemSchema = (IDictionary<string, object>)ex;
      var itemParameterCount = itemSchema.Values.Count();
      var requiredParams = items.Count * itemParameterCount;
      var batchCounter = requiredParams / 2000;

      var rowsAffected = 0;
      if (items.Count() > 0) {
        using (var conn = OpenConnection()) {
          var commands = CreateInsertBatchCommands(items);
          foreach (var cmd in commands) {
            cmd.Connection = conn;
            rowsAffected += cmd.ExecuteNonQuery();
          }
        }
      }
      return rowsAffected;
    }


    public int Update(T item)
    {
      var result = 0;
      if (BeforeSave(item)) {
        using (var conn = OpenConnection()) {
          var cmd = (DbCommand)CreateUpdateCommand(item);
          result = cmd.ExecuteNonQuery();
        }
      }

      return result;
    }

    /// <summary>
    /// Removes one or more records from the DB according to the passed-in WHERE
    /// </summary>
    public int Delete(object key) {
      var deleted = this.Find<T>(key);
      var result = 0;
      if (BeforeDelete(deleted)) {
        result = Execute(CreateDeleteCommand(key: key));
        Deleted(deleted);
      }
      return result;
    }

    /// <summary>
    /// Removes one or more records from the DB according to the passed-in WHERE
    /// </summary>
    public int DeleteWhere(string where = "", params object[] args) {
      return Execute(CreateDeleteCommand(where: where, args: args));
    }

    //Hooks
    public virtual void Validate(T item) { }
    public virtual void Inserted(T item) { }
    public virtual void Updated(T item) { }
    public virtual void Deleted(T item) { }
    public virtual bool BeforeDelete(T item) { return true; }
    public virtual bool BeforeSave(T item) { return true; }


    public int Count() {
      return Count(TableName);
    }

    public int Count(string tableName, string where = "", params object[] args) {
      return (int)Scalar("SELECT COUNT(1) FROM " + tableName + " " + where, args);
    }

  }
}
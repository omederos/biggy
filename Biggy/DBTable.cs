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

    /// <summary>
    /// Returns an Open Connection
    /// </summary>
    internal abstract DbConnection OpenConnection();
    protected abstract string BuildSelect(string where, string orderBy = "", int limit = 0);
    protected abstract string GetSingleSelect(string where);
    public abstract string GetInsertReturnValueSQL();
    protected abstract string DbDelimiterFormatString { get; }
    protected abstract bool columnIsAutoIncrementing(string columnName);


    public virtual string DelimitedTableName {
      get {
        return string.Format(this.DbDelimiterFormatString, this.TableName);
      }
    }

    public virtual DbColumnMapping PrimaryKeyMapping { get; set; }
    public virtual string TableName { get; set; }
    public string DescriptorField { get; protected set; }
    public virtual DbColumnMappingLookup PropertyColumnMappings { get; private set; }

    protected void mapDbColumns() {
      this.PropertyColumnMappings = new DbColumnMappingLookup(this.DbDelimiterFormatString);
      var columnNames = this.getTableColumns();
      if(columnNames.Count > 0) {
        var item = new T();
        var itemType = item.GetType();
        var props = itemType.GetProperties();
        string replaceString = "[^a-zA-Z1-9]";
        var rgx = new Regex(replaceString);

        // First map all the columns. If an explicit attribute mapping exists, use it. Otherwise, math 'em up:
        foreach (var property in props) {
          string propertyName = rgx.Replace(property.Name.ToLower(), "");
          string columnName = columnNames.FirstOrDefault(c => rgx.Replace(c.ToLower(), "") == propertyName);

          if (columnName != null) {
            // Just map it:
            var newMapping = this.PropertyColumnMappings.Add(columnName, property.Name);
            newMapping.DataType = property.GetType();
          } else {
            // Look for a custom column name attribute:
            DbColumnAttribute mappedColumnAttribute = null;
            var attribute = property.GetCustomAttributes(false).FirstOrDefault(a => a.GetType() == typeof(DbColumnAttribute));
            if (attribute != null) {
              // Use the column name found in the attribute:
              mappedColumnAttribute = attribute as DbColumnAttribute;
              columnName = mappedColumnAttribute.Name;
            }
            var newMapping = this.PropertyColumnMappings.Add(columnName, property.Name);
            newMapping.DataType = property.GetType();
          }
        }

        // Now, find the PK  column. If one is explicitly set with an attribute, use that:
        var pkProperty = props.FirstOrDefault(p => p.GetCustomAttributes(false).Any(a => a.GetType() == typeof(PrimaryKeyAttribute)));
        PrimaryKeyAttribute pkAttribute = null;
        if (pkProperty != null) {
          pkAttribute = pkProperty.GetCustomAttributes(false).FirstOrDefault(a => a.GetType() == typeof(PrimaryKeyAttribute)) as PrimaryKeyAttribute;
          this.PrimaryKeyMapping = this.PropertyColumnMappings.FindByProperty(pkProperty.Name);
          this.PrimaryKeyMapping.IsPrimaryKey = true;
          this.PrimaryKeyMapping.IsAutoIncementing = pkAttribute.IsAutoIncrementing;
        } else {
          // Otherwise, try to find one:
          string pkCandidate = rgx.Replace(itemType.Name.ToLower(), "") + "id";
          string columnMatch = columnNames.FirstOrDefault(c => rgx.Replace(c.ToLower(), "") == pkCandidate);
          if (columnMatch != null) {
            this.PrimaryKeyMapping = this.PropertyColumnMappings.FindByColumn(columnMatch);
            bool isAuto = this.columnIsAutoIncrementing(this.PrimaryKeyMapping.ColumnName);
            this.PrimaryKeyMapping.IsAutoIncementing = isAuto;
          }
        }
      }
    }

    protected virtual List<string> getTableColumns() {
      var result = new List<string>();
      string sql = string.Format("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", this.TableName);
      using (var conn = OpenConnection()) {
        var rdr = CreateCommand(sql, conn, "").ExecuteReader();
        while (rdr.Read()) {
          result.Add((string)rdr["COLUMN_NAME"]);
        }
      }
      return result;
    }

    public DBTable(string connectionStringName) {
      var thingyType = this.GetType().GenericTypeArguments[0].Name;
      this.TableName = Inflector.Inflector.Pluralize(thingyType);
      ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
      this.mapDbColumns();
    }
    
    public DBTable(string connectionStringName, string tableName = "",
      string primaryKeyField = "", bool pkIsIdentityColumn = true)
    {
      ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
      TableName = tableName == "" ? this.GetType().Name : tableName;
      this.mapDbColumns();
    }

    /// <summary>
    /// Conventionally introspects the object passed in for a field that 
    /// looks like a PK. If you've named your PrimaryKeyField, this becomes easy
    /// </summary>
    public bool HasPrimaryKey(object o)
    {
      return o.ToDictionary().ContainsKey(this.PrimaryKeyMapping.PropertyName);
    }

    /// <summary>
    /// If the object passed in has a property with the same name as your PrimaryKeyField
    /// it is returned here.
    /// </summary>
    public object GetPrimaryKey(object o) {
      object result = null;
      var lookup = o.ToDictionary();
      string propName = this.PrimaryKeyMapping.PropertyName;
      var found = lookup.FirstOrDefault(x => x.Key.Equals(propName, StringComparison.OrdinalIgnoreCase));
      result = found.Value;
      return result;
    }

    public void SetPrimaryKey(T item, object value) {
      var props = item.GetType().GetProperties();
      if (item is ExpandoObject) {
        var d = item as IDictionary<string, object>;
        d[this.PrimaryKeyMapping.PropertyName] = value;
      } else {
        // Find the property the PK maps to:
        string mappedPropertyName = this.PrimaryKeyMapping.PropertyName;
        var pkProp = props.FirstOrDefault(x => x.Name.Equals(mappedPropertyName, StringComparison.OrdinalIgnoreCase));
        var converted = Convert.ChangeType(value, pkProp.PropertyType);
        pkProp.SetValue(item, converted);
      }
    }



    public IEnumerable<T> Where(string where, params object[] args) {
      var sql = BuildSelect(where, "", -1);
      var formatted = string.Format(sql, "*", this.DelimitedTableName);
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
          yield return this.MapReaderToObject<T>(rdr);
        }
      }
    }

    public IEnumerable<T> Query<T>(string sql, DbConnection connection, params object[] args) where T : new() {
      using (var rdr = CreateCommand(sql, connection, args).ExecuteReader()) {
        while (rdr.Read()) {
          yield return this.MapReaderToObject<T>(rdr);
        }
      }
    }

    internal T MapReaderToObject<T>(IDataReader reader) where T : new() {
      var item = new T();
      var props = item.GetType().GetProperties();
      foreach (var property in props) {
        if (this.PropertyColumnMappings.ContainsPropertyName(property.Name)) {
          string mappedColumn = this.PropertyColumnMappings.FindByProperty(property.Name).ColumnName;
          int ordinal = reader.GetOrdinal(mappedColumn);
          var val = reader.GetValue(ordinal);
          if (val.GetType() != typeof(DBNull)) {
            property.SetValue(item, reader.GetValue(ordinal));
          }
        }
      }
      return item;
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
      var formatted = string.Format(sql, columns, this.DelimitedTableName);
      return Query<T>(formatted, args);
    }

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
      var sql = GetSingleSelect(this.PrimaryKeyMapping.DelimitedColumnName + "=@0");
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
      if (this.PrimaryKeyMapping.IsAutoIncementing) {
        string mappedPropertyName = this.PrimaryKeyMapping.PropertyName;
        var col = settings.FirstOrDefault(x => x.Key.Equals(mappedPropertyName, StringComparison.OrdinalIgnoreCase));
        settings.Remove(col);
      }
      foreach (var item in settings) {
        sbKeys.AppendFormat("{0},", this.PropertyColumnMappings.FindByProperty(item.Key).DelimitedColumnName);

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
        var sql = string.Format(stub, this.DelimitedTableName, keys, vals);
        result.CommandText = sql;
      }
      else throw new InvalidOperationException("Can't parse this object to the database - there are no properties set");
      return result;
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
      string mappedPkPropertyName = this.PrimaryKeyMapping.PropertyName;
      foreach (var item in settings) {
        var val = item.Value;
        // Find the property name mapped to this column name:
        if (!item.Key.Equals(mappedPkPropertyName, StringComparison.OrdinalIgnoreCase) && item.Value != null) {
          result.AddParam(val);
          //// use the mapped, delimited database column name:
          string dbColumnName = this.PropertyColumnMappings.FindByProperty(item.Key).DelimitedColumnName;
          sbKeys.AppendFormat("{0} = @{1}, \r\n", dbColumnName, counter.ToString());
          counter++;
        }
      }
      if (counter > 0) {
        //add the key
        result.AddParam(key);
        //strip the last commas
        var keys = sbKeys.ToString().Substring(0, sbKeys.Length - 4);
        result.CommandText = string.Format(stub, this.DelimitedTableName, keys, this.PrimaryKeyMapping.DelimitedColumnName, counter);
      } else {
        throw new InvalidOperationException("No parsable object was sent in - could not divine any name/value pairs");
      }
      return result;
    }

    /// <summary>
    /// Removes one or more records from the DB according to the passed-in WHERE
    /// </summary>
    DbCommand CreateDeleteCommand(string where = "", object key = null, params object[] args) {
      var sql = string.Format("DELETE FROM {0} ", this.DelimitedTableName);
      if (key != null) {
        sql += string.Format("WHERE {0}=@0", this.PrimaryKeyMapping.DelimitedColumnName);
        args = new object[] { key };
      }
      else if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : "WHERE " + where;
      }
      return CreateCommand(sql, null, args);
    }

    //Temporary holder for error messages
    public IList<string> Errors = new List<string>();


    public T Insert (T item) {
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
      int rowsAffected = 0;
      const int MAGIC_PG_PARAMETER_LIMIT = 2100;
      const int MAGIC_PG_ROW_VALUE_LIMIT = 1000;
      var first = items.First();
      string insertClause = "";
      var sbSql = new StringBuilder("");

      using (var conn = OpenConnection()) {
        using (var transaction = conn.BeginTransaction()) {
          var commands = new List<DbCommand>();
          DbCommand dbCommand = conn.CreateCommand();
          dbCommand.Transaction = transaction;
          var paramCounter = 0;
          var rowValueCounter = 0;

          foreach (var item in items) {
            var itemEx = item.ToExpando();
            var itemSchema = itemEx as IDictionary<string, object>;
            var sbParamGroup = new StringBuilder();
            if (this.PrimaryKeyMapping.IsAutoIncementing) {
              // Don't insert against a serial id:
              string mappedPkPropertyName = this.PrimaryKeyMapping.PropertyName;
              string key = itemSchema.Keys.First(k => k.ToString().Equals(mappedPkPropertyName, StringComparison.OrdinalIgnoreCase));
              itemSchema.Remove(key);
            }
            // Build the first part of the INSERT, including delimited column names:
            if (ReferenceEquals(item, first)) {
              var sbFieldNames = new StringBuilder();
              foreach (var field in itemSchema) {
                string mappedColumnName = this.PropertyColumnMappings.FindByProperty(field.Key).DelimitedColumnName;
                sbFieldNames.AppendFormat("{0},", mappedColumnName);
              }
              var keys = sbFieldNames.ToString().Substring(0, sbFieldNames.Length - 1);
              string stub = "INSERT INTO {0} ({1}) VALUES ";
              insertClause = string.Format(stub, this.DelimitedTableName, string.Join(", ", keys));
              sbSql = new StringBuilder(insertClause);
            }
            foreach (var key in itemSchema.Keys) {
              if (paramCounter + itemSchema.Count >= MAGIC_PG_PARAMETER_LIMIT || rowValueCounter >= MAGIC_PG_ROW_VALUE_LIMIT) {
                dbCommand.CommandText = sbSql.ToString().Substring(0, sbSql.Length - 1);
                commands.Add(dbCommand);
                sbSql = new StringBuilder(insertClause);
                paramCounter = 0;
                rowValueCounter = 0;
                dbCommand = conn.CreateCommand();
                dbCommand.Transaction = transaction;
              }
              // Add the Param groups to the end:
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
            transaction.Commit();
          } catch (Exception) {
            transaction.Rollback();
          }
        }
      }
      return rowsAffected;
    }

    public int Update(T item)  {
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
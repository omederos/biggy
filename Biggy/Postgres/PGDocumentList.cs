using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Biggy.Extensions;

namespace Biggy.Postgres {

  public class PGDocumentList<T> : InMemoryList<T> where T : new(){

    public string TableName { get; set; }
    public PGTable<dynamic> Model { get; set; }
    public string PrimaryKeyField { get; set; }
    public Type PrimaryKeyType { get; set; }
    string BaseName {
      get {
        return typeof(T).Name.ToLower();
      }
    }

    public PGDocumentList(string connectionStringName) {
      DecideTableName();
      AssureKeyForT();
      this.Model = new PGTable<dynamic>(connectionStringName, this.TableName, this.PrimaryKeyField, this.PrimaryKeyType == typeof(int));
      TryLoadData();
    }

    void DecideTableName() {
      //use the type name
      this.TableName = Inflector.Inflector.Pluralize(BaseName).ToLower();
    }

    void AssureKeyForT() {
      var acceptableKeys = new string[]{"ID", this.BaseName + "ID"};
      var props = typeof(T).GetProperties();
      var conventionalKey = props.FirstOrDefault(x => x.Name.Equals("id",StringComparison.OrdinalIgnoreCase)) ??
         props.FirstOrDefault(x => x.Name.Equals(this.BaseName + "ID" ,StringComparison.OrdinalIgnoreCase));

      if (conventionalKey == null) {
        //HACK: This is horrible... but it works. Attribute.GetCustomAttribute doesn't work for some reason
        //I think it's because of assembly issues?
        foreach (var prop in props) {
          foreach (var att in prop.CustomAttributes) {
            if (att.AttributeType == typeof(PrimaryKeyAttribute)) {
              this.PrimaryKeyField = prop.Name;
              this.PrimaryKeyType = prop.PropertyType;
              break;
            }
          }
        }
      } else {
        this.PrimaryKeyType = typeof(int);
        
      }

      if (String.IsNullOrWhiteSpace(this.PrimaryKeyField)) {
        throw new InvalidOperationException("Can't tell what the primary key is. You can use ID, " + BaseName + "ID, or specify with the PrimaryKey attribute");
      }

    }


    public void Clear() {
      this.Model.Execute("DELETE FROM " + TableName);
      base.Clear();
    }

    void TryLoadData() {
      try {
        this.Reload();
      } catch (Npgsql.NpgsqlException x) {
        if (x.Message.Contains("does not exist")) {

          //create the table
          var idType = this.PrimaryKeyType == typeof(int) ? "int serial" : "varchar(255)";
          var sql = string.Format("CREATE TABLE {0} ({1} {2} primary key not null, body json not null);", this.TableName, this.PrimaryKeyField, idType);
          this.Model.Execute(sql);
          TryLoadData();
        } else {
          throw;
        }
      }
    }
    public void Reload() {
      _items = this.Model.All<T>().ToList();

    }
    public void Add(T item) {
      var expando = SetDataForDocument(item);
      this.Model.Insert(expando);
      base.Add(item);
    }
    public int AddRange(List<T> items) {
      var addList = new List<dynamic>();
      foreach (var item in items) {
        var ex = new ExpandoObject();
        var d = ex as IDictionary<string, object>;
        d[PrimaryKeyField] = this.Model.GetPrimaryKey(item);
        //TODO: this is SLOW over a large set 
        d["Body"] = JsonConvert.SerializeObject(item);
        addList.Add(ex);
      }

      var first = addList.First();
      var requiredParams = 2;
      var batchCounter = requiredParams / 2000;

      var rowsAffected = 0;
      if (addList.Count() > 0) {
        using (var conn = this.Model.OpenConnection()) {
          var commands = this.Model.CreateInsertBatchCommands(addList);
          foreach (var cmd in commands) {
            cmd.Connection = conn;
            rowsAffected += cmd.ExecuteNonQuery();
          }
        }
      }
      return rowsAffected;


      int affected = this.Model.BulkInsert(addList);
      this.Reload();
      return affected;
    }
    ExpandoObject SetDataForDocument(T item) {
      var json = JsonConvert.SerializeObject(item);
      var key = this.Model.GetPrimaryKey(item);
      var expando = new ExpandoObject();
      var dict = expando as IDictionary<string, object>;
      dict[PrimaryKeyField] = key;
      dict["body"] = json;
      return expando;
    }

    public int Update(T item) {
      var expando = SetDataForDocument(item);
      return this.Model.Update(expando);
    }
  }
}

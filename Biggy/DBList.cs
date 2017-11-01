using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  public abstract class DBList<T> : InMemoryList<T> where T : new(){

    public string TableName { get; set; }
    //public string PrimaryKeyField { get; set; }
    public string ConnectionStringName { get; set; }
    public DBTable<T> Model { get; set; }

    public abstract void SetModel();


    public DBList(string connectionStringName, string tableName = "guess") {
      //this.PrimaryKeyField = primaryKeyName;
      this.ConnectionStringName = connectionStringName;
      SetTableName(tableName);
      SetModel();
      this.Reload();
      this.FireLoadedEvents();
    }
    protected void SetTableName(string tableName) {
      if (tableName != "guess") {
        this.TableName = tableName;
      } else {
        var thingyType = this.GetType().GenericTypeArguments[0].Name;
        if (thingyType == "Object")
        {
            //this is DYNAMIC so set a DYNAMIC flag
            this.TableName = "DYNAMIC";
        }
        else
        {
            this.TableName = Inflector.Inflector.Pluralize(thingyType).ToLower();
        }
      }
    }
    public IEnumerable<T> Query(string sql, params object[] args) {
        var results = this.Model.Query<T>(sql, args);
        //HACK: formalize this 
        if (this.TableName == "DYNAMIC")
        {
            _items = results.ToList();
        }
        return results;
    }


    public void Reload() {
        if (this.TableName != "DYNAMIC")
        {
            _items = this.Model.All<T>().ToList();

        }
        else
        {
            //we'll load the items at first run
        }
    }

    public int Update(T item) {
      var updated = this.Model.Update(item);
      base.Update(item);
      
      return updated;
    }

    public void Add(T item) {
      this.Model.Insert(item);
      base.Add(item);
    }

    public int AddRange(List<T> items) {
        int affected = this.Model.BulkInsert(items);
        this.Reload();
        return affected;
    }


    public bool Remove(T item) {
      this.Model.Delete(this.Model.GetPrimaryKey(item));
      return base.Remove(item);
    }

    public int RemoveSet(IEnumerable<T> list) {
      var removed = 0;
      if (list.Count() > 0) {
        //remove from the DB
        var keyList = new List<string>();
        foreach (var item in list) {
          keyList.Add(this.Model.GetPrimaryKey(item).ToString());
        }
        var keySet = String.Join(",", keyList.ToArray());
        var inStatement = this.Model.PrimaryKeyMapping.DelimitedColumnName + " IN (" + keySet + ")";
        removed = this.Model.DeleteWhere(inStatement, "");

        this.Reload();
      }
      return removed;
    }

  }
}

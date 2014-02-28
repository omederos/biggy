using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Biggy.Extensions;
using System.Dynamic;
using Newtonsoft.Json;
namespace Biggy {
  public abstract class DBDocumentList<T> : InMemoryList<T> where T : new() {
    
    public string TableName { get; set; }
    public DBTable<dynamic> Model { get; set; }
    public string PrimaryKeyField { get; set; }
    public Type PrimaryKeyType { get; set; }
    public bool PKIsIdentity { get; set; }
    public string[] FullTextFields { get; set; }
    public string ConnectionStringName { get; set; }

    internal virtual string GetBaseName(){
      return typeof(T).Name;
    }
    public abstract void SetModel();

    public DBDocumentList(string connectionStringName) {
      this.ConnectionStringName = connectionStringName;
      DecideTableName();
      AssureKeyForT();
      SetFullTextColumns();
      var pkIsIdentity = this.PrimaryKeyType == typeof(int);
      SetModel();
      TryLoadData();
    }

    void DecideTableName() {
      //use the type name
      var baseName = this.GetBaseName();
      this.TableName = Inflector.Inflector.Pluralize(baseName);
    }

    void SetFullTextColumns() {
      var foundProps = new T().LookForCustomAttribute(typeof(FullTextAttribute));
      this.FullTextFields = foundProps.Select(x => x.Name).ToArray();
    }

    void AssureKeyForT() {
      var baseName = this.GetBaseName();
      var acceptableKeys = new string[]{"ID", baseName + "ID"};
      var props = typeof(T).GetProperties();
      var conventionalKey = props.FirstOrDefault(x => x.Name.Equals("id",StringComparison.OrdinalIgnoreCase)) ??
         props.FirstOrDefault(x => x.Name.Equals(baseName + "ID" ,StringComparison.OrdinalIgnoreCase));

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
        this.PrimaryKeyField = conventionalKey.Name;
      }
      this.PKIsIdentity = this.PrimaryKeyType == typeof(int);
      if (String.IsNullOrWhiteSpace(this.PrimaryKeyField)) {
        throw new InvalidOperationException("Can't tell what the primary key is. You can use ID, " + baseName + "ID, or specify with the PrimaryKey attribute");
      }

    }

    /// <summary>
    /// Drops all data from the table - BEWARE
    /// </summary>
    public override void Clear() {
      this.Model.Execute("DELETE FROM " + TableName);
      base.Clear();
    }

    internal abstract void TryLoadData();

    /// <summary>
    /// Reloads the internal memory list
    /// </summary>
    public void Reload() {
      var results = this.Model.Query("select body from " + this.TableName);//this.Model.All<T>().ToList();
      //our results are all dynamic - but all we care about is the body
      var sb = new StringBuilder();
      foreach (var item in results) {
        sb.AppendFormat("{0},",item.body);
      }
      // Can't take a substring of a zero-length string:
      if(sb.Length > 0)
      {
        var scrunched = sb.ToString();
        var stripped = scrunched.Substring(0, scrunched.Length - 1);
        var json = string.Format("[{0}]", stripped);
        _items = JsonConvert.DeserializeObject<List<T>>(json);
      }
    }

    /// <summary>
    /// Adds a single T item to the database
    /// </summary>
    /// <param name="item"></param>
    public override void Add(T item){
     base.Add(item); 
    }

    /// <summary>
    /// A high-performance bulk-insert that can drop 10,000 documents in about 500ms
    /// </summary>
    public abstract int AddRange(List<T> items);

    protected ExpandoObject SetDataForDocument(T item) {
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
      if(this.PKIsIdentity)
      {
        dict.Remove(this.PrimaryKeyField);
      }
      return expando;
    }

    /// <summary>
    /// Updates a single T item
    /// </summary>
    public override int Update(T item) {
      return base.Update(item);
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

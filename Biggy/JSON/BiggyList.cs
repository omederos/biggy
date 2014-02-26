using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Biggy.JSON
{

    public class BiggyList<T> :InMemoryList<T> where T: new() {

      List<T> _items = null;
      public string DbDirectory { get; set; }
      public bool InMemory { get; set; }
      public string DbFileName { get; set; }
      public string DbName { get; set; }

      public string DbPath {
        get {
          return Path.Combine(DbDirectory, DbFileName);
        }
      }

      public bool HasDbFile {
        get {
          return File.Exists(DbPath);
        }
      }

      public BiggyList(string dbPath = "current", bool inMemory = false, string dbName = "") {

        this.InMemory = inMemory;

        if (String.IsNullOrWhiteSpace(dbName)) {
          var thingyType = this.GetType().GenericTypeArguments[0].Name;
          this.DbName = Inflector.Inflector.Pluralize(thingyType).ToLower();
        } else {
          this.DbName = dbName.ToLower();
        }
        this.DbFileName = this.DbName + ".json";
        this.SetDataDirectory(dbPath);
        _items = TryLoadFileData(this.DbPath);
      }



      public void SetDataDirectory(string dbPath) {
        var dataDir = dbPath;
        if (dbPath == "current") {
          var currentDir = Directory.GetCurrentDirectory();
          if (currentDir.EndsWith("Debug") || currentDir.EndsWith("Release")) {
            var projectRoot = Directory.GetParent(@"..\..\").FullName;
            dataDir = Path.Combine(projectRoot, "Data");
          }
        } else {
          dataDir = Path.Combine(dbPath, "Data");
        }
        Directory.CreateDirectory(dataDir);
        this.DbDirectory = dataDir;

      }


      public List<T> TryLoadFileData(string path) {

        List<T> result = new List<T>();
        if (File.Exists(path)) {
          //format for the deserializer...
          var json = "[" + File.ReadAllText(path).Replace(Environment.NewLine,",")+ "]";
          result = JsonConvert.DeserializeObject<List<T>>(json);
        }

        FireLoadedEvents();

        return result;
      }

      public void Reload() {
        _items = TryLoadFileData(this.DbPath);
      }

      public void Update(T item) {
        base.Update(item);
        this.FlushToDisk();
      }

      public void Add(T item) {
        var json = JsonConvert.SerializeObject(item);
        //append the to the file
        using (var writer = File.AppendText(this.DbPath)) {
          writer.WriteLine(json);
        }
        base.Add(item);
      }

      public void Clear() {
        base.Clear();
        this.FlushToDisk();
      }


      public bool Remove(T item) {
        var removed = base.Remove(item);
        this.FlushToDisk();
        return removed;
      }


      public bool FlushToDisk() {
        var json = JsonConvert.SerializeObject(this);
        var cleaned = json.Replace("[", "").Replace("]", "").Replace(",", Environment.NewLine);
        var buff = Encoding.Default.GetBytes(json);
        using (var fs = File.OpenWrite(this.DbPath)) {
          fs.WriteAsync(buff, 0, buff.Length);
        }
        return true;
      }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Biggy.JSON
{

    public class BiggyList<T> : FastList<T> {

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
        _items = TryLoadList();

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

      public override void Purge() {
        this.Clear();
        this.Save();
      }

      public override List<T> TryLoadList() {
        List<T> result = new List<T>();
        if (File.Exists(this.DbPath)) {
          var json = File.ReadAllText(this.DbPath);
          result = JsonConvert.DeserializeObject<List<T>>(json);
        }

        //call the base for eventing
        base.TryLoadList();

        return result;      
      }

      public override async Task SaveAsync() {
        if (!this.InMemory) {
          var json = JsonConvert.SerializeObject(this);
          var buff = Encoding.Default.GetBytes(json);
          using (var fs = File.OpenWrite(this.DbPath)) {
            await fs.WriteAsync(buff, 0, buff.Length);
          }
        }
      }

      public override bool Save() {
        try {
          if (!String.IsNullOrWhiteSpace(this.DbDirectory) && !this.InMemory) {
            //write it to disk
            var serializer = new JsonSerializer();
            using (var fs = File.CreateText(this.DbPath)) {
              serializer.Serialize(fs, this);
            }
          }
          this.FireSavedEvents();
          return true;

        } catch (Exception x) {
          //log it?
          throw;
        }
      }

      public override bool SaveBulk(params T[] items) {
        throw new NotImplementedException();
      }

      public override Task SaveBulkAsync(params T[] items) {
        _items.AddRange(items);
        return this.SaveAsync();
      }


    }
}

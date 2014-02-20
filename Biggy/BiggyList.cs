using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Biggy
{

    public class BiggyEventArgs<T> : EventArgs{
      public List<T> Items { get; set; }
      public dynamic Item { get; set; }

      public BiggyEventArgs() {
        Items = new List<T>();
        this.Item = default(T);
      }
    }

    public class BiggyList<T> : ICollection<T> {

      List<T> _items = null;
      public string DbDirectory { get; set; }
      public bool InMemory { get; set; }
      public string DbFileName { get; set; }
      public string DbName { get; set; }
      FileStream fs;


      public event EventHandler ItemRemoved;
      public event EventHandler ItemAdded;
      public event EventHandler Saved;
      public event EventHandler Changed;
      public event EventHandler Loaded;

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

      public void ClearAndSave() {
        this.Clear();
        this.Save();
      }

      public List<T> TryLoadFileData(string path) {

        List<T> result = new List<T>();
        if (File.Exists(path)) {
          var json = File.ReadAllText(path);
          result = JsonConvert.DeserializeObject<List<T>>(json);
        }

        if (this.Loaded != null) {
          var args = new BiggyEventArgs<T>();
          args.Items = result;
          this.Loaded.Invoke(this, args);
        }

        return result;
      }

      public void Reload() {
        _items = TryLoadFileData(this.DbPath);
      }

      public void Update(T item) {
        var index = _items.IndexOf(item);
        if (index > -1) {
          _items.RemoveAt(index);
          _items.Insert(index, item);
        } else {
          Add(item);
        }

      }

      public void Add(T item) {

        if (_items.Contains(item)) {
          //let's not overwrite -- this will be determined by
          //item.Equals()
          Update(item);
        } else {
          _items.Add(item);
        }

        //_items.Add(item);

        if (this.ItemAdded != null) {
          var args = new BiggyEventArgs<T>();
          args.Item = item;
          this.ItemAdded.Invoke(this, args);
        }
        if (this.Changed != null) {
          var args = new BiggyEventArgs<T>();
          args.Item = item;
          this.Changed.Invoke(this, args);
        }
      }

      public void Clear() {
        _items.Clear();
        if (this.Changed != null) {
          var args = new BiggyEventArgs<T>();
          this.Changed.Invoke(this, args);
        }
      }

      public bool Contains(T item) {
        return _items.Contains(item);
      }

      public void CopyTo(T[] array, int arrayIndex) {
        _items.CopyTo(array, arrayIndex);
      }

      public int Count {
        get { return _items.Count; }
      }

      public bool IsReadOnly {
        get { return false; }
      }

      public bool Remove(T item) {
        if (this.ItemRemoved != null) {
          var args = new BiggyEventArgs<T>();
          args.Item = item;
          this.ItemRemoved.Invoke(this, args);
        }
        if (this.Changed != null) {
          var args = new BiggyEventArgs<T>();
          args.Item = item;
          this.Changed.Invoke(this, args);
        }
        return _items.Remove(item);
      }

      public IEnumerator<T> GetEnumerator() {
        return _items.GetEnumerator();
      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return _items.GetEnumerator();
      }


      public async Task SaveAsync() {
        var json = JsonConvert.SerializeObject(this);
        var buff = Encoding.Default.GetBytes(json);
        using (var fs = File.OpenWrite(this.DbPath)) {
          await fs.WriteAsync(buff, 0, buff.Length);
        }
      }

      public bool Save() {

        try {
          if (!String.IsNullOrWhiteSpace(this.DbDirectory)) {
            //write it to disk
            var serializer = new JsonSerializer();
            using (var fs = File.CreateText(this.DbPath)) {
              serializer.Serialize(fs, this);
            }
          }
          if (this.Saved != null) {
            var args = new BiggyEventArgs<T>();
            args.Items = _items;
            this.Saved.Invoke(this, args);
          }
          return true;

        } catch (Exception x) {
          //log it?
          throw;
        }

      }

    }
}

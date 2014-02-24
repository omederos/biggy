using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  public abstract class FastList<T> : ICollection<T> {

    protected List<T> _items = null;

    public event EventHandler ItemRemoved;
    public event EventHandler ItemAdded;
    public event EventHandler Saved;
    public event EventHandler Changed;
    public event EventHandler Loaded;


    public abstract void Purge();

    public virtual List<T> TryLoadList(){
      this.FireLoadedEvents();
      return _items;
    }

    public void Reload() {
      _items = TryLoadList();
    }

    public void Update(T item) {
      var index = _items.IndexOf(item);
      if (index > -1) {
        _items.RemoveAt(index);
        _items.Insert(index, item);
        this.FireChangedEvents();
      } else {
        Add(item);
      }

    }

    public virtual void Add(T item) {

      if (_items.Contains(item)) {
        //let's not overwrite -- this will be determined by
        //item.Equals()
        Update(item);
      } else {
        _items.Add(item);
      }

      this.FireInsertedEvents(item);
      this.FireChangedEvents();
    }

    public virtual void Clear() {
      _items.Clear();
      this.FireChangedEvents();
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

    public virtual bool Remove(T item) {
      return _items.Remove(item);
      this.FireRemovedEvents(item);
      this.FireChangedEvents();
    }

    public IEnumerator<T> GetEnumerator() {
      return _items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return _items.GetEnumerator();
    }

    public abstract Task SaveAsync();
    public abstract bool Save();
    public abstract bool SaveBulk(params T[] items);
    public abstract Task SaveBulkAsync(params T[] items);


    protected void FireChangedEvents() {
      if (this.Changed != null) {
        var args = new BiggyEventArgs<T>();
        args.Items = _items;
        this.ItemRemoved.Invoke(this, args);
      }
    }

    protected void FireRemovedEvents(T item) {
      if (this.ItemRemoved != null) {
        var args = new BiggyEventArgs<T>();
        args.Item = item;
        this.ItemRemoved.Invoke(this, args);
      }
    }

    protected void FireInsertedEvents(T item) {
      if (this.ItemAdded != null) {
        var args = new BiggyEventArgs<T>();
        args.Item = item;
        this.ItemAdded.Invoke(this, args);
      }
    }

    protected void FireLoadedEvents() {
      if (this.Loaded != null) {
        var args = new BiggyEventArgs<T>();
        args.Items = _items;
        this.Loaded.Invoke(this, args);
      }
    }

    protected void FireSavedEvents() {
      if (this.Saved != null) {
        var args = new BiggyEventArgs<T>();
        args.Items = _items;
        this.Saved.Invoke(this, args);
      }
    }

  }
}

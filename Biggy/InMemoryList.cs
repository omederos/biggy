using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biggy {
  public class InMemoryList<T> : ICollection<T> where T : new() {

    protected List<T> _items = null;
    public InMemoryList() {
      _items = new List<T>();
    }
    public event EventHandler ItemRemoved;
    public event EventHandler ItemAdded;
    public event EventHandler Changed;
    public event EventHandler Loaded;
    public event EventHandler Saved;

    public void Purge() {
      this.Clear();
    }

    public virtual int Update(T item) {
      var index = _items.IndexOf(item);
      if (index > -1) {
        _items.RemoveAt(index);
        _items.Insert(index, item);
      }
      FireChangedEvents();
      return 1;
    }

    public virtual void Add(T item) {
      _items.Add(item);

      FireInsertedEvents(item);
      FireChangedEvents();
    }

    public virtual void Clear() {
      _items.Clear();
      FireChangedEvents();
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
      var removed =  _items.Remove(item);
      FireRemovedEvents(item);
      FireChangedEvents();
      return removed;
    }

    public IEnumerator<T> GetEnumerator() {
      return _items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return _items.GetEnumerator();
    }

    protected void FireChangedEvents() {
      if (this.Changed != null) {
        var args = new BiggyEventArgs<T>();
        args.Items = _items;
        this.Changed.Invoke(this, args);
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

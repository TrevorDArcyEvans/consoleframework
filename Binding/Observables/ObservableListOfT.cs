using System;
using System.Collections;
using System.Collections.Generic;

namespace Binding.Observables
{
  /// <summary>
  /// Generic implementation of <see cref="IObservableList"/>.
  /// Non-generic IList is implemented to enforce compatibility with
  /// Collection&lt;T&gt; and List&lt;T&gt;.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ObservableList<T> : IObservableList, IList<T>, IList
  {
    private readonly IList<T> _list;

    public ObservableList(IList<T> list)
    {
      this._list = list;
    }

    public IEnumerator<T> GetEnumerator()
    {
      return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public void Add(T item)
    {
      var index = _list.Count;
      _list.Add(item);
      RaiseListElementsAdded(index, 1);
    }

    int IList.Add(object value)
    {
      var count = this.Count;
      Add((T) value);
      return count;
    }

    bool IList.Contains(object value)
    {
      return Contains((T) value);
    }

    public void Clear()
    {
      var count = _list.Count;
      var removedItems = new List<object>();
      foreach (var item in _list)
      {
        removedItems.Add(item);
      }

      _list.Clear();

      RaiseListElementsRemoved(0, count, removedItems);
    }

    int IList.IndexOf(object value)
    {
      return IndexOf((T) value);
    }

    void IList.Insert(int index, object value)
    {
      Insert(index, (T) value);
    }

    void IList.Remove(object value)
    {
      Remove((T) value);
    }

    public bool Contains(T item)
    {
      return _list.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      _list.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
      var index = _list.IndexOf(item);
      _list.Remove(item);
      if (-1 != index)
      {
        RaiseListElementsRemoved(index, 1, new List<object>() { item });
        return true;
      }

      return false;
    }

    void ICollection.CopyTo(Array array, int index)
    {
      ((ICollection) _list).CopyTo(array, index);
    }

    public int Count
    {
      get { return _list.Count; }
    }

    object ICollection.SyncRoot
    {
      get { return ((ICollection) _list).SyncRoot; }
    }

    bool ICollection.IsSynchronized
    {
      get { return ((ICollection) _list).IsSynchronized; }
    }

    public bool IsReadOnly
    {
      get { return _list.IsReadOnly; }
    }

    bool IList.IsFixedSize
    {
      get { return ((IList) _list).IsFixedSize; }
    }

    public int IndexOf(T item)
    {
      return _list.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
      _list.Insert(index, item);
      RaiseListElementsAdded(index, 1);
    }

    public void RemoveAt(int index)
    {
      var removedItem = _list[index];
      _list.RemoveAt(index);
      RaiseListElementsRemoved(index, 1, new List<object>() { removedItem });
    }

    object IList.this[int index]
    {
      get { return this[index]; }
      set { this[index] = (T) value; }
    }

    public T this[int index]
    {
      get { return _list[index]; }
      set
      {
        T removedItem = _list[index];
        _list[index] = value;
        RaiseListElementReplaced(index, new List<object>() { removedItem });
      }
    }

    private void RaiseListElementsAdded(int index, int length)
    {
      if (null != ListChanged)
      {
        ListChanged.Invoke(this, new ListChangedEventArgs(ListChangedEventType.ItemsInserted, index, length, null));
      }
    }

    private void RaiseListElementsRemoved(int index, int length, List<object> removedItems)
    {
      if (null != ListChanged)
      {
        ListChanged.Invoke(this, new ListChangedEventArgs(ListChangedEventType.ItemsRemoved, index, length, removedItems));
      }
    }

    private void RaiseListElementReplaced(int index, List<object> removedItems)
    {
      if (null != ListChanged)
      {
        ListChanged.Invoke(this, new ListChangedEventArgs(ListChangedEventType.ItemReplaced, index, 1, removedItems));
      }
    }

    public event ListChangedHandler ListChanged;
  }
}

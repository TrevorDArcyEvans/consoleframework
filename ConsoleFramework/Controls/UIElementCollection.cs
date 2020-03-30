using System;
using System.Collections;
using System.Collections.Generic;
using Binding.Observables;

namespace ConsoleFramework.Controls
{
  public class UIElementCollection : IList
  {
    private readonly IList _list = new ObservableList<Control>(new List<Control>());
    private readonly Control _parent;

    public event Control.ControlAddedEventHandler ControlAdded;
    public event Control.ControlAddedEventHandler ControlRemoved;

    public UIElementCollection(Control parent)
    {
      this._parent = parent;
      ObservableList<Control> observableList = new ObservableList<Control>(new List<Control>());
      this._list = observableList;
      observableList.ListChanged += OnListChanged;
    }

    private void OnListChanged(object sender, ListChangedEventArgs args)
    {
      switch (args.Type)
      {
        case ListChangedEventType.ItemsInserted:
        {
          for (var i = 0; i < args.Count; i++)
          {
            var control = (Control) _list[args.Index + i];
            _parent.InsertChildAt(args.Index + i, control);
            if (ControlAdded != null)
            {
              ControlAdded.Invoke(control);
            }
          }

          break;
        }

        case ListChangedEventType.ItemsRemoved:
          for (var i = 0; i < args.Count; i++)
          {
            var control = _parent.Children[args.Index];
            _parent.RemoveChild(control);
            if (ControlRemoved != null)
            {
              ControlRemoved.Invoke(control);
            }
          }

          break;
        case ListChangedEventType.ItemReplaced:
        {
          var removedControl = _parent.Children[args.Index];
          _parent.RemoveChild(removedControl);
          if (ControlRemoved != null)
          {
            ControlRemoved.Invoke(removedControl);
          }

          var addedControl = (Control) _list[args.Index];
          _parent.InsertChildAt(args.Index, addedControl);
          if (ControlAdded != null)
          {
            ControlAdded.Invoke(addedControl);
          }

          break;
        }
      }
    }

    public IEnumerator GetEnumerator()
    {
      return _list.GetEnumerator();
    }

    public void CopyTo(Array array, int index)
    {
      _list.CopyTo(array, index);
    }

    public int Count
    {
      get { return _list.Count; }
    }

    public object SyncRoot
    {
      get { return _list.SyncRoot; }
    }

    public bool IsSynchronized
    {
      get { return _list.IsSynchronized; }
    }

    public int Add(object value)
    {
      return _list.Add(value);
    }

    public bool Contains(object value)
    {
      return _list.Contains(value);
    }

    public void Clear()
    {
      _list.Clear();
    }

    public int IndexOf(object value)
    {
      return _list.IndexOf(value);
    }

    public void Insert(int index, object value)
    {
      _list.Insert(index, value);
    }

    public void Remove(object value)
    {
      _list.Remove(value);
    }

    public void RemoveAt(int index)
    {
      _list.RemoveAt(index);
    }

    public object this[int index]
    {
      get { return _list[index]; }
      set { _list[index] = value; }
    }

    public bool IsReadOnly
    {
      get { return _list.IsReadOnly; }
    }

    public bool IsFixedSize
    {
      get { return _list.IsFixedSize; }
    }
  }
}

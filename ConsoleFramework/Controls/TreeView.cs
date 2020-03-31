using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using Xaml;
using ListChangedEventArgs = Binding.Observables.ListChangedEventArgs;

namespace ConsoleFramework.Controls
{
  [ContentProperty("Items")]
  public class TreeView : Control
  {
    private readonly ObservableList<TreeItem> _items = new ObservableList<TreeItem>(new List<TreeItem>());

    public IList<TreeItem> Items
    {
      get { return _items; }
    }

    public IItemsSource ItemsSource { get; set; }

    private readonly ListBox _listBox;

    public TreeItem SelectedItem
    {
      get
      {
        if (_treeItemsFlat.Count == 0)
        {
          return null;
        }

        if (_listBox.SelectedItemIndex == null)
        {
          return null;
        }

        return _treeItemsFlat[_listBox.SelectedItemIndex.Value];
      }
    }

    public TreeView()
    {
      _listBox = new ListBox();
      _listBox.HorizontalAlignment = HorizontalAlignment.Stretch;
      _listBox.VerticalAlignment = VerticalAlignment.Stretch;

      // Stretch by default too
      this.HorizontalAlignment = HorizontalAlignment.Stretch;
      this.VerticalAlignment = VerticalAlignment.Stretch;

      this.AddChild(_listBox);
      this._items.ListChanged += ItemsOnListChanged;

      _listBox.AddHandler(MouseDownEvent, new MouseEventHandler((sender, args) =>
      {
        if (!args.Handled)
        {
          if (_listBox.SelectedItemIndex.HasValue)
            ExpandCollapse(_treeItemsFlat[_listBox.SelectedItemIndex.Value]);
        }
      }), true);

      _listBox.SelectedItemIndexChanged += (sender, args) => { this.RaisePropertyChanged("SelectedItem"); };
    }

    private void SubscribeToItem(TreeItem item, ListChangedHandler handler)
    {
      item._items.ListChanged += handler;
      item.PropertyChanged += ItemOnPropertyChanged;
      foreach (var child in item._items)
      {
        SubscribeToItem(child, handler);
      }
    }

    private void UnsubscribeFromItem(TreeItem item, ListChangedHandler handler)
    {
      item._items.ListChanged -= handler;
      item.PropertyChanged -= ItemOnPropertyChanged;
      foreach (var child in item._items)
      {
        UnsubscribeFromItem(child, handler);
      }
    }

    private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      var senderItem = (TreeItem) sender;
      if (args.PropertyName == "DisplayTitle")
      {
        if (senderItem.Position >= 0)
        {
          _listBox.Items[senderItem.Position] = senderItem.DisplayTitle;
        }
      }

      if (args.PropertyName == "Disabled")
      {
        if (senderItem.Position >= 0)
        {
          if (senderItem.Disabled)
          {
            _listBox.DisabledItemsIndexes.Add(senderItem.Position);
          }
          else
          {
            _listBox.DisabledItemsIndexes.Remove(senderItem.Position);
          }
        }
      }

      if (args.PropertyName == "Expanded")
      {
        if (senderItem.Position >= 0)
        {
          if (senderItem.Expanded)
          {
            Expand(senderItem);
          }
          else
          {
            Collapse(senderItem);
          }
        }
      }
    }

    private void EnsureFlatListIsCorrect()
    {
      for (var i = 0; i < _treeItemsFlat.Count; i++)
      {
        assert(_treeItemsFlat[i].Position == i);
      }
    }

    /// <summary>
    /// Maintains the correct order of _items in flat list.
    /// </summary>
    private void OnItemInserted(int pos)
    {
      var treeItem = _items[pos];
      TreeItem prevItem = null;
      if (pos > 0)
      {
        prevItem = this._items[pos];
      }

      treeItem.Position = prevItem != null ? prevItem.Position + 1 : _items.Count - 1;
      for (var j = treeItem.Position; j < _treeItemsFlat.Count; j++)
      {
        _treeItemsFlat[j].Position++;
      }

      _treeItemsFlat.Insert(treeItem.Position, treeItem);
      _listBox.Items.Insert(treeItem.Position, treeItem.DisplayTitle);
      if (treeItem.Disabled)
      {
        _listBox.DisabledItemsIndexes.Add(treeItem.Position);
      }

      // Handle modification of inner list recursively
      SubscribeToItem(treeItem, ItemsOnListChanged);
      if (treeItem.Position <= _listBox.SelectedItemIndex)
      {
        RaisePropertyChanged("SelectedItem");
      }

      EnsureFlatListIsCorrect();
    }

    private void OnItemRemoved(TreeItem treeItem)
    {
      if (treeItem.Expanded)
      {
        Collapse(treeItem);
      }

      _treeItemsFlat.RemoveAt(treeItem.Position);
      _listBox.Items.RemoveAt(treeItem.Position);
      for (var j = treeItem.Position; j < _treeItemsFlat.Count; j++)
      {
        _treeItemsFlat[j].Position--;
      }

      // Cleanup event handler recursively
      UnsubscribeFromItem(treeItem, ItemsOnListChanged);

      if (_listBox.SelectedItemIndex >= treeItem.Position)
      {
        RaisePropertyChanged("SelectedItem");
      }

      EnsureFlatListIsCorrect();
    }

    private void ItemsOnListChanged(object sender, ListChangedEventArgs args)
    {
      switch (args.Type)
      {
        case ListChangedEventType.ItemsInserted:
        {
          for (var i = 0; i < args.Count; i++)
          {
            OnItemInserted(i + args.Index);
          }

          break;
        }

        case ListChangedEventType.ItemsRemoved:
        {
          foreach (var treeItem in args.RemovedItems.Cast<TreeItem>())
          {
            OnItemRemoved(treeItem);
          }

          break;
        }

        case ListChangedEventType.ItemReplaced:
        {
          OnItemRemoved((TreeItem) args.RemovedItems[0]);
          OnItemInserted(args.Index);
          break;
        }
      }
    }

    /// <summary>
    /// Flat list of tree _items in order corresponding to actual listbox content.
    /// </summary>
    private readonly List<TreeItem> _treeItemsFlat = new List<TreeItem>();

    private void Expand(TreeItem item)
    {
      var index = _treeItemsFlat.IndexOf(item);
      for (var i = 0; i < item.Items.Count; i++)
      {
        var child = item.Items[i];
        _treeItemsFlat.Insert(i + index + 1, child);
        child.Position = i + index + 1;
        child.Level = item.Level + 1;

        // Учесть уровень вложенности в title
        _listBox.Items.Insert(i + index + 1, child.DisplayTitle);
        if (child.Disabled)
        {
          _listBox.DisabledItemsIndexes.Add(i + index + 1);
        }
      }

      for (var k = index + 1 + item.Items.Count; k < _treeItemsFlat.Count; k++)
      {
        _treeItemsFlat[k].Position += item.Items.Count;
      }

      // Children are _expanded too according to their Expanded stored state
      foreach (var child in item.Items.Where(child => child.Expanded))
      {
        Expand(child);
      }

      EnsureFlatListIsCorrect();
    }

    private void Collapse(TreeItem item)
    {
      // Children are collapsed but with Expanded state saved
      foreach (var child in item.Items.Where(child => child.Expanded))
      {
        Collapse(child);
      }

      var index = _treeItemsFlat.IndexOf(item);
      foreach (var child in item.Items)
      {
        _treeItemsFlat.RemoveAt(index + 1);
        if (child.Disabled)
        {
          _listBox.DisabledItemsIndexes.Remove(index + 1);
        }

        _listBox.Items.RemoveAt(index + 1);
        child.Position = -1;
      }

      for (var k = index + 1; k < _treeItemsFlat.Count; k++)
      {
        _treeItemsFlat[k].Position -= item.Items.Count;
      }

      EnsureFlatListIsCorrect();
    }

    private void ExpandCollapse(TreeItem item)
    {
      var index = _treeItemsFlat.IndexOf(item);
      if (item.Expanded)
      {
        Collapse(item);
        item._expanded = false;
        // Need to update item string (because Expanded status has been changed)
        _listBox.Items[index] = item.DisplayTitle;
      }
      else
      {
        Expand(item);
        item._expanded = true;
        // Need to update item string (because Expanded status has been changed)
        _listBox.Items[index] = item.DisplayTitle;
      }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      _listBox.Measure(availableSize);
      return _listBox.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      _listBox.Arrange(new Rect(finalSize));
      return finalSize;
    }
  }
}

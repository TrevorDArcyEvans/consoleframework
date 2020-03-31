using System.Collections.Generic;
using System.ComponentModel;
using Binding.Observables;
using ConsoleFramework.Core;
using Xaml;

namespace ConsoleFramework.Controls
{
  [ContentProperty("Items")]
  public class TreeItem : INotifyPropertyChanged
  {
    /// <summary>
    /// Pos in TreeView listbox.
    /// </summary>
    internal int Position;

    internal int Level;

    internal string DisplayTitle
    {
      get
      {
        if (Items.Count != 0)
        {
          return string.Format("{0}{1} {2}", new string(' ', Level * 2),
            (Expanded ? UnicodeTable.ArrowDown : UnicodeTable.ArrowRight), Title);
        }

        return string.Format("{0}{1}", new string(' ', (Level + 1) * 2), Title);
      }
    }

    // todo : call listBox.Invalidate() if item is visible now
    private string _title;

    public string Title
    {
      get { return _title; }
      set
      {
        if (_title != value)
        {
          _title = value;
          RaisePropertyChanged("Title");
          RaisePropertyChanged("DisplayTitle");
        }
      }
    }

    private bool _disabled;

    public bool Disabled
    {
      get { return _disabled; }
      set
      {
        if (_disabled != value)
        {
          _disabled = value;
          RaisePropertyChanged("Disabled");
        }
      }
    }

    internal readonly ObservableList<TreeItem> _items = new ObservableList<TreeItem>(new List<TreeItem>());

    public IList<TreeItem> Items
    {
      get { return _items; }
    }

    public bool HasChildren
    {
      get { return _items.Count != 0; }
    }

    public IItemsSource ItemsSource { get; set; }

    internal bool _expanded;

    public bool Expanded
    {
      get { return _expanded; }
      set
      {
        if (_expanded != value)
        {
          _expanded = value;
          RaisePropertyChanged("Expanded");
          RaisePropertyChanged("DisplayTitle");
        }
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void RaisePropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(propertyName));
      }
    }
  }
}

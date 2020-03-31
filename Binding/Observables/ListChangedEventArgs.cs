using System;
using System.Collections.Generic;

namespace Binding.Observables
{
  public class ListChangedEventArgs : EventArgs
  {
    public ListChangedEventArgs(ListChangedEventType type, int index, int count, List<Object> removedItems)
    {
      this.Type = type;
      this.Index = index;
      this.Count = count;
      this.RemovedItems = removedItems;
    }

    public readonly ListChangedEventType Type;
    public readonly int Index;
    public readonly int Count;
    public readonly List<object> RemovedItems;
  }
}

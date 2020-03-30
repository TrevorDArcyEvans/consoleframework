using System.Collections.Generic;

namespace ConsoleFramework.Controls
{
  public interface IItemsSource
  {
    IList<TreeItem> GetItems();
  }
}

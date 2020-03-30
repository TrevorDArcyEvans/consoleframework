using ConsoleFramework.Events;

namespace ConsoleFramework.Controls
{
  public class ScrollBarValueChanged : RoutedEventArgs
  {
    public ScrollBarValueChanged(object source, RoutedEvent routedEvent) : base(source, routedEvent)
    {
    }
  }
}

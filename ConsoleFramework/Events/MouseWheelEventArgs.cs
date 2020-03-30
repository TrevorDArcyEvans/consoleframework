using ConsoleFramework.Core;

namespace ConsoleFramework.Events
{
  public class MouseWheelEventArgs : MouseEventArgs
  {
    public MouseWheelEventArgs(
      object source,
      RoutedEvent routedEvent,
      Point rawPosition,
      MouseButtonState leftButton,
      MouseButtonState middleButton,
      MouseButtonState rightButton,
      int delta)
      : base(source, routedEvent, rawPosition, leftButton, middleButton, rightButton)
    {
      Delta = delta;
    }

    public int Delta { get; private set; }
  }
}

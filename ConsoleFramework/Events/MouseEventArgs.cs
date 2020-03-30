using ConsoleFramework.Controls;
using ConsoleFramework.Core;

namespace ConsoleFramework.Events
{
  public delegate void MouseEventHandler(object sender, MouseEventArgs e);

  public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);

  public delegate void MouseWheelEventHandler(object sender, MouseWheelEventArgs e);

  public class MouseEventArgs : RoutedEventArgs
  {
    public MouseEventArgs(object source, RoutedEvent routedEvent) :
      base(source, routedEvent)
    {
    }

    public MouseEventArgs(
      object source,
      RoutedEvent routedEvent,
      Point rawPosition,
      MouseButtonState leftButton,
      MouseButtonState middleButton,
      MouseButtonState rightButton)
      : base(source, routedEvent)
    {
      RawPosition = rawPosition;
      LeftButton = leftButton;
      MiddleButton = middleButton;
      RightButton = rightButton;
    }

    public Point RawPosition { get; }

    public MouseButtonState LeftButton { get; }

    public MouseButtonState MiddleButton { get; }

    public MouseButtonState RightButton { get; }

    /// <summary>
    /// Returns translated coords, relative to specified control.
    /// Can return negative values (or greater than ActualWidth/ActualHeight)
    /// if control is capturing mouse input.
    /// </summary>
    public Point GetPosition(Control relativeTo)
    {
      return Control.TranslatePoint(null, RawPosition, relativeTo);
    }
  }
}

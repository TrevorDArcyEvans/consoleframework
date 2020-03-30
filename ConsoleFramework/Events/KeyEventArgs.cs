using ConsoleFramework.Native;

namespace ConsoleFramework.Events
{
  public delegate void KeyEventHandler(object sender, KeyEventArgs args);

  public class KeyEventArgs : RoutedEventArgs
  {
    public KeyEventArgs(object source, RoutedEvent routedEvent) :
      base(source, routedEvent)
    {
    }

    public bool KeyDown;
    public ushort RepeatCount;
    public VirtualKeys VirtualKeyCode;
    public ushort VirtualScanCode;
    public char UnicodeChar;
    public ControlKeyState ControlKeyState;
  }
}

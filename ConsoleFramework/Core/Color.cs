using ConsoleFramework.Native;

namespace ConsoleFramework.Core
{
  /// <summary>
  /// Set of predefined colors.
  /// </summary>
  public enum Color : ushort
  {
    Black = 0x0000,
    DarkBlue = 0x0001,
    DarkGreen = 0x0002,
    DarkRed = 0x0004,

    DarkCyan = DarkBlue | DarkGreen,
    DarkMagenta = DarkBlue | DarkRed,
    DarkYellow = DarkGreen | DarkRed,
    Gray = DarkRed | DarkGreen | DarkBlue,

    DarkGray = Black | Attr.FOREGROUND_INTENSITY,
    Blue = DarkBlue | Attr.FOREGROUND_INTENSITY,
    Green = DarkGreen | Attr.FOREGROUND_INTENSITY,
    Red = DarkRed | Attr.FOREGROUND_INTENSITY,

    Cyan = DarkCyan | Attr.FOREGROUND_INTENSITY,
    Magenta = DarkMagenta | Attr.FOREGROUND_INTENSITY,
    Yellow = DarkYellow | Attr.FOREGROUND_INTENSITY,
    White = Gray | Attr.FOREGROUND_INTENSITY
  }
}

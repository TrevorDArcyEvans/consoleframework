using System;

namespace ConsoleFramework.Native
{
  /// <summary>
  /// Structure to retrieve terminal size.
  /// </summary>
  public struct Winsize
  {
    public UInt16 ws_row;
    public UInt16 ws_col;
    public UInt16 ws_xpixel;
    public UInt16 ws_ypixel;
  }
}

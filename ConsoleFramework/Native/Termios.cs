using System;
using System.Runtime.InteropServices;

namespace ConsoleFramework.Native
{
  [StructLayout(LayoutKind.Sequential)]
  public struct Termios
  {
    public UInt32 c_iflag;
    public UInt32 c_oflag;
    public UInt32 c_cflag;
    public UInt32 c_lflag;
    public Byte c_line;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public Byte[] c_cc; // 32 _items

    public UInt32 c_ispeed;
    public UInt32 c_ospeed;
  }
}

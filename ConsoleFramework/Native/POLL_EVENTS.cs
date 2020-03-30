using System;

namespace ConsoleFramework.Native
{
  [Flags]
  public enum POLL_EVENTS : ushort
  {
    NONE = 0x0000,
    POLLIN = 0x001,
    POLLPRI = 0x002,
    POLLOUT = 0x004,
    POLLMSG = 0x400,
    POLLREMOVE = 0x1000,
    POLLRDHUP = 0x2000,

    // output only
    POLLERR = 0x008,
    POLLHUP = 0x010,
    POLLNVAL = 0x020
  }
}

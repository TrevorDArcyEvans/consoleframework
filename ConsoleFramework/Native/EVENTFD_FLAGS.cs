using System;

namespace ConsoleFramework.Native
{
  [Flags]
  public enum EVENTFD_FLAGS : int
  {
    EFD_SEMAPHORE = 0x00000001,
    EFD_CLOEXEC = 0x00080000,
    EEFD_NONBLOCK = 0x00000800
  }
}

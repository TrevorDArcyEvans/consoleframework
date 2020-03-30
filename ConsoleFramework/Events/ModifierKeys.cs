using System;
using Xaml;

namespace ConsoleFramework.Events
{
  [Flags, TypeConverter(typeof(ModifierKeysConverter))]
  public enum ModifierKeys
  {
    Alt = 1,
    Control = 2,
    None = 0,
    Shift = 4
  }
}

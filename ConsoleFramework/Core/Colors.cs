using ConsoleFramework.Native;

namespace ConsoleFramework.Core
{
  public static class Colors
  {
    /// <summary>
    /// Blends foreground and background colors into one char attributes code.
    /// </summary>
    /// <param name="foreground">Foreground color</param>
    /// <param name="background">Background color</param>
    public static Attr Blend(Color foreground, Color background)
    {
      return (Attr) ((ushort) foreground + (((ushort) background) << 4));
    }
  }
}

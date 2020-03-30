using ConsoleFramework.Core;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Cannot be added in root menu.
  /// </summary>
  public class Separator : MenuItemBase
  {
    public Separator()
    {
      Focusable = false;

      // Stretch by default
      HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      return new Size(1, 1);
    }

    public override void Render(RenderingBuffer buffer)
    {
      Attr captionAttrs;
      if (HasFocus)
        captionAttrs = Colors.Blend(Color.Black, Color.DarkGreen);
      else
        captionAttrs = Colors.Blend(Color.Black, Color.Gray);

      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, UnicodeTable.SingleFrameHorizontal, captionAttrs);
    }
  }
}

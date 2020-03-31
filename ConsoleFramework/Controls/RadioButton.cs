using ConsoleFramework.Core;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  public class RadioButton : CheckBox
  {
    public override void Render(RenderingBuffer buffer)
    {
      Attr captionAttrs;
      if (HasFocus)
      {
        captionAttrs = Colors.Blend(Color.White, Color.DarkGreen);
      }
      else
      {
        captionAttrs = Colors.Blend(Color.Black, Color.DarkGreen);
      }

      var buttonAttrs = captionAttrs;

      buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);

      buffer.SetPixel(0, 0, _pressed ? '<' : '(', buttonAttrs);
      buffer.SetPixel(1, 0, Checked ? 'X' : ' ', buttonAttrs);
      buffer.SetPixel(2, 0, _pressed ? '>' : ')', buttonAttrs);
      buffer.SetPixel(3, 0, ' ', buttonAttrs);
      if (null != Caption)
      {
        RenderString(Caption, buffer, 4, 0, ActualWidth - 4, captionAttrs);
      }
    }
  }
}

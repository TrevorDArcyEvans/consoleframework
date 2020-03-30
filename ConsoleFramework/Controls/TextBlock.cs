using ConsoleFramework.Core;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using Xaml;

namespace ConsoleFramework.Controls
{
  [ContentProperty("Text")]
  public class TextBlock : Control
  {
    private void Initialize()
    {
    }

    public TextBlock()
    {
      Initialize();
    }

    private Color _color = Color.Black;

    public Color Color
    {
      get { return _color; }
      set
      {
        if (_color != value)
        {
          _color = value;
          Invalidate();
        }
      }
    }

    private string _text;

    public string Text
    {
      get { return _text; }
      set
      {
        if (_text != value)
        {
          _text = value;
          this.Invalidate();
        }
      }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      if (null != _text)
      {
        return new Size(_text.Length, 1);
      }

      return new Size(0, 0);
    }

    public override void Render(RenderingBuffer buffer)
    {
      var attr = Colors.Blend(_color, Color.DarkYellow);
      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);
      for (var x = 0; x < ActualWidth; ++x)
      {
        for (var y = 0; y < ActualHeight; ++y)
        {
          if (y == 0 && x < _text.Length)
          {
            buffer.SetPixel(x, y, _text[x], attr);
          }
        }
      }

      buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);
    }

    public override string ToString()
    {
      return "TextBlock";
    }
  }
}

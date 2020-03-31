using System;
using ConsoleFramework.Core;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using Xaml;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Контрол, который может состоять из других контролов.
  /// Позиционирует входящие в него контролы в соответствии с внутренним поведением панели и
  /// заданными свойствами дочерних контролов.
  /// Как и все контролы, связан с виртуальным канвасом.
  /// Может быть самым первым контролом программы (окно не может, к примеру, оно может существовать
  /// только в рамках хоста окон).
  /// </summary>
  [ContentProperty("Children")]
  public class Panel : Control
  {
    public Panel()
    {
      _children = new UIElementCollection(this);
    }

    public Attr Background { get; set; }

    private Orientation _orientation = Orientation.Vertical;

    public Orientation Orientation
    {
      get { return _orientation; }
      set
      {
        if (_orientation != value)
        {
          _orientation = value;
          this.Invalidate();
        }
      }
    }

    private readonly UIElementCollection _children;

    public new UIElementCollection Children
    {
      get { return _children; }
    }

    /// <summary>
    /// Размещает элементы вертикально, самым простым методом.
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns></returns>
    protected override Size MeasureOverride(Size availableSize)
    {
      if (_orientation == Orientation.Vertical)
      {
        var totalHeight = 0;
        var maxWidth = 0;
        foreach (var child in base.Children)
        {
          child.Measure(availableSize);
          totalHeight += child.DesiredSize.Height;
          if (child.DesiredSize.Width > maxWidth)
          {
            maxWidth = child.DesiredSize.Width;
          }
        }

        foreach (var child in base.Children)
        {
          child.Measure(new Size(maxWidth, child.DesiredSize.Height));
        }

        return new Size(maxWidth, totalHeight);
      }
      else
      {
        var totalWidth = 0;
        var maxHeight = 0;
        foreach (var child in base.Children)
        {
          child.Measure(availableSize);
          totalWidth += child.DesiredSize.Width;
          if (child.DesiredSize.Height > maxHeight)
          {
            maxHeight = child.DesiredSize.Height;
          }
        }

        foreach (var child in base.Children)
        {
          child.Measure(new Size(child.DesiredSize.Width, maxHeight));
        }

        return new Size(totalWidth, maxHeight);
      }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      if (_orientation == Orientation.Vertical)
      {
        var totalHeight = 0;
        var maxWidth = 0;
        foreach (var child in base.Children)
        {
          if (child.DesiredSize.Width > maxWidth)
          {
            maxWidth = child.DesiredSize.Width;
          }
        }

        maxWidth = Math.Max(maxWidth, finalSize.Width);
        foreach (var child in base.Children)
        {
          var y = totalHeight;
          var height = child.DesiredSize.Height;
          child.Arrange(new Rect(0, y, maxWidth, height));
          totalHeight += height;
        }

        return finalSize;
      }
      else
      {
        var totalWidth = 0;
        var maxHeight = 0;
        foreach (var child in base.Children)
        {
          if (child.DesiredSize.Height > maxHeight)
          {
            maxHeight = child.DesiredSize.Height;
          }
        }

        maxHeight = Math.Max(maxHeight, finalSize.Height);
        foreach (var child in base.Children)
        {
          var x = totalWidth;
          var width = child.DesiredSize.Width;
          child.Arrange(new Rect(x, 0, width, maxHeight));
          totalWidth += width;
        }

        return finalSize;
      }
    }

    /// <summary>
    /// Рисует исключительно себя - просто фон.
    /// </summary>
    /// <param name="buffer"></param>
    public override void Render(RenderingBuffer buffer)
    {
      for (var x = 0; x < ActualWidth; ++x)
      {
        for (var y = 0; y < ActualHeight; ++y)
        {
          buffer.SetPixel(x, y, ' ', Attr.BACKGROUND_BLUE |
                                     Attr.BACKGROUND_GREEN | Attr.BACKGROUND_RED | Attr.FOREGROUND_BLUE |
                                     Attr.FOREGROUND_GREEN | Attr.FOREGROUND_RED | Attr.FOREGROUND_INTENSITY);
          buffer.SetOpacity(x, y, 4);
        }
      }
    }
  }
}

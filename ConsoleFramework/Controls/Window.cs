using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Window is a control that can hold one child only.
  /// Usually a child is some panel or grid (or another layout control)
  /// Window exists in WindowsHost instance, so Window should be aware about WindowsHost
  /// and should be able to interoperate with it.
  /// </summary>
  public class Window : Control
  {
    public static RoutedEvent ActivatedEvent = EventManager.RegisterRoutedEvent("Activated", RoutingStrategy.Direct, typeof(EventHandler), typeof(Window));
    public static RoutedEvent DeactivatedEvent = EventManager.RegisterRoutedEvent("Deactivated", RoutingStrategy.Direct, typeof(EventHandler), typeof(Window));
    public static RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Direct, typeof(EventHandler), typeof(Window));
    public static RoutedEvent ClosingEvent = EventManager.RegisterRoutedEvent("Closing", RoutingStrategy.Direct, typeof(CancelEventHandler), typeof(Window));

    public string ChildToFocus { get; set; }

    public Window()
    {
      this.IsFocusScope = true;
      AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(Window_OnPreviewMouseDown));
      Initialize();
    }

    protected virtual void Initialize()
    {
      AddHandler(MouseDownEvent, new MouseButtonEventHandler(Window_OnMouseDown));
      AddHandler(MouseUpEvent, new MouseButtonEventHandler(Window_OnMouseUp));
      AddHandler(MouseMoveEvent, new MouseEventHandler(Window_OnMouseMove));
      AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
      AddHandler(ActivatedEvent, new EventHandler(Window_OnActivated));
      AddHandler(DeactivatedEvent, new EventHandler(Window_OnDeactivated));
    }

    /// <summary>
    /// Handles the mouse click: founds the end Focusable element which
    /// is placed under mouse and sets the focus to it.
    /// </summary>
    private void Window_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      PassFocusToChildUnderPoint(e);
    }

    protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs args)
    {
      if (args.wVirtualKeyCode == VirtualKeys.Tab)
      {
        ConsoleApplication.Instance.FocusManager.MoveFocusNext();
        args.Handled = true;
      }
    }

    /// <summary>
    /// Window coords in WindowsHost. In fact it is poor man attached properties.
    /// </summary>
    public int? X { get; set; }

    public int? Y { get; set; }

    public Control Content
    {
      get { return Children.Count != 0 ? Children[0] : null; }
      set
      {
        if (Children.Count != 0)
        {
          RemoveChild(Children[0]);
        }

        AddChild(value);
      }
    }

    private string _title;

    public string Title
    {
      get { return _title; }
      set
      {
        if (_title != value)
        {
          _title = value;
          Invalidate();
          RaisePropertyChanged("Title");
        }
      }
    }

    protected WindowsHost GetWindowsHost()
    {
      return (WindowsHost) Parent;
    }

    public static Size EMPTY_WINDOW_SIZE = new Size(12, 3);

    protected override Size MeasureOverride(Size availableSize)
    {
      if (Content == null)
      {
        return new Size(
          Math.Min(availableSize.Width, EMPTY_WINDOW_SIZE.Width + 4),
          Math.Min(availableSize.Height, EMPTY_WINDOW_SIZE.Height + 3)
        );
      }

      // Reserve 2 pixels for frame and 2/1 pixels for shadow
      var width = availableSize.Width != int.MaxValue ? Math.Max(4, availableSize.Width) - 4 : int.MaxValue;
      var height = availableSize.Height != int.MaxValue ? Math.Max(3, availableSize.Height) - 3 : int.MaxValue;
      Content.Measure(new Size(width, height));

      // Avoid int overflow. Additional -1 to avoid returning int.MaxValue from MeasureOverride (by contract)
      var result = new Size(
        Math.Min(int.MaxValue - 4 - 1, Content.DesiredSize.Width) + 4,
        Math.Min(int.MaxValue - 3 - 1, Content.DesiredSize.Height) + 3
      );
      return result;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      if (Content != null)
      {
        Content.Arrange(new Rect(1, 1,
          Math.Max(4, finalSize._width) - 4,
          Math.Max(3, finalSize._height) - 3));
      }

      return finalSize;
    }

    protected void RenderBorders(RenderingBuffer buffer, Point a, Point b, bool singleOrDouble, Attr attrs)
    {
      if (singleOrDouble)
      {
        // Corners
        buffer.SetPixel(a.X, a.Y, UnicodeTable.SingleFrameTopLeftCorner, attrs);
        buffer.SetPixel(b.X, b.Y, UnicodeTable.SingleFrameBottomRightCorner, attrs);
        buffer.SetPixel(a.X, b.Y, UnicodeTable.SingleFrameBottomLeftCorner, attrs);
        buffer.SetPixel(b.X, a.Y, UnicodeTable.SingleFrameTopRightCorner, attrs);

        // Horizontal & vertical frames
        buffer.FillRectangle(a.X + 1, a.Y, b.X - a.X - 1, 1, UnicodeTable.SingleFrameHorizontal, attrs);
        buffer.FillRectangle(a.X + 1, b.Y, b.X - a.X - 1, 1, UnicodeTable.SingleFrameHorizontal, attrs);
        buffer.FillRectangle(a.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.SingleFrameVertical, attrs);
        buffer.FillRectangle(b.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.SingleFrameVertical, attrs);
      }
      else
      {
        // Corners
        buffer.SetPixel(a.X, a.Y, UnicodeTable.DoubleFrameTopLeftCorner, attrs);
        buffer.SetPixel(b.X, b.Y, UnicodeTable.DoubleFrameBottomRightCorner, attrs);
        buffer.SetPixel(a.X, b.Y, UnicodeTable.DoubleFrameBottomLeftCorner, attrs);
        buffer.SetPixel(b.X, a.Y, UnicodeTable.DoubleFrameTopRightCorner, attrs);

        // Horizontal & vertical frames
        buffer.FillRectangle(a.X + 1, a.Y, b.X - a.X - 1, 1, UnicodeTable.DoubleFrameHorizontal, attrs);
        buffer.FillRectangle(a.X + 1, b.Y, b.X - a.X - 1, 1, UnicodeTable.DoubleFrameHorizontal, attrs);
        buffer.FillRectangle(a.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.DoubleFrameVertical, attrs);
        buffer.FillRectangle(b.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.DoubleFrameVertical, attrs);
      }
    }

    public override void Render(RenderingBuffer buffer)
    {
      var borderAttrs = _moving ? Colors.Blend(Color.Green, Color.Gray) : Colors.Blend(Color.White, Color.Gray);
      if (GetWindowsHost().TopWindow == this)
      {
        borderAttrs = Colors.Blend(Color.DarkBlue, Color.Gray);
      }

      // background
      buffer.FillRectangle(0, 0, this.ActualWidth, this.ActualHeight, ' ', borderAttrs);

      // Borders
      var bottomRight = new Point(ActualWidth - 3, ActualHeight - 2);
      RenderBorders(buffer, new Point(0, 0), bottomRight, this._moving || this._resizing, borderAttrs);

      // Additional green right bottom corner if _resizing
      if (_resizing)
      {
        buffer.SetPixel(bottomRight.X, bottomRight.Y, UnicodeTable.SingleFrameBottomRightCorner,
          Colors.Blend(Color.Green, Color.Gray));
      }

      // close button
      if (ActualWidth > 4)
      {
        buffer.SetPixel(2, 0, '[');
        buffer.SetPixel(3, 0, _showClosingGlyph ? UnicodeTable.WindowClosePressedSymbol : UnicodeTable.WindowCloseSymbol,
          Colors.Blend(Color.Green, Color.Gray));
        buffer.SetPixel(4, 0, ']');
      }

      // shadows
      buffer.SetOpacity(0, ActualHeight - 1, 2 + 4);
      buffer.SetOpacity(1, ActualHeight - 1, 2 + 4);
      buffer.SetOpacity(ActualWidth - 1, 0, 2 + 4);
      buffer.SetOpacity(ActualWidth - 2, 0, 2 + 4);
      buffer.SetOpacityRect(2, ActualHeight - 1, ActualWidth - 2, 1, 1 + 4);
      buffer.SetOpacityRect(ActualWidth - 2, 1, 2, ActualHeight - 1, 1 + 4);

      // _title
      if (!string.IsNullOrEmpty(Title))
      {
        var titleStartX = 7;
        var renderTitle = false;
        string renderTitleString = null;
        var availablePixelsCount = ActualWidth - titleStartX * 2;
        if (availablePixelsCount > 0)
        {
          renderTitle = true;
          if (Title.Length <= availablePixelsCount)
          {
            // dont truncate _title
            titleStartX += (availablePixelsCount - Title.Length) / 2;
            renderTitleString = Title;
          }
          else
          {
            renderTitleString = Title.Substring(0, availablePixelsCount);
            if (renderTitleString.Length > 2)
            {
              renderTitleString = renderTitleString.Substring(0, renderTitleString.Length - 2) + "..";
            }
            else
            {
              renderTitle = false;
            }
          }
        }

        if (renderTitle)
        {
          buffer.SetPixel(titleStartX - 1, 0, ' ', borderAttrs);
          for (var i = 0; i < renderTitleString.Length; i++)
          {
            buffer.SetPixel(titleStartX + i, 0, renderTitleString[i], borderAttrs);
          }

          buffer.SetPixel(titleStartX + renderTitleString.Length, 0, ' ', borderAttrs);
        }
      }
    }

    private bool _closing = false;
    private bool _showClosingGlyph = false;

    private bool _moving = false;
    private int _movingStartX;
    private int _movingStartY;
    private Point _movingStartPoint;

    private bool _resizing = false;
    private int _resizingStartWidth;
    private int _resizingStartHeight;
    private Point _resizingStartPoint;

    public void Window_OnMouseDown(object sender, MouseButtonEventArgs args)
    {
      // Moving is enabled only when windows is not _resizing, and vice versa
      if (!_moving && !_resizing && !_closing)
      {
        var point = args.GetPosition(this);
        var parentPoint = args.GetPosition(GetWindowsHost());
        if (point._y == 0 && point._x == 3)
        {
          _closing = true;
          _showClosingGlyph = true;
          ConsoleApplication.Instance.BeginCaptureInput(this);
          // _closing is started, we should redraw the border
          Invalidate();
          args.Handled = true;
        }
        else if (point._y == 0)
        {
          _moving = true;
          _movingStartPoint = parentPoint;
          _movingStartX = RenderSlotRect.TopLeft.X;
          _movingStartY = RenderSlotRect.TopLeft.Y;
          ConsoleApplication.Instance.BeginCaptureInput(this);
          // _moving is started, we should redraw the border
          Invalidate();
          args.Handled = true;
        }
        else if (point._x == ActualWidth - 3 && point._y == ActualHeight - 2)
        {
          _resizing = true;
          _resizingStartPoint = parentPoint;
          _resizingStartWidth = ActualWidth;
          _resizingStartHeight = ActualHeight;
          ConsoleApplication.Instance.BeginCaptureInput(this);
          // _resizing is started, we should redraw the border
          Invalidate();
          args.Handled = true;
        }
      }
    }

    public void Close()
    {
      this.HandleClosing();
    }

    protected void HandleClosing()
    {
      var args = new CancelEventArgs(this, ClosingEvent);

      ConsoleApplication.Instance.EventManager.ProcessRoutedEvent(ClosingEvent, args);

      if (!args.Cancel)
      {
        GetWindowsHost().CloseWindow(this);
      }
    }

    public void Window_OnMouseUp(object sender, MouseButtonEventArgs args)
    {
      if (_closing)
      {
        Point point = args.GetPosition(this);
        if (point._x == 3 && point._y == 0)
        {
          this.HandleClosing();
        }

        _closing = false;
        _showClosingGlyph = false;
        ConsoleApplication.Instance.EndCaptureInput(this);
        Invalidate();
        args.Handled = true;
      }

      if (_moving)
      {
        _moving = false;
        ConsoleApplication.Instance.EndCaptureInput(this);
        Invalidate();
        args.Handled = true;
      }

      if (_resizing)
      {
        _resizing = false;
        ConsoleApplication.Instance.EndCaptureInput(this);
        Invalidate();
        args.Handled = true;
      }
    }

    public void Window_OnMouseMove(object sender, MouseEventArgs args)
    {
      if (_closing)
      {
        var point = args.GetPosition(this);
        var anyChanged = false;
        if (point._x == 3 && point._y == 0)
        {
          if (!_showClosingGlyph)
          {
            _showClosingGlyph = true;
            anyChanged = true;
          }
        }
        else
        {
          if (_showClosingGlyph)
          {
            _showClosingGlyph = false;
            anyChanged = true;
          }
        }

        if (anyChanged)
          Invalidate();
        args.Handled = true;
      }

      if (_moving)
      {
        var parentPoint = args.GetPosition(GetWindowsHost());
        var vector = new Vector(parentPoint.X - _movingStartPoint._x, parentPoint.Y - _movingStartPoint._y);
        X = _movingStartX + vector.X;
        Y = _movingStartY + vector.Y;
        GetWindowsHost().Invalidate();
        args.Handled = true;
      }

      if (_resizing)
      {
        var parentPoint = args.GetPosition(GetWindowsHost());
        var deltaWidth = parentPoint.X - _resizingStartPoint._x;
        var deltaHeight = parentPoint.Y - _resizingStartPoint._y;
        var width = _resizingStartWidth + deltaWidth;
        var height = _resizingStartHeight + deltaHeight;
        var anyChanged = false;
        if (width >= 4)
        {
          this.Width = width;
          anyChanged = true;
        }

        if (height >= 3)
        {
          this.Height = height;
          anyChanged = true;
        }

        if (anyChanged)
        {
          Invalidate();
        }

        args.Handled = true;
      }
    }

    public void Window_OnActivated(object sender, EventArgs args)
    {
      Invalidate();
    }

    public void Window_OnDeactivated(object sender, EventArgs args)
    {
      Invalidate();
    }

    public event CancelEventHandler Closing
    {
      add => AddHandler(ClosingEvent, value);
      remove => RemoveHandler(ClosingEvent, value);
    }

    public event EventHandler Closed
    {
      add => AddHandler(ClosedEvent, value);
      remove => RemoveHandler(ClosedEvent, value);
    }
  }
}

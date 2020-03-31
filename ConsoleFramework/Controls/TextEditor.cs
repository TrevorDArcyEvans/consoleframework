using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using Xaml;

// TODO : Autoindent
// TODO : Ctrl+Home/Ctrl+End
// TODO : Alt+Backspace deletes word
// TODO : Shift+Delete deletes line
// TODO : Scrollbars full support
// TODO : Ctrl+arrows
// TODO : Selection
// TODO : Selection copy/paste/cut/delete
// TODO : Undo/Redo, commands autogrouping
// TODO : Read only mode
// TODO : Tabs (converting to spaces when loading?)
namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Multiline text editor.
  /// </summary>
  [ContentProperty("Text")]
  public class TextEditor : Control
  {
    private TextEditorController _controller;
    private char[,] _buffer;
    private ScrollBar _horizontalScrollbar;
    private ScrollBar _verticalScrollbar;

    public string Text
    {
      get => _controller.Text;
      set
      {
        if (value != _controller.Text)
        {
          _controller.Text = value;
          CursorPosition = _controller.CursorPos;
          Invalidate();
        }
      }
    }

    // TODO : Scrollbars always visible

    private void ApplyCommand(TextEditorController.ICommand cmd)
    {
      var oldCursorPos = _controller.CursorPos;
      if (cmd.Do(_controller))
      {
        Invalidate();
      }

      if (oldCursorPos != _controller.CursorPos)
      {
        CursorPosition = _controller.CursorPos;
      }
    }

    public TextEditor()
    {
      _controller = new TextEditorController("", 0, 0);
      KeyDown += OnKeyDown;
      MouseDown += OnMouseDown;
      CursorVisible = true;
      CursorPosition = new Point(0, 0);
      Focusable = true;

      _horizontalScrollbar = new ScrollBar
      {
        Orientation = Orientation.Horizontal,
        Visibility = Visibility.Hidden
      };
      _verticalScrollbar = new ScrollBar
      {
        Orientation = Orientation.Vertical,
        Visibility = Visibility.Hidden
      };
      AddChild(_horizontalScrollbar);
      AddChild(_verticalScrollbar);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      _verticalScrollbar.Measure(new Size(1, availableSize.Height));
      _horizontalScrollbar.Measure(new Size(availableSize.Width, 1));
      return new Size(0, 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      if (_controller.LinesCount > finalSize.Height)
      {
        _verticalScrollbar.Visibility = Visibility.Visible;
        _verticalScrollbar.MaxValue = _controller.LinesCount + TextEditorController.LINES_BOTTOM_MAX_GAP - _controller.Window.Height;
        _verticalScrollbar.Value = _controller.Window.Top;
        _verticalScrollbar.Invalidate();
      }
      else
      {
        _verticalScrollbar.Visibility = Visibility.Collapsed;
        _verticalScrollbar.Value = 0;
        _verticalScrollbar.MaxValue = 10;
      }

      if (_controller.ColumnsCount >= finalSize.Width)
      {
        _horizontalScrollbar.Visibility = Visibility.Visible;
        _horizontalScrollbar.MaxValue = _controller.ColumnsCount + TextEditorController.COLUMNS_RIGHT_MAX_GAP - _controller.Window.Width;
        _horizontalScrollbar.Value = _controller.Window.Left;
        _horizontalScrollbar.Invalidate();
      }
      else
      {
        _horizontalScrollbar.Visibility = Visibility.Collapsed;
        _horizontalScrollbar.Value = 0;
        _horizontalScrollbar.MaxValue = 10;
      }

      _horizontalScrollbar.Arrange(new Rect(
        0,
        Math.Max(0, finalSize.Height - 1),
        Math.Max(0, finalSize.Width -
                    (_verticalScrollbar.Visibility == Visibility.Visible
                     || _horizontalScrollbar.Visibility != Visibility.Visible
                      ? 1
                      : 0)),
        1
      ));
      _verticalScrollbar.Arrange(new Rect(
        Math.Max(0, finalSize.Width - 1),
        0,
        1,
        Math.Max(0, finalSize.Height -
                    (_horizontalScrollbar.Visibility == Visibility.Visible
                     || _verticalScrollbar.Visibility != Visibility.Visible
                      ? 1
                      : 0))
      ));
      var contentSize = new Size(
        Math.Max(0, finalSize.Width - (_verticalScrollbar.Visibility == Visibility.Visible ? 1 : 0)),
        Math.Max(0, finalSize.Height - (_horizontalScrollbar.Visibility == Visibility.Visible ? 1 : 0))
      );
      _controller.Window = new Rect(_controller.Window.TopLeft, contentSize);
      _buffer = new char[contentSize.Height, contentSize.Width];
      return finalSize;
    }

    public override void Render(RenderingBuffer buffer)
    {
      var attrs = Colors.Blend(Color.Green, Color.DarkBlue);
      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attrs);

      _controller.WriteToWindow(this._buffer);
      var contentSize = _controller.Window.Size;
      for (var y = 0; y < contentSize.Height; y++)
      {
        for (var x = 0; x < contentSize.Width; x++)
        {
          buffer.SetPixel(x, y, this._buffer[y, x]);
        }
      }

      if (_verticalScrollbar.Visibility == Visibility.Visible
          && _horizontalScrollbar.Visibility == Visibility.Visible)
      {
        buffer.SetPixel(buffer.Width - 1, buffer.Height - 1,
          UnicodeTable.SingleFrameBottomRightCorner,
          Colors.Blend(Color.DarkCyan, Color.DarkBlue));
      }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
      var position = mouseButtonEventArgs.GetPosition(this);
      var constrained = new Point(
        Math.Max(0, Math.Min(_controller.Window.Size.Width - 1, position.X)),
        Math.Max(0, Math.Min(_controller.Window.Size.Height - 1, position.Y))
      );
      ApplyCommand(new TextEditorController.TrySetCursorCmd(constrained));
      mouseButtonEventArgs.Handled = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
      var keyInfo = new ConsoleKeyInfo(args.UnicodeChar,
        (ConsoleKey) args.VirtualKeyCode,
        (args.ControlKeyState & ControlKeyState.SHIFT_PRESSED) == ControlKeyState.SHIFT_PRESSED,
        (args.ControlKeyState & ControlKeyState.LEFT_ALT_PRESSED) == ControlKeyState.LEFT_ALT_PRESSED
        || (args.ControlKeyState & ControlKeyState.RIGHT_ALT_PRESSED) == ControlKeyState.RIGHT_ALT_PRESSED,
        (args.ControlKeyState & ControlKeyState.LEFT_CTRL_PRESSED) == ControlKeyState.LEFT_CTRL_PRESSED
        || (args.ControlKeyState & ControlKeyState.RIGHT_CTRL_PRESSED) == ControlKeyState.RIGHT_CTRL_PRESSED
      );
      if (!char.IsControl(keyInfo.KeyChar))
      {
        ApplyCommand(new TextEditorController.AppendStringCmd(new string(keyInfo.KeyChar, 1)));
      }

      switch (keyInfo.Key)
      {
        case ConsoleKey.Enter:
          ApplyCommand(new TextEditorController.AppendStringCmd(Environment.NewLine));
          break;

        case ConsoleKey.UpArrow:
          ApplyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Up));
          break;

        case ConsoleKey.DownArrow:
          ApplyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Down));
          break;

        case ConsoleKey.LeftArrow:
          ApplyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Left));
          break;

        case ConsoleKey.RightArrow:
          ApplyCommand(new TextEditorController.MoveCursorCmd(TextEditorController.Direction.Right));
          break;

        case ConsoleKey.Backspace:
          ApplyCommand(new TextEditorController.DeleteLeftSymbolCmd());
          break;

        case ConsoleKey.Delete:
          ApplyCommand(new TextEditorController.DeleteRightSymbolCmd());
          break;

        case ConsoleKey.PageDown:
          ApplyCommand(new TextEditorController.PageDownCmd());
          break;

        case ConsoleKey.PageUp:
          ApplyCommand(new TextEditorController.PageUpCmd());
          break;

        case ConsoleKey.Home:
          ApplyCommand(new TextEditorController.HomeCommand());
          break;

        case ConsoleKey.End:
          ApplyCommand(new TextEditorController.EndCommand());
          break;
      }
    }
  }
}

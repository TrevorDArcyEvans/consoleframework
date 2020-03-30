using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Incapsulates text holder and all the data required to display the content
  /// properly in predictable way. Should be covered by unit tests. Unit tests
  /// can be written easy using steps like theese:
  /// 1. Initialize using some text and initial _cursorPos, window values
  /// 2. Apply some commands
  /// 3. Check the result state
  /// </summary>
  public partial class TextEditorController
  {
    /// <summary>
    /// Gap shown after scrolling to the very end of document
    /// </summary>
    public const int LINES_BOTTOM_MAX_GAP = 4;

    /// <summary>
    /// Gap shown after typing last character in line if there is no remaining space
    /// (and when End key was _pressed)
    /// </summary>
    public const int COLUMNS_RIGHT_MAX_GAP = 3;

    /// <summary>
    /// Gap to left char of line when returning to the line which was out of window
    /// (if window moves from right to left)
    /// </summary>
    public const int COLUMNS_LEFT_GAP = 4;

    private Point _cursorPos;

    /// <summary>
    /// Logical cursor position (points to symbol in textItems, not to display coord)
    /// </summary>
    public Point CursorPos
    {
      get => _cursorPos;
      set
      {
        _cursorPos = value;
        _lastTextPosX = CursorPosToTextPos(_cursorPos, Window).X;
      }
    }

    /// <summary>
    /// Stores the last X coord of cursor, before line was changed
    /// (when PageUp/PageDown/ArrowUp/ArrowDown _pressed)
    /// </summary>
    private int _lastTextPosX;

    /// <summary>
    /// Changes cursor position without changing lastCursorX value
    /// </summary>
    private void SetCursorPosLight(Point cursorPos)
    {
      this._cursorPos = cursorPos;
    }

    /// <summary>
    /// Current display window
    /// </summary>
    public Rect Window { get; set; }

    public void WriteToWindow(char[,] buffer)
    {
      _textHolder.WriteToWindow(Window.Left, Window.Top, Window.Width, Window.Height, buffer);
    }

    // TODO : property
    private string _newLine;

    /// <summary>
    /// Current text in editor
    /// </summary>
    private TextHolder _textHolder;
    public string Text
    {
      get => _textHolder.Text;
      set
      {
        if (_textHolder.Text != value)
        {
          string newLineToUse;
          if (_newLine == null)
          {
            // Auto-detect newline format
            newLineToUse = DetectNewLine(value);
          }
          else
          {
            newLineToUse = _newLine;
          }

          _textHolder = new TextHolder(value, newLineToUse);
          CursorPos = new Point();
          Window = new Rect(new Point(), Window.Size);
        }
      }
    }

    /// <summary>
    /// Since XamlReader passes \n-delimited lines in all platforms
    /// (without looking what really is in CDATA, for example), we should provide
    /// auto-detection feature according to the principle of least astonishment.
    /// Then, one xaml-file can be used in different platforms without changes.
    /// </summary>
    private static string DetectNewLine(string text)
    {
      if (text.Contains("\r\n"))
      {
        return "\r\n";
      }

      return "\n";
    }

    public int LinesCount => _textHolder.LinesCount;
    public int ColumnsCount => _textHolder.ColumnsCount;

    public TextEditorController(string text, int width, int height) :
      this(new TextHolder(text), new Point(), new Rect(0, 0, width, height))
    {
    }

    public TextEditorController(TextHolder textHolder, Point cursorPos, Rect window)
    {
      this._textHolder = textHolder;
      this.CursorPos = cursorPos;
      this.Window = window;
    }

    private static Point CursorPosToTextPos(Point cursorPos, Rect window)
    {
      cursorPos.Offset(window.X, window.Y);
      return cursorPos;
    }

    private static Point TextPosToCursorPos(Point textPos, Rect window)
    {
      textPos.Offset(-window.X, -window.Y);
      return textPos;
    }

    /// <summary>
    /// Moves window to make the cursor visible in it
    /// </summary>
    private static void MoveWindowToCursor(Point cursor, TextEditorController controller, bool light = false)
    {
      var oldWindow = controller.Window;

      int? windowX;
      int? windowY;

      if (cursor.X >= oldWindow.Width)
      {
        // Move window 3px right if nextChar is outside the window after add char
        windowX = oldWindow.X + cursor.X - oldWindow.Width + COLUMNS_RIGHT_MAX_GAP;
      }
      else if (cursor.X < 0)
      {
        // Move window left if need (with 4px gap from left)
        windowX = Math.Max(0, oldWindow.X + cursor.X - COLUMNS_LEFT_GAP);
      }
      else
      {
        windowX = null;
      }

      // Move window down if nextChar is outside the window
      if (cursor.Y >= controller.Window.Height)
      {
        windowY = controller.Window.Top + cursor.Y - controller.Window.Height + 1;
      }
      else if (cursor.Y < 0)
      {
        windowY = controller.Window.Y + cursor.Y;
      }
      else
      {
        windowY = null;
      }

      if (windowX != null || windowY != null)
      {
        controller.Window = new Rect(
          new Point(windowX ?? oldWindow.X, windowY ?? oldWindow.Y), oldWindow.Size);
      }

      // Actualize cursor position to new window
      var cursorPos = TextPosToCursorPos(CursorPosToTextPos(cursor, oldWindow), controller.Window);
      if (light)
      {
        controller.SetCursorPosLight(cursorPos);
      }
      else
      {
        controller.CursorPos = cursorPos;
      }
    }
  }
}

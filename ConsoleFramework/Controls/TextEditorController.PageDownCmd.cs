using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public class PageDownCmd : ICommand
    {
      public bool Do(TextEditorController controller)
      {
        var oldCursorPos = controller.CursorPos;
        var oldWindow = controller.Window;
        var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);

        // Scroll window one page down
        var maxWindowY = controller._textHolder.LinesCount + LINES_BOTTOM_MAX_GAP - controller.Window.Height;
        if (controller.Window.Y < maxWindowY)
        {
          var y = Math.Min(controller.Window.Y + controller.Window.Height, maxWindowY);
          controller.Window = new Rect(new Point(controller.Window.X, y), controller.Window.Size);
        }

        // Move cursor down too
        var window = controller.Window;
        Point textPos;
        if (oldTextPos.Y == controller._textHolder.LinesCount - 1)
        {
          var lastLine = controller._textHolder.Lines[controller._textHolder.LinesCount - 1];
          if (oldTextPos.X == lastLine.Length)
          {
            textPos = oldTextPos;
          }
          else
          {
            textPos = new Point(lastLine.Length, controller._textHolder.LinesCount - 1);
          }
        }
        else
        {
          var lineIndex = Math.Min(oldTextPos.Y + window.Height, controller.LinesCount - 1);
          var line = controller._textHolder.Lines[lineIndex];
          int x;
          if (oldTextPos.Y + window.Height > lineIndex)
          {
            x = line.Length;
          }
          else
          {
            x = Math.Min(controller._lastTextPosX, line.Length);
          }

          textPos = new Point(x, lineIndex);
        }

        // Actualize cursor
        MoveWindowToCursor(TextPosToCursorPos(textPos, window), controller, true);

        return oldWindow != controller.Window || oldCursorPos != controller.CursorPos;
      }
    }
  }
}

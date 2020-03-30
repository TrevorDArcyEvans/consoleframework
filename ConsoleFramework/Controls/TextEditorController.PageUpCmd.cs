using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public class PageUpCmd : ICommand
    {
      public bool Do(TextEditorController controller)
      {
        var oldCursorPos = controller.CursorPos;
        var oldWindow = controller.Window;
        var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);

        // Scroll window one page up
        if (controller.Window.Y > 0)
        {
          int y = Math.Max(0, controller.Window.Y - controller.Window.Height);
          controller.Window = new Rect(new Point(controller.Window.X, y), controller.Window.Size);
        }

        // Move cursor up too
        var window = controller.Window;
        Point textPos;
        if (oldTextPos.Y == 0)
        {
          textPos = oldTextPos.X == 0 ? oldTextPos : new Point(0, 0);
        }
        else
        {
          var lineIndex = Math.Max(0, oldTextPos.Y - window.Height);
          var line = controller._textHolder.Lines[lineIndex];
          int x;
          if (oldTextPos.Y - window.Height < 0)
          {
            x = 0;
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

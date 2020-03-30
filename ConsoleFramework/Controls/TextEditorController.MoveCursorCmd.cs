using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public class MoveCursorCmd : ICommand
    {
      private readonly Direction _direction;

      public MoveCursorCmd(Direction direction)
      {
        this._direction = direction;
      }

      public bool Do(TextEditorController controller)
      {
        var oldCursorPos = controller.CursorPos;
        var oldWindow = controller.Window;
        switch (_direction)
        {
          case Direction.Up:
          {
            Point oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);
            Point textPos;
            if (oldTextPos.Y == 0)
            {
              if (oldTextPos.X == 0)
              {
                break;
              }

              textPos = new Point(0, 0);
            }
            else
            {
              string prevLine = controller._textHolder.Lines[oldTextPos.Y - 1];
              textPos = new Point(
                Math.Min(controller._lastTextPosX, prevLine.Length),
                oldTextPos.Y - 1
              );
            }

            MoveWindowToCursor(TextPosToCursorPos(textPos, oldWindow), controller, true);
            break;
          }

          case Direction.Down:
          {
            var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);
            Point textPos;
            if (oldTextPos.Y == controller._textHolder.LinesCount - 1)
            {
              string lastLine = controller._textHolder.Lines[controller._textHolder.LinesCount - 1];
              if (oldTextPos.X == lastLine.Length)
              {
                break;
              }

              textPos = new Point(lastLine.Length, controller._textHolder.LinesCount - 1);
            }
            else
            {
              var nextLine = controller._textHolder.Lines[oldTextPos.Y + 1];
              textPos = new Point(
                Math.Min(controller._lastTextPosX, nextLine.Length),
                oldTextPos.Y + 1
              );
            }

            MoveWindowToCursor(TextPosToCursorPos(textPos, oldWindow), controller, true);
            break;
          }

          case Direction.Left:
          {
            var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);
            Point textPos;
            if (oldTextPos.X == 0)
            {
              if (oldTextPos.Y == 0)
              {
                break;
              }

              var prevLine = controller._textHolder.Lines[oldTextPos.Y - 1];
              textPos = new Point(prevLine.Length, oldTextPos.Y - 1);
            }
            else
            {
              textPos = new Point(oldTextPos.X - 1, oldTextPos.Y);
            }

            MoveWindowToCursor(TextPosToCursorPos(textPos, oldWindow), controller);
            break;
          }

          case Direction.Right:
          {
            var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);
            Point textPos;
            if (oldTextPos.Y == controller._textHolder.LinesCount - 1)
            {
              var lastLine = controller._textHolder.Lines[controller._textHolder.LinesCount - 1];
              if (oldTextPos.X == lastLine.Length)
              {
                break;
              }

              textPos = new Point(oldTextPos.X + 1, oldTextPos.Y);
            }
            else
            {
              var line = controller._textHolder.Lines[oldTextPos.Y];
              if (oldTextPos.X < line.Length)
              {
                textPos = new Point(oldTextPos.X + 1, oldTextPos.Y);
              }
              else
              {
                textPos = new Point(0, oldTextPos.Y + 1);
              }
            }

            MoveWindowToCursor(TextPosToCursorPos(textPos, oldWindow), controller);
            break;
          }
        }

        return controller.Window != oldWindow;
      }
    }
  }
}

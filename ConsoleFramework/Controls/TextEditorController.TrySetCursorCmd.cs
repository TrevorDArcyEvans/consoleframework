using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    /// <summary>
    /// Command accepts _coord from mouse and applies it to current text window,
    /// don't allowing to set cursor out of filled text
    /// </summary>
    public class TrySetCursorCmd : ICommand
    {
      private readonly Point _coord;

      public TrySetCursorCmd(Point coord)
      {
        this._coord = coord;
      }

      public bool Do(TextEditorController controller)
      {
        if (!new Rect(new Point(), controller.Window.Size).Contains(_coord))
        {
          throw new ArgumentException("_coord should be inside window");
        }

        var desiredTextPos = CursorPosToTextPos(_coord, controller.Window);
        var y = Math.Min(desiredTextPos.Y, controller._textHolder.LinesCount - 1);
        var x = Math.Min(desiredTextPos.X, controller._textHolder.Lines[y].Length);

        MoveWindowToCursor(TextPosToCursorPos(new Point(x, y), controller.Window), controller);

        return false;
      }
    }
  }
}

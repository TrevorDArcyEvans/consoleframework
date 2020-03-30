using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    /// <summary>
    /// Initiated with BackSpace key
    /// </summary>
    public class DeleteLeftSymbolCmd : ICommand
    {
      public bool Do(TextEditorController controller)
      {
        var toTextPos = CursorPosToTextPos(controller.CursorPos, controller.Window);
        Point fromTextPos;
        if (toTextPos.X == 0)
        {
          if (toTextPos.Y == 0)
          {
            return false;
          }

          fromTextPos = new Point(controller._textHolder.Lines[toTextPos.Y - 1].Length, toTextPos.Y - 1);
        }
        else
        {
          fromTextPos = new Point(toTextPos.X - 1, toTextPos.Y);
        }

        controller._textHolder.Delete(fromTextPos.Y, fromTextPos.X, toTextPos.Y, toTextPos.X);
        MoveWindowToCursor(TextPosToCursorPos(fromTextPos, controller.Window), controller);

        return true;
      }
    }
  }
}

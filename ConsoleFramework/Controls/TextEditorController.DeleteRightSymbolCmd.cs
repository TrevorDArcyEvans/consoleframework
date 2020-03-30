using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    /// <summary>
    /// Initiated with Delete key
    /// </summary>
    public class DeleteRightSymbolCmd : ICommand
    {
      public bool Do(TextEditorController controller)
      {
        var fromTextPos = CursorPosToTextPos(controller.CursorPos, controller.Window);
        Point toTextPos;
        var line = controller._textHolder.Lines[fromTextPos.Y];
        if (fromTextPos.X == line.Length)
        {
          if (fromTextPos.Y == controller._textHolder.LinesCount - 1)
          {
            return false;
          }

          toTextPos = new Point(0, fromTextPos.Y + 1);
        }
        else
        {
          toTextPos = new Point(fromTextPos.X + 1, fromTextPos.Y);
        }

        controller._textHolder.Delete(fromTextPos.Y, fromTextPos.X, toTextPos.Y, toTextPos.X);
        MoveWindowToCursor(TextPosToCursorPos(fromTextPos, controller.Window), controller);

        return true;
      }
    }
  }
}

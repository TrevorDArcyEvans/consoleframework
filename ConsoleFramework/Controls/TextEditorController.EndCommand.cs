using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public class EndCommand : ICommand
    {
      public bool Do(TextEditorController controller)
      {
        var oldCursorPos = controller.CursorPos;
        var oldWindow = controller.Window;
        var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);

        var line = controller._textHolder.Lines[oldTextPos.Y];
        var textPos = new Point(line.Length, oldTextPos.Y);

        MoveWindowToCursor(TextPosToCursorPos(textPos, oldWindow), controller);

        return oldWindow != controller.Window || oldCursorPos != controller.CursorPos;
      }
    }
  }
}

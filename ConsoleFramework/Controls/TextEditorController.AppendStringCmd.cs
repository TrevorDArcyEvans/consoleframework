using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public class AppendStringCmd : ICommand
    {
      private readonly string s;

      public AppendStringCmd(string s)
      {
        this.s = s;
      }

      public bool Do(TextEditorController controller)
      {
        Point textPos = CursorPosToTextPos(controller.CursorPos, controller.Window);
        Point nextCharPos =
          controller._textHolder.Insert(textPos.Y, textPos.X, s);

        // Move window to just edited place if need
        Point cursor = TextPosToCursorPos(nextCharPos, controller.Window);

        MoveWindowToCursor(cursor, controller);

        return true;
      }
    }
  }
}

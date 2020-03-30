using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public class HomeCommand : ICommand
    {
      public bool Do(TextEditorController controller)
      {
        var oldCursorPos = controller.CursorPos;
        var oldWindow = controller.Window;
        var oldTextPos = CursorPosToTextPos(oldCursorPos, oldWindow);

        var line = controller._textHolder.Lines[oldTextPos.Y];
        var homeIndex = HomeOfLine(line);
        int x;
        if (oldTextPos.X == 0)
        {
          x = homeIndex;
        }
        else
        {
          x = oldTextPos.X <= homeIndex ? 0 : homeIndex;
        }

        var textPos = new Point(x, oldTextPos.Y);

        MoveWindowToCursor(TextPosToCursorPos(textPos, oldWindow), controller);

        return oldWindow != controller.Window || oldCursorPos != controller.CursorPos;
      }

      /// <summary>
      /// Returns index of first non-space symbol (or s.Length if there is no non-space symbols)
      /// </summary>
      private int HomeOfLine(string s)
      {
        for (var i = 0; i < s.Length; i++)
        {
          if (!char.IsWhiteSpace(s[i]))
          {
            return i;
          }
        }

        return s.Length;
      }
    }
  }
}

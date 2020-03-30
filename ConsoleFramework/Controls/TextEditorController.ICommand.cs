namespace ConsoleFramework.Controls
{
  public partial class TextEditorController
  {
    public interface ICommand
    {
      /// <summary>
      /// Returns true if visible content has changed during the operation
      /// (and therefore should be invalidated), false otherwise.
      /// </summary>
      bool Do(TextEditorController controller);
    }
  }
}

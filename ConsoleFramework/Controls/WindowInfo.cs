namespace ConsoleFramework.Controls
{
  public sealed class WindowInfo
  {
    public readonly bool Modal;
    public readonly bool OutsideClickClosesWindow;

    public WindowInfo(bool modal, bool outsideClickClosesWindow)
    {
      Modal = modal;
      OutsideClickClosesWindow = outsideClickClosesWindow;
    }
  }
}

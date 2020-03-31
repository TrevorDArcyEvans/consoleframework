using ConsoleFramework.Controls;

namespace ConsoleFramework.Rendering
{
  public partial class Renderer
  {
    private struct ControlAffectInfo
    {
      public readonly Control Control;
      public readonly AffectType AffectType;

      public ControlAffectInfo(Control control, AffectType affectType)
      {
        this.Control = control;
        this.AffectType = affectType;
      }
    }
  }
}

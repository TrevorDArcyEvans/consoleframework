using ConsoleFramework.Controls;

namespace ConsoleFramework.Rendering
{
  public partial class Renderer
  {
    private struct ControlAffectInfo
    {
      public readonly Control control;
      public readonly AffectType affectType;

      public ControlAffectInfo(Control control, AffectType affectType)
      {
        this.control = control;
        this.affectType = affectType;
      }
    }
  }
}

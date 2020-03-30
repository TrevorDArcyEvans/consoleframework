using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Controls;

namespace Examples.MainMenu
{
  public sealed class MainMenuWindowHost : WindowsHost
  {
    public IEnumerable<Window> Windows => Children.OfType<Window>().ToList().AsReadOnly();

    public void UpdateWindowsMenu()
    {
      var mnuWindows = MainMenu.Items
        .Cast<MenuItem>()
        .Single(x => x.Title == "_Windows");
      mnuWindows.Items.Clear();
      var windowSubMenus = Windows
        .Select(x =>
        {
          var mi = new MenuItem { Title = x.Title };
          mi.Click += (sender, eventArgs) => { ActivateWindow(x); };
          return mi;
        });
      foreach (var windowSubMenu in windowSubMenus)
      {
        mnuWindows.Items.Add(windowSubMenu);
      }
    }
  }
}

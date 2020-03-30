using ConsoleFramework;
using ConsoleFramework.Controls;
using ConsoleFramework.Core;

namespace Examples.MainMenu
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var windowsHost = (MainMenuWindowHost) ConsoleApplication.LoadFromXaml("Examples.MainMenu.windows-host.xml", null);
      var dataContext = new DataContext();
      var mainWindow = (Window) ConsoleApplication.LoadFromXaml("Examples.MainMenu.main.xml", dataContext);
      
      windowsHost.Show(mainWindow);
      var otherWindow = (Window) ConsoleApplication.LoadFromXaml("Examples.MainMenu.main.xml", dataContext);
      otherWindow.Title = "Other Window";
      windowsHost.Show(otherWindow);

      windowsHost.UpdateWindowsMenu();

      foreach (var window in windowsHost.Windows)
      {
        window.Closed += (sender, eventArgs) => { windowsHost.UpdateWindowsMenu(); };
      }

      // Example of direct subscribing to Click event
      var menuItems = VisualTreeHelper.FindAllChilds(windowsHost.MainMenu, control => control is MenuItem);
      foreach (var menuItem in menuItems)
      {
        var item = ((MenuItem) menuItem);
        if (item.Title == "Go")
        {
          item.Click += (sender, eventArgs) => { MessageBox.Show("Go", "Some text", result => { }); };
        }
      }

      ConsoleApplication.Instance.Run(windowsHost);
    }
  }
}

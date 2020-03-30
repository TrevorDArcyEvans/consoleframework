using System.ComponentModel;
using ConsoleFramework.Controls;
using ConsoleFramework.Events;

namespace Examples.MainMenu
{
  // Example of binding menu item to command
  public sealed class DataContext : INotifyPropertyChanged
  {
    public DataContext()
    {
      command = new RelayCommand(
        parameter => MessageBox.Show("Information", "Command executed !", result => { }),
        parameter => true);
    }

    private readonly RelayCommand command;

    public ICommand MyCommand => command;

    public event PropertyChangedEventHandler PropertyChanged;
  }
}

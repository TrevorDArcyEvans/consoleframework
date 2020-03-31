using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;

namespace ConsoleFramework.Controls
{
  public delegate void MessageBoxClosedEventHandler(MessageBoxResult result);

  public class MessageBox : Window
  {
    public MessageBox()
    {
      var panel = new Panel();
      _textBlock = new TextBlock
      {
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(1)
      };
      Button button = new Button
      {
        Margin = new Thickness(4, 0, 4, 0),
        HorizontalAlignment = HorizontalAlignment.Center,
        Caption = "OK"
      };
      button.OnClick += CloseButtonOnClicked;
      panel.Children.Add(_textBlock);
      panel.Children.Add(button);
      panel.HorizontalAlignment = HorizontalAlignment.Center;
      panel.VerticalAlignment = VerticalAlignment.Bottom;
      this.Content = panel;
    }

    protected virtual void CloseButtonOnClicked(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private readonly TextBlock _textBlock;

    public string Text
    {
      get { return _textBlock.Text; }
      set { _textBlock.Text = value; }
    }

    public static void Show(string title, string text, MessageBoxClosedEventHandler onClosed)
    {
      var rootControl = ConsoleApplication.Instance.RootControl;
      if (!(rootControl is WindowsHost))
      {
        throw new InvalidOperationException("Default windows host not found, create MessageBox manually");
      }

      var windowsHost = (WindowsHost) rootControl;
      var messageBox = new MessageBox();
      messageBox.Title = title;
      messageBox.Text = text;
      messageBox.AddHandler(ClosedEvent, new EventHandler((sender, args) =>
      {
        if (null != onClosed)
        {
          onClosed(MessageBoxResult.Button1);
        }
      }));
      windowsHost.ShowModal(messageBox);
    }
  }
}

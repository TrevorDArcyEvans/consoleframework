using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;

namespace ConsoleFramework.Controls
{
  public delegate void MessageBoxClosedEventHandler(MessageBoxResult result);

  public class MessageBox : Window
  {
    private readonly TextBlock _textBlock;

    public MessageBox()
    {
      Panel panel = new Panel();
      _textBlock = new TextBlock();
      _textBlock.HorizontalAlignment = HorizontalAlignment.Center;
      _textBlock.VerticalAlignment = VerticalAlignment.Center;
      _textBlock.Margin = new Thickness(1);
      Button button = new Button();
      button.Margin = new Thickness(4, 0, 4, 0);
      button.HorizontalAlignment = HorizontalAlignment.Center;
      button.Caption = "OK";
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

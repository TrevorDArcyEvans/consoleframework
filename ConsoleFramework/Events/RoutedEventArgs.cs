using System;

namespace ConsoleFramework.Events
{
  public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

  public class RoutedEventArgs : EventArgs
  {
    private bool _handled;

    public bool Handled
    {
      get { return _handled; }
      set { _handled = value; }
    }

    private readonly object _source;

    public object Source
    {
      get { return _source; }
    }

    private readonly RoutedEvent _routedEvent;

    public RoutedEvent RoutedEvent
    {
      get { return _routedEvent; }
    }

    public RoutedEventArgs(object source, RoutedEvent routedEvent)
    {
      this._source = source;
      this._routedEvent = routedEvent;
    }
  }
}

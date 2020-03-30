using System;
using ConsoleFramework.Events;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Event args containing info for ScrollViewer - how to display inner content.
  /// </summary>
  public class ContentShouldBeScrolledEventArgs : RoutedEventArgs
  {
    private readonly int? _mostLeftVisibleX;
    private readonly int? _mostRightVisibleX;
    private readonly int? _mostTopVisibleY;
    private readonly int? _mostBottomVisibleY;

    public ContentShouldBeScrolledEventArgs(object source, RoutedEvent routedEvent,
      int? mostLeftVisibleX, int? mostRightVisibleX,
      int? mostTopVisibleY, int? mostBottomVisibleY)
      : base(source, routedEvent)
    {
      if (mostLeftVisibleX.HasValue && mostRightVisibleX.HasValue)
      {throw new ArgumentException("Only one of X values can be specified");}
      if (mostTopVisibleY.HasValue && mostBottomVisibleY.HasValue)
      {throw new ArgumentException("Only one of Y values can be specified");}
      this._mostLeftVisibleX = mostLeftVisibleX;
      this._mostRightVisibleX = mostRightVisibleX;
      this._mostTopVisibleY = mostTopVisibleY;
      this._mostBottomVisibleY = mostBottomVisibleY;
    }

    public int? MostLeftVisibleX
    {
      get { return _mostLeftVisibleX; }
    }

    public int? MostRightVisibleX
    {
      get { return _mostRightVisibleX; }
    }

    public int? MostTopVisibleY
    {
      get { return _mostTopVisibleY; }
    }

    public int? MostBottomVisibleY
    {
      get { return _mostBottomVisibleY; }
    }
  }
}

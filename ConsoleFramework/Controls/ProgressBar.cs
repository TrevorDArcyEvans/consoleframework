using System;
using ConsoleFramework.Core;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  public class ProgressBar : Control
  {
    private int _percent;

    /// <summary>
    /// Percent (from 0 to 100).
    /// </summary>
    public int Percent
    {
      get { return _percent; }
      set
      {
        if (_percent != value)
        {
          _percent = value;
          RaisePropertyChanged("Percent");
        }
      }
    }

    public override void Render(RenderingBuffer buffer)
    {
      var attr = Colors.Blend(Color.DarkCyan, Color.DarkBlue);
      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, UnicodeTable.MediumShade, attr);
      var filled = (int) (ActualWidth * (Percent * 0.01));
      buffer.FillRectangle(0, 0, Math.Min(filled, ActualWidth), ActualHeight, UnicodeTable.DarkShade, attr);
    }
  }
}

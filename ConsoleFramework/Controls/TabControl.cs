﻿using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using Xaml;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Control that presents a tabbed layout.
  /// </summary>
  [ContentProperty("Controls")]
  public class TabControl : Control
  {
    public TabControl()
    {
      _controls = new UIElementCollection(this);
      AddHandler(MouseDownEvent, new MouseButtonEventHandler(mouseDown));
    }

    private void mouseDown(object sender, MouseButtonEventArgs args)
    {
      var pos = args.GetPosition(this);
      if (pos.y > 2)
      {
        return;
      }

      var x = 0;
      for (var i = 0; i < _tabDefinitions.Count; i++)
      {
        var tabDefinition = _tabDefinitions[i];
        if (pos.X > x && pos.X <= x + tabDefinition.Title.Length + 2)
        {
          _activeTabIndex = i;
          Invalidate();
          break;
        }

        x += tabDefinition.Title.Length + 2 + 1; // Two spaces around + one vertical border
      }

      args.Handled = true;
    }

    private readonly List<TabDefinition> _tabDefinitions = new List<TabDefinition>();

    public List<TabDefinition> TabDefinitions
    {
      get { return _tabDefinitions; }
    }

    private readonly UIElementCollection _controls;

    public UIElementCollection Controls
    {
      get { return _controls; }
    }

    private int _activeTabIndex;

    public int ActiveTabIndex
    {
      get { return _activeTabIndex; }
      set
      {
        if (value != _activeTabIndex)
        {
          if (value < 0 || value >= _tabDefinitions.Count)
          {
            throw new ArgumentException("Tab index out of bounds");
          }

          _activeTabIndex = value;
          Invalidate();
        }
      }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      // Get children max desired size to determine the tab content desired size
      var childrenAvailableSize = new Size(
        Math.Max(availableSize.Width - 2, 0),
        Math.Max(availableSize.Height - 4, 0));
      var maxDesiredWidth = 0;
      var maxDesiredHeight = 0;
      for (var i = 0; i < Math.Min(Children.Count, _tabDefinitions.Count); i++)
      {
        var child = Children[i];
        child.Measure(childrenAvailableSize);
        if (child.DesiredSize.Width > maxDesiredWidth)
        {
          maxDesiredWidth = child.DesiredSize.Width;
        }

        if (child.DesiredSize.Height > maxDesiredHeight)
        {
          maxDesiredHeight = child.DesiredSize.Height;
        }
      }

      // Get tab header desired size
      var tabHeaderWidth = GetTabHeaderWidth();

      // Calculate final size = min(availableSize, controlWithChildrenDesiredSize)
      var controlWithChildrenDesiredSize = new Size(
        Math.Max(maxDesiredWidth + 2, tabHeaderWidth),
        maxDesiredHeight + 4
      );
      var finalAvailableSize = new Size(
        Math.Min(availableSize.Width, controlWithChildrenDesiredSize.Width),
        Math.Min(availableSize.Height, controlWithChildrenDesiredSize.Height)
      );

      // Invoke children.Measure() with final size
      for (var i = 0; i < Children.Count; i++)
      {
        var child = Children[i];
        if (_activeTabIndex == i)
        {
          child.Measure(new Size(
            Math.Max(0, finalAvailableSize.Width - 2),
            Math.Max(0, finalAvailableSize.Height - 4)
          ));
        }
        else
        {
          child.Measure(Size.Empty);
        }
      }

      return finalAvailableSize;
    }

    private int GetTabHeaderWidth()
    {
      // Two spaces around + one vertical border per tab, plus extra one vertical border
      return 1 + _tabDefinitions.Sum(tabDefinition => tabDefinition.Title.Length + 2 + 1);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      for (var i = 0; i < Children.Count; i++)
      {
        var child = Children[i];
        if (_activeTabIndex == i)
        {
          child.Arrange(new Rect(
            new Point(1, 3),
            new Size(Math.Max(0, finalSize.Width - 2),
              Math.Max(0, finalSize.Height - 4)
            )));
        }
        else
        {
          child.Arrange(Rect.Empty);
        }
      }

      return finalSize;
    }

    private void renderBorderSafe(RenderingBuffer buffer, int x, int y, int x2, int y2)
    {
      if (ActualWidth > x && ActualHeight > y)
      {
        buffer.SetPixel(x, y, UnicodeTable.SingleFrameTopLeftCorner);
      }

      if (ActualWidth > x && ActualHeight > y2 && y2 > y)
      {
        buffer.SetPixel(x, y2, UnicodeTable.SingleFrameBottomLeftCorner);
      }

      if (ActualHeight > y)
        for (var i = x + 1; i <= Math.Min(x2 - 1, ActualWidth - 1); i++)
        {
          buffer.SetPixel(i, y, UnicodeTable.SingleFrameHorizontal);
        }

      if (ActualHeight > y2)
      {
        for (var i = x + 1; i <= Math.Min(x2 - 1, ActualWidth - 1); i++)
        {
          buffer.SetPixel(i, y2, UnicodeTable.SingleFrameHorizontal);
        }
      }

      if (ActualWidth > x)
      {
        for (var j = y + 1; j <= Math.Min(y2 - 1, ActualHeight - 1); j++)
        {
          buffer.SetPixel(x, j, UnicodeTable.SingleFrameVertical);
        }
      }

      if (ActualWidth > x2)
      {
        for (var j = y + 1; j <= Math.Min(y2 - 1, ActualHeight - 1); j++)
        {
          buffer.SetPixel(x2, j, UnicodeTable.SingleFrameVertical);
        }
      }

      if (ActualWidth > x2 && ActualHeight > y && x2 > x)
      {
        buffer.SetPixel(x2, y, UnicodeTable.SingleFrameTopRightCorner);
      }

      if (ActualWidth > x2 && ActualHeight > y2 && y2 > y && x2 > x)
      {
        buffer.SetPixel(x2, y2, UnicodeTable.SingleFrameBottomRightCorner);
      }
    }

    public override void Render(RenderingBuffer buffer)
    {
      var attr = Colors.Blend(Color.Black, Color.DarkGreen);
      var inactiveAttr = Colors.Blend(Color.DarkGray, Color.DarkGreen);

      // Transparent background for borders
      buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);

      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);

      // Transparent child content part
      if (ActualWidth > 2 && ActualHeight > 3)
      {
        buffer.SetOpacityRect(1, 3, ActualWidth - 2, ActualHeight - 4, 2);
      }

      renderBorderSafe(buffer, 0, 2, Math.Max(GetTabHeaderWidth() - 1, ActualWidth - 1), ActualHeight - 1);

      // Start to render header
      buffer.FillRectangle(0, 0, ActualWidth, Math.Min(2, ActualHeight), ' ', attr);

      var x = 0;

      // Render tabs before active tab
      for (var tab = 0; tab < _tabDefinitions.Count; x += TabDefinitions[tab++].Title.Length + 3)
      {
        var tabDefinition = TabDefinitions[tab];
        if (tab <= _activeTabIndex)
        {
          buffer.SetPixelSafe(x, 0, UnicodeTable.SingleFrameTopLeftCorner);
          buffer.SetPixelSafe(x, 1, UnicodeTable.SingleFrameVertical);
        }

        if (tab == _activeTabIndex)
        {
          buffer.SetPixelSafe(x, 2,
            _activeTabIndex == 0 ? UnicodeTable.SingleFrameVertical : UnicodeTable.SingleFrameBottomRightCorner);
        }

        for (var i = 0; i < tabDefinition.Title.Length + 2; i++)
        {
          buffer.SetPixelSafe(x + 1 + i, 0, UnicodeTable.SingleFrameHorizontal);
          if (tab == _activeTabIndex)
          {
            buffer.SetPixelSafe(x + 1 + i, 2, ' ');
          }
        }

        buffer.RenderStringSafe(" " + tabDefinition.Title + " ", x + 1, 1, _activeTabIndex == tab ? attr : inactiveAttr);
        if (tab >= _activeTabIndex)
        {
          buffer.SetPixelSafe(x + tabDefinition.Title.Length + 3, 0, UnicodeTable.SingleFrameTopRightCorner);
          buffer.SetPixelSafe(x + tabDefinition.Title.Length + 3, 1, UnicodeTable.SingleFrameVertical);
        }

        if (tab == _activeTabIndex)
        {
          buffer.SetPixelSafe(x + tabDefinition.Title.Length + 3, 2,
            _activeTabIndex == _tabDefinitions.Count - 1 && ActualWidth - 1 == x + tabDefinition.Title.Length + 3
              ? UnicodeTable.SingleFrameVertical
              : UnicodeTable.SingleFrameBottomLeftCorner);
        }
      }
    }
  }
}

using System;
using System.Collections.Generic;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  public class PopupWindow : Window
  {
    public int? IndexSelected;
    private readonly bool _shadow;
    private readonly ListBox _listbox;
    private readonly ScrollViewer _scrollViewer;

    public PopupWindow(
      IEnumerable<string> items,
      int selectedItemIndex, bool shadow,
      int? shownItemsCount)
    {
      this._shadow = shadow;
      _scrollViewer = new ScrollViewer();
      _listbox = new ListBox();
      foreach (string item in items)
      {
        _listbox.Items.Add(item);
      }

      _listbox.SelectedItemIndex = selectedItemIndex;
      if (shownItemsCount != null)
      {
        _listbox.PageSize = shownItemsCount.Value;
      }

      IndexSelected = selectedItemIndex;
      _listbox.HorizontalAlignment = HorizontalAlignment.Stretch;
      _scrollViewer.HorizontalScrollEnabled = false;
      _scrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
      _scrollViewer.Content = _listbox;
      Content = _scrollViewer;

      // If click on the transparent header, close the popup
      AddHandler(MouseDownEvent, new MouseButtonEventHandler((sender, args) =>
      {
        if (!_scrollViewer.RenderSlotRect.Contains(args.GetPosition(this)))
        {
          Close();
          args.Handled = true;
        }
      }));

      // If _listbox item has been selected
      EventManager.AddHandler(_listbox, MouseUpEvent, new MouseButtonEventHandler(
        (sender, args) =>
        {
          IndexSelected = _listbox.SelectedItemIndex;
          Close();
        }), true);
      EventManager.AddHandler(_listbox, KeyDownEvent, new KeyEventHandler(
        (sender, args) =>
        {
          if (args.VirtualKeyCode == VirtualKeys.Return)
          {
            IndexSelected = _listbox.SelectedItemIndex;
            Close();
          }
        }), true);
      // todo : cleanup event handlers after popup closing
    }

    private void InitListBoxScrollingPos()
    {
      var itemIndex = _listbox.SelectedItemIndex ?? 0;
      var firstVisibleItemIndex = _scrollViewer.DeltaY;
      var lastVisibleItemIndex = firstVisibleItemIndex + _scrollViewer.ActualHeight -
                                 (_scrollViewer.HorizontalScrollVisible ? 1 : 0) - 1;
      if (itemIndex > lastVisibleItemIndex)
      {
        _scrollViewer.ScrollContent(ScrollViewer.Direction.Up, itemIndex - lastVisibleItemIndex);
      }
      else if (itemIndex < firstVisibleItemIndex)
      {
        _scrollViewer.ScrollContent(ScrollViewer.Direction.Down, firstVisibleItemIndex - itemIndex);
      }
    }

    protected override void Initialize()
    {
      AddHandler(ActivatedEvent, new EventHandler(OnActivated));
      AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
      if (args.VirtualKeyCode == VirtualKeys.Escape)
      {
        Close();
      }
      else base.OnPreviewKeyDown(sender, args);
    }

    private void OnActivated(object sender, EventArgs eventArgs)
    {
    }

    public override void Render(RenderingBuffer buffer)
    {
      Attr borderAttrs = Colors.Blend(Color.Black, Color.DarkCyan);

      // Background
      buffer.FillRectangle(1, 1, this.ActualWidth - 1, this.ActualHeight - 1, ' ', borderAttrs);

      // First row and first column are transparent
      // Column is also transparent for mouse events
      buffer.SetOpacityRect(0, 0, ActualWidth, 1, 2);
      buffer.SetOpacityRect(0, 1, 1, ActualHeight - 1, 6);
      if (_shadow)
      {
        buffer.SetOpacity(1, ActualHeight - 1, 2 + 4);
        buffer.SetOpacity(ActualWidth - 1, 0, 2 + 4);
        buffer.SetOpacityRect(ActualWidth - 1, 1, 1, ActualHeight - 1, 1 + 4);
        buffer.FillRectangle(ActualWidth - 1, 1, 1, ActualHeight - 1, UnicodeTable.FullBlock, borderAttrs);
        buffer.SetOpacityRect(2, ActualHeight - 1, ActualWidth - 2, 1, 3 + 4);
        buffer.FillRectangle(2, ActualHeight - 1, ActualWidth - 2, 1, UnicodeTable.UpperHalfBlock, Attr.NO_ATTRIBUTES);
      }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      if (Content == null)
      {
        return new Size(0, 0);
      }
      if (_shadow)
      {
        // 1 row and 1 column - reserved for transparent space, remaining - for ListBox
        Content.Measure(new Size(availableSize.Width - 2, availableSize.Height - 2));
        return new Size(Content.DesiredSize.Width + 2, Content.DesiredSize.Height + 2);
      }
      else
      {
        // 1 row and 1 column - reserved for transparent space, remaining - for ListBox
        Content.Measure(new Size(availableSize.Width - 1, availableSize.Height - 1));
        return new Size(Content.DesiredSize.Width + 1, Content.DesiredSize.Height + 1);
      }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      if (Content != null)
      {
        if (_shadow)
        {
          Content.Arrange(new Rect(new Point(1, 1),
            new Size(finalSize.Width - 2, finalSize.Height - 2)));
        }
        else
        {
          Content.Arrange(new Rect(new Point(1, 1),
            new Size(finalSize.Width - 1, finalSize.Height - 1)));
        }

        // When initializing we need to correctly assign offsets to ScrollViewer for
        // currently selected item. Because ScrollViewer depends of ActualWidth / ActualHeight
        // of Content, we need to do this after arrangement has finished.
        InitListBoxScrollingPos();
      }

      return finalSize;
    }

    public override string ToString()
    {
      return "PopupWindow";
    }
  }
}

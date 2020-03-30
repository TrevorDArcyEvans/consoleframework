using System;
using System.Collections.Generic;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  internal class Popup : Window
  {
    private readonly bool _shadow;
    private readonly int _parentItemWidth; // Размер непрозрачной для нажатий мыши области в 1ой строке окна
    private readonly Panel _panel;

    public static readonly RoutedEvent ControlKeyPressedEvent = EventManager.RegisterRoutedEvent("ControlKeyPressed",
      RoutingStrategy.Bubble, typeof(KeyEventHandler), typeof(Popup));

    /// <summary>
    /// Call this method to remove all menu items that are used as child items.
    /// It is necessary before reuse MenuItems in another Popup instance.
    /// </summary>
    public void DisconnectMenuItems()
    {
      _panel.Children.Clear();
    }

    /// <summary>
    /// Первая строчка всплывающего окна - особенная. Она прозрачна с точки зрения
    /// рендеринга полностью. Однако Opacity для событий мыши в ней разные.
    /// Первые width пикселей в ней - непрозрачные для событий мыши, но при клике на них
    /// окно закрывается вызовом Close(). Остальные ActualWidth - width пикселей - прозрачные
    /// для событий мыши, и нажатие мыши в этой области приводит к тому, что окно
    /// WindowsHost закрывает окно как окно с OutsideClickClosesWindow = True.
    /// </summary>
    public Popup(IEnumerable<MenuItemBase> menuItems, bool shadow, int parentItemWidth)
    {
      this._parentItemWidth = parentItemWidth;
      this._shadow = shadow;
      _panel = new Panel();
      _panel.Orientation = Orientation.Vertical;
      foreach (var item in menuItems)
      {
        _panel.Children.Add(item);
      }

      Content = _panel;

      // If click on the transparent header, close the popup
      AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler((sender, args) =>
      {
        if (Content != null && !Content.RenderSlotRect.Contains(args.GetPosition(this)))
        {
          Close();
          if (new Rect(new Size(parentItemWidth, 1)).Contains(args.GetPosition(this)))
          {
            args.Handled = true;
          }
        }
      }));

      EventManager.AddHandler(_panel, PreviewMouseMoveEvent, new MouseEventHandler(OnPanelMouseMove));
    }

    protected override void OnPreviewKeyDown(object sender, KeyEventArgs args)
    {
      switch (args.wVirtualKeyCode)
      {
        case VirtualKeys.Right:
        {
          KeyEventArgs newArgs = new KeyEventArgs(this, ControlKeyPressedEvent);
          newArgs.wVirtualKeyCode = args.wVirtualKeyCode;
          RaiseEvent(ControlKeyPressedEvent, newArgs);
          args.Handled = true;
          break;
        }

        case VirtualKeys.Left:
        {
          KeyEventArgs newArgs = new KeyEventArgs(this, ControlKeyPressedEvent);
          newArgs.wVirtualKeyCode = args.wVirtualKeyCode;
          RaiseEvent(ControlKeyPressedEvent, newArgs);
          args.Handled = true;
          break;
        }

        case VirtualKeys.Down:
          ConsoleApplication.Instance.FocusManager.MoveFocusNext();
          args.Handled = true;
          break;

        case VirtualKeys.Up:
          ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
          args.Handled = true;
          break;

        case VirtualKeys.Escape:
          Close();
          args.Handled = true;
          break;
      }
    }

    private void OnPanelMouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        PassFocusToChildUnderPoint(e);
      }
    }

    protected override void initialize()
    {
      AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
    }

    public override void Render(RenderingBuffer buffer)
    {
      var borderAttrs = Colors.Blend(Color.Black, Color.Gray);

      // Background
      buffer.FillRectangle(0, 1, ActualWidth, ActualHeight - 1, ' ', borderAttrs);

      // Первые width пикселей первой строки - прозрачные, но события мыши не пропускают
      // По нажатию на них мы закрываем всплывающее окно вручную
      buffer.SetOpacityRect(0, 0, Math.Min(ActualWidth, _parentItemWidth), 1, 2);
      // Оставшиеся пиксели первой строки - пропускают события мыши
      // И WindowsHost закроет всплывающее окно автоматически при нажатии или
      // перемещении нажатого курсора над этим местом
      if (ActualWidth > _parentItemWidth)
        buffer.SetOpacityRect(_parentItemWidth, 0, ActualWidth - _parentItemWidth, 1, 6);

      if (_shadow)
      {
        buffer.SetOpacity(0, ActualHeight - 1, 2 + 4);
        buffer.SetOpacity(ActualWidth - 1, 1, 2 + 4);
        buffer.SetOpacityRect(ActualWidth - 1, 2, 1, ActualHeight - 2, 1 + 4);
        buffer.FillRectangle(ActualWidth - 1, 2, 1, ActualHeight - 2, UnicodeTable.FullBlock, borderAttrs);
        buffer.SetOpacityRect(1, ActualHeight - 1, ActualWidth - 1, 1, 3 + 4);
        buffer.FillRectangle(1, ActualHeight - 1, ActualWidth - 1, 1, UnicodeTable.UpperHalfBlock, Attr.NO_ATTRIBUTES);
      }

      RenderBorders(buffer, new Point(1, 1),
        _shadow
          ? new Point(ActualWidth - 3, ActualHeight - 2)
          : new Point(ActualWidth - 2, ActualHeight - 1),
        true, borderAttrs);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      if (Content == null)
      {
        return new Size(0, 0);
      }

      if (_shadow)
      {
        // 1 строку и 1 столбец оставляем для прозрачного пространства, остальное занимает Content
        Content.Measure(new Size(availableSize.Width - 3, availableSize.Height - 4));
        // +2 for left empty space and right
        return new Size(Content.DesiredSize.Width + 3 + 2, Content.DesiredSize.Height + 4);
      }
      else
      {
        // 1 строку и 1 столбец оставляем для прозрачного пространства, остальное занимает Content
        Content.Measure(new Size(availableSize.Width - 2, availableSize.Height - 3));
        // +2 for left empty space and right
        return new Size(Content.DesiredSize.Width + 2 + 2, Content.DesiredSize.Height + 3);
      }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      if (Content != null)
      {
        if (_shadow)
        {
          // 1 pixel from all borders - for popup padding
          // 1 pixel from top - for transparent region
          // Additional pixel from right and bottom - for _shadow
          Content.Arrange(new Rect(new Point(2, 2), new Size(finalSize.Width - 5, finalSize.Height - 4)));
        }
        else
        {
          // 1 pixel from all borders - for popup padding
          // 1 pixel from top - for transparent region
          Content.Arrange(new Rect(new Point(2, 2), new Size(finalSize.Width - 4, finalSize.Height - 3)));
        }
      }

      return finalSize;
    }
  }
}

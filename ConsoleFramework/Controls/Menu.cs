using System;
using System.Collections.Generic;
using System.Linq;
using Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  public class Menu : Control
  {
    private readonly ObservableList<MenuItemBase> _items = new ObservableList<MenuItemBase>(new List<MenuItemBase>());

    public IList<MenuItemBase> Items
    {
      get { return _items; }
    }

    private void GetGestures(MenuItem item, Dictionary<KeyGesture, MenuItem> map)
    {
      if (item.Gesture != null)
      {
        map.Add(item.Gesture, item);
      }

      if (item.Type == MenuItemType.RootSubmenu ||
          item.Type == MenuItemType.Submenu)
      {
        foreach (var itemBase in item.Items)
        {
          if (itemBase is MenuItem)
          {
            GetGestures((MenuItem) itemBase, map);
          }
        }
      }
    }

    public void RefreshKeyGestures()
    {
      _gestures = null;
    }

    private Dictionary<KeyGesture, MenuItem> _gestures;

    private Dictionary<KeyGesture, MenuItem> GetGesturesMap()
    {
      if (_gestures == null)
      {
        _gestures = new Dictionary<KeyGesture, MenuItem>();
        foreach (var itemBase in this.Items)
        {
          if (itemBase is MenuItem)
          {
            GetGestures((MenuItem) itemBase, _gestures);
          }
        }
      }

      return _gestures;
    }

    public bool TryMatchGesture(KeyEventArgs args)
    {
      Dictionary<KeyGesture, MenuItem> map = GetGesturesMap();
      KeyGesture match = map.Keys.FirstOrDefault(gesture => gesture.Matches(args));
      if (match == null) return false;

      this.CloseAllSubmenus();

      // Activate matches menu item
      var menuItem = map[match];
      var path = new List<MenuItem>();
      var currentItem = menuItem;
      while (currentItem != null)
      {
        path.Add(currentItem);
        currentItem = currentItem.ParentItem;
      }

      path.Reverse();

      // Open all menu _items in path successively
      var i = 0;
      Action action = null;
      action = () =>
      {
        if (i < path.Count)
        {
          var item = path[i];
          if (item.Type == MenuItemType.Item)
          {
            item.RaiseClick();
            return;
          }

          // Activate focus on item
          if (item.ParentItem == null)
          {
            ConsoleApplication.Instance.FocusManager.SetFocus(this, item);
          }
          else
          {
            // Set focus to PopupWindow -> item
            ConsoleApplication.Instance.FocusManager.SetFocus(item.Parent.Parent, item);
          }

          item.Invalidate();
          EventHandler handler = null;

          // Wait for layout to be revalidated and expand it
          handler = (o, eventArgs) =>
          {
            item.Expand();
            item.LayoutRevalidated -= handler;
            i++;
            if (i < path.Count)
            {
              action();
            }
          };
          item.LayoutRevalidated += handler;
        }
      };
      action();

      return true;
    }

    /// <summary>
    /// Forces all open submenus to be closed.
    /// </summary>
    public void CloseAllSubmenus()
    {
      var expandedSubmenus = new List<MenuItem>();
      var currentItem = (MenuItem) this.Items.SingleOrDefault(item => item is MenuItem && ((MenuItem) item).expanded);
      while (null != currentItem)
      {
        expandedSubmenus.Add(currentItem);
        currentItem = (MenuItem) currentItem.Items.SingleOrDefault(item => item is MenuItem && ((MenuItem) item).expanded);
      }

      expandedSubmenus.Reverse();
      foreach (MenuItem expandedSubmenu in expandedSubmenus)
      {
        expandedSubmenu.Close();
      }
    }

    public Menu()
    {
      var stackPanel = new Panel();
      stackPanel.Orientation = Orientation.Horizontal;
      this.AddChild(stackPanel);

      // Subscribe to Items change and add to Children them
      this._items.ListChanged += (sender, args) =>
      {
        switch (args.Type)
        {
          case ListChangedEventType.ItemsInserted:
          {
            for (var i = 0; i < args.Count; i++)
            {
              var item = _items[args.Index + i];
              if (item is Separator)
              {
                throw new InvalidOperationException("Separator cannot be added to root menu.");
              }

              if (((MenuItem) item).Type == MenuItemType.Submenu)
              {
                ((MenuItem) item).Type = MenuItemType.RootSubmenu;
              }

              stackPanel.Children.Insert(args.Index + i, item);
            }

            break;
          }

          case ListChangedEventType.ItemsRemoved:
            for (var i = 0; i < args.Count; i++)
            {
              stackPanel.Children.RemoveAt(args.Index);
            }

            break;

          case ListChangedEventType.ItemReplaced:
          {
            var item = _items[args.Index];
            if (item is Separator)
            {
              throw new InvalidOperationException("Separator cannot be added to root menu.");
            }

            if (((MenuItem) item).Type == MenuItemType.Submenu)
            {
              ((MenuItem) item).Type = MenuItemType.RootSubmenu;
            }

            stackPanel.Children[args.Index] = item;
            break;
          }
        }
      };
      this.IsFocusScope = true;

      this.AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
      this.AddHandler(PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove));
      this.AddHandler(PreviewMouseDownEvent, new MouseEventHandler(OnPreviewMouseDown));
    }

    protected override void OnParentChanged()
    {
      if (Parent != null)
      {
        assert(Parent is WindowsHost);

        // Вешаем на WindowsHost обработчик события MenuItem.ClickEvent,
        // чтобы ловить момент выбора пункта меню в одном из модальных всплывающих окошек
        // Дело в том, что эти окошки не являются дочерними элементами контрола Menu,
        // а напрямую являются дочерними элементами WindowsHost (т.к. именно он создаёт
        // окна). И событие выбора пункта меню из всплывающего окошка может быть поймано 
        // в WindowsHost, но не в Menu. А нам нужно повесить обработчик, который закроет
        // все показанные попапы.
        EventManager.AddHandler(Parent, MenuItem.ClickEvent, new RoutedEventHandler((sender, args) => CloseAllSubmenus()), true);

        EventManager.AddHandler(Parent, Popup.ControlKeyPressedEvent,
          new KeyEventHandler((sender, args) =>
          {
            CloseAllSubmenus();

            ConsoleApplication.Instance.FocusManager.SetFocusScope(this);
            if (args.VirtualKeyCode == VirtualKeys.Right)
            {
              ConsoleApplication.Instance.FocusManager.MoveFocusNext();
            }
            else if (args.VirtualKeyCode == VirtualKeys.Left)
            {
              ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
            }

            var focusedItem = (MenuItem) this.Items.SingleOrDefault(item => item is MenuItem && item.HasFocus);
            focusedItem.Expand();
          }));
      }
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs args)
    {
      if (args.LeftButton == MouseButtonState.Pressed)
      {
        OnPreviewMouseDown(sender, args);
      }
    }

    private void OnPreviewMouseDown(object sender, MouseEventArgs e)
    {
      PassFocusToChildUnderPoint(e);
    }

    private static void OnKeyDown(object sender, KeyEventArgs args)
    {
      if (args.VirtualKeyCode == VirtualKeys.Right)
      {
        ConsoleApplication.Instance.FocusManager.MoveFocusNext();
        args.Handled = true;
      }

      if (args.VirtualKeyCode == VirtualKeys.Left)
      {
        ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
        args.Handled = true;
      }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      this.Children[0].Measure(availableSize);
      return this.Children[0].DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      this.Children[0].Arrange(new Rect(new Point(0, 0), finalSize));
      return finalSize;
    }

    public override void Render(RenderingBuffer buffer)
    {
      var attr = Colors.Blend(Color.Black, Color.Gray);
      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);
    }
  }
}

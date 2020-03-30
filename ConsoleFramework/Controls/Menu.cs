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
    private readonly ObservableList<MenuItemBase> items = new ObservableList<MenuItemBase>(
      new List<MenuItemBase>());

    public IList<MenuItemBase> Items
    {
      get { return items; }
    }

    private void getGestures(MenuItem item, Dictionary<KeyGesture, MenuItem> map)
    {
      if (item.Gesture != null)
        map.Add(item.Gesture, item);
      if (item.Type == MenuItemType.RootSubmenu ||
          item.Type == MenuItemType.Submenu)
      {
        foreach (MenuItemBase itemBase in item.Items)
        {
          if (itemBase is MenuItem)
          {
            getGestures((MenuItem) itemBase, map);
          }
        }
      }
    }

    public void RefreshKeyGestures()
    {
      gestures = null;
    }

    private Dictionary<KeyGesture, MenuItem> gestures;

    private Dictionary<KeyGesture, MenuItem> getGesturesMap()
    {
      if (gestures == null)
      {
        gestures = new Dictionary<KeyGesture, MenuItem>();
        foreach (MenuItemBase itemBase in this.Items)
        {
          if (itemBase is MenuItem)
          {
            getGestures((MenuItem) itemBase, gestures);
          }
        }
      }

      return gestures;
    }

    public bool TryMatchGesture(KeyEventArgs args)
    {
      Dictionary<KeyGesture, MenuItem> map = getGesturesMap();
      KeyGesture match = map.Keys.FirstOrDefault(gesture => gesture.Matches(args));
      if (match == null) return false;

      this.CloseAllSubmenus();

      // Activate matches menu item
      MenuItem menuItem = map[match];
      List<MenuItem> path = new List<MenuItem>();
      MenuItem currentItem = menuItem;
      while (currentItem != null)
      {
        path.Add(currentItem);
        currentItem = currentItem.ParentItem;
      }

      path.Reverse();

      // Open all menu _items in path successively
      int i = 0;
      Action action = null;
      action = new Action(() =>
      {
        if (i < path.Count)
        {
          MenuItem item = path[i];
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
            ConsoleApplication.Instance.FocusManager.SetFocus(
              item.Parent.Parent, item);
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
      });
      action();

      return true;
    }

    /// <summary>
    /// Forces all open submenus to be closed.
    /// </summary>
    public void CloseAllSubmenus()
    {
      List<MenuItem> expandedSubmenus = new List<MenuItem>();
      MenuItem currentItem = (MenuItem) this.Items.SingleOrDefault(
        item => item is MenuItem && ((MenuItem) item).expanded);
      while (null != currentItem)
      {
        expandedSubmenus.Add(currentItem);
        currentItem = (MenuItem) currentItem.Items.SingleOrDefault(
          item => item is MenuItem && ((MenuItem) item).expanded);
      }

      expandedSubmenus.Reverse();
      foreach (MenuItem expandedSubmenu in expandedSubmenus)
      {
        expandedSubmenu.Close();
      }
    }

    public Menu()
    {
      Panel stackPanel = new Panel();
      stackPanel.Orientation = Orientation.Horizontal;
      this.AddChild(stackPanel);

      // Subscribe to Items change and add to Children them
      this.items.ListChanged += (sender, args) =>
      {
        switch (args.Type)
        {
          case ListChangedEventType.ItemsInserted:
          {
            for (int i = 0; i < args.Count; i++)
            {
              MenuItemBase item = items[args.Index + i];
              if (item is Separator)
                throw new InvalidOperationException("Separator cannot be added to root menu.");
              if (((MenuItem) item).Type == MenuItemType.Submenu)
                ((MenuItem) item).Type = MenuItemType.RootSubmenu;
              stackPanel.Children.Insert(args.Index + i, item);
            }

            break;
          }
          case ListChangedEventType.ItemsRemoved:
            for (int i = 0; i < args.Count; i++)
              stackPanel.Children.RemoveAt(args.Index);
            break;
          case ListChangedEventType.ItemReplaced:
          {
            MenuItemBase item = items[args.Index];
            if (item is Separator)
              throw new InvalidOperationException("Separator cannot be added to root menu.");
            if (((MenuItem) item).Type == MenuItemType.Submenu)
              ((MenuItem) item).Type = MenuItemType.RootSubmenu;
            stackPanel.Children[args.Index] = item;
            break;
          }
        }
      };
      this.IsFocusScope = true;

      this.AddHandler(KeyDownEvent, new KeyEventHandler(onKeyDown));
      this.AddHandler(PreviewMouseMoveEvent, new MouseEventHandler(onPreviewMouseMove));
      this.AddHandler(PreviewMouseDownEvent, new MouseEventHandler(onPreviewMouseDown));
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
        EventManager.AddHandler(Parent, MenuItem.ClickEvent,
          new RoutedEventHandler((sender, args) => CloseAllSubmenus()), true);

        EventManager.AddHandler(Parent, Popup.ControlKeyPressedEvent,
          new KeyEventHandler((sender, args) =>
          {
            CloseAllSubmenus();
            //
            ConsoleApplication.Instance.FocusManager.SetFocusScope(this);
            if (args.wVirtualKeyCode == VirtualKeys.Right)
              ConsoleApplication.Instance.FocusManager.MoveFocusNext();
            else if (args.wVirtualKeyCode == VirtualKeys.Left)
              ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
            MenuItem focusedItem = (MenuItem) this.Items.SingleOrDefault(
              item => item is MenuItem && item.HasFocus);
            focusedItem.Expand();
          }));
      }
    }

    private void onPreviewMouseMove(object sender, MouseEventArgs args)
    {
      if (args.LeftButton == MouseButtonState.Pressed)
      {
        onPreviewMouseDown(sender, args);
      }
    }

    private void onPreviewMouseDown(object sender, MouseEventArgs e)
    {
      PassFocusToChildUnderPoint(e);
    }

    private void onKeyDown(object sender, KeyEventArgs args)
    {
      if (args.wVirtualKeyCode == VirtualKeys.Right)
      {
        ConsoleApplication.Instance.FocusManager.MoveFocusNext();
        args.Handled = true;
      }

      if (args.wVirtualKeyCode == VirtualKeys.Left)
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
      Attr attr = Colors.Blend(Color.Black, Color.Gray);
      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);
    }
  }
}

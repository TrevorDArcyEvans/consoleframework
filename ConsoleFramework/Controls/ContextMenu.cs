using System;
using System.Collections.Generic;
using System.Linq;
using Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using Xaml;

namespace ConsoleFramework.Controls
{
  [ContentProperty("Items")]
  public class ContextMenu
  {
    private readonly ObservableList<MenuItemBase> _items = new ObservableList<MenuItemBase>(new List<MenuItemBase>());

    public IList<MenuItemBase> Items
    {
      get { return _items; }
    }

    private Popup _popup;
    private bool _expanded;

    private bool _popupShadow = true;

    public bool PopupShadow
    {
      get { return _popupShadow; }
      set { _popupShadow = value; }
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
      foreach (var expandedSubmenu in expandedSubmenus)
      {
        expandedSubmenu.Close();
      }
    }

    private WindowsHost _windowsHost;
    private RoutedEventHandler _windowsHostClick;
    private KeyEventHandler _windowsHostControlKeyPressed;

    public void OpenMenu(WindowsHost windowsHost, Point point)
    {
      if (_expanded)
      {
        return;
      }

      // Вешаем на WindowsHost обработчик события MenuItem.ClickEvent,
      // чтобы ловить момент выбора пункта меню в одном из модальных всплывающих окошек
      // Дело в том, что эти окошки не являются дочерними элементами контрола Menu,
      // а напрямую являются дочерними элементами WindowsHost (т.к. именно он создаёт
      // окна). И событие выбора пункта меню из всплывающего окошка может быть поймано 
      // в WindowsHost, но не в Menu. А нам нужно повесить обработчик, который закроет
      // все показанные попапы.
      EventManager.AddHandler(windowsHost, MenuItem.ClickEvent,
        _windowsHostClick = (sender, args) =>
        {
          CloseAllSubmenus();
          _popup.Close();
        }, true);

      EventManager.AddHandler(windowsHost, Popup.ControlKeyPressedEvent,
        _windowsHostControlKeyPressed = (sender, args) =>
        {
          CloseAllSubmenus();

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
        });

      if (null == _popup)
      {
        _popup = new Popup(this.Items, this._popupShadow, 0);
        _popup.AddHandler(Window.ClosedEvent, new EventHandler(OnPopupClosed));
      }

      _popup.X = point.X;
      _popup.Y = point.Y;
      windowsHost.ShowModal(_popup, true);
      _expanded = true;
      this._windowsHost = windowsHost;
    }

    private void OnPopupClosed(object sender, EventArgs eventArgs)
    {
      if (!_expanded)
      {
        throw new InvalidOperationException("This shouldn't happen");
      }

      _expanded = false;
      EventManager.RemoveHandler(_windowsHost, MenuItem.ClickEvent, _windowsHostClick);
      EventManager.RemoveHandler(_windowsHost, Popup.ControlKeyPressedEvent, _windowsHostControlKeyPressed);
    }
  }
}

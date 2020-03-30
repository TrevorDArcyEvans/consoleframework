using System;
using System.Collections.Generic;
using Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using Xaml;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Item of menu.
  /// </summary>
  [ContentProperty("Items")]
  public class MenuItem : MenuItemBase, ICommandSource
  {
    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click",
      RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

    public MenuItem ParentItem { get; internal set; }

    /// <summary>
    /// Call this method if you have changed menu items set
    /// after menu popup has been shown.
    /// </summary>
    public void ReinitializePopup()
    {
      if (null != popup)
      {
        popup.DisconnectMenuItems();
      }
    }

    public event RoutedEventHandler Click
    {
      add { AddHandler(ClickEvent, value); }
      remove { RemoveHandler(ClickEvent, value); }
    }

    private bool _expanded;

    internal bool expanded
    {
      get { return _expanded; }
      private set
      {
        if (_expanded != value)
        {
          _expanded = value;
          Invalidate();
        }
      }
    }

    private bool _disabled;

    public bool Disabled
    {
      get { return _disabled; }
      set
      {
        if (_disabled != value)
        {
          _disabled = value;
          Focusable = !_disabled;
          Invalidate();
        }
      }
    }

    private KeyGesture _gesture;

    public KeyGesture Gesture
    {
      get { return _gesture; }
      set { _gesture = value; }
    }

    private bool _popupShadow = true;

    public bool PopupShadow
    {
      get { return _popupShadow; }
      set { _popupShadow = value; }
    }

    public MenuItem()
    {
      Focusable = true;

      AddHandler(MouseDownEvent, new MouseEventHandler(OnMouseDown));
      AddHandler(MouseMoveEvent, new MouseEventHandler(OnMouseMove));
      AddHandler(MouseUpEvent, new MouseEventHandler(OnMouseUp));
      AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));

      // Stretch by default
      HorizontalAlignment = HorizontalAlignment.Stretch;

      items.ListChanged += (sender, args) =>
      {
        switch (args.Type)
        {
          case ListChangedEventType.ItemsInserted:
          {
            for (var i = 0; i < args.Count; i++)
            {
              var itemBase = items[args.Index + i];
              if (itemBase is MenuItem)
              {
                (itemBase as MenuItem).ParentItem = this;
              }
            }

            break;
          }

          case ListChangedEventType.ItemsRemoved:
            foreach (object removedItem in args.RemovedItems)
            {
              if (removedItem is MenuItem)
              {
                (removedItem as MenuItem).ParentItem = null;
              }
            }

            break;

          case ListChangedEventType.ItemReplaced:
          {
            object removedItem = args.RemovedItems[0];
            if (removedItem is MenuItem)
              (removedItem as MenuItem).ParentItem = null;

            MenuItemBase itemBase = items[args.Index];
            if (itemBase is MenuItem)
            {
              (itemBase as MenuItem).ParentItem = this;
            }

            break;
          }
        }
      };
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
      if (args.wVirtualKeyCode == VirtualKeys.Return)
      {
        if (Type == MenuItemType.RootSubmenu || Type == MenuItemType.Submenu)
        {
          OpenMenu();
        }
        else if (Type == MenuItemType.Item)
        {
          RaiseClick();
        }

        args.Handled = true;
      }
    }

    private void OnMouseUp(object sender, MouseEventArgs args)
    {
      if (Type == MenuItemType.Item)
      {
        RaiseClick();
        args.Handled = true;
      }
    }

    private void OnMouseMove(object sender, MouseEventArgs args)
    {
      // Mouse move opens the submenus only in root level
      if (!_disabled && args.LeftButton == MouseButtonState.Pressed /*&& Parent.Parent is Menu*/)
      {
        OpenMenu();
      }

      args.Handled = true;
    }

    private void OnMouseDown(object sender, MouseEventArgs args)
    {
      if (!_disabled)
      {
        OpenMenu();
      }

      args.Handled = true;
    }

    private Popup popup;

    private void OpenMenu()
    {
      if (expanded)
      {
        return;
      }

      if (this.Type == MenuItemType.Submenu || Type == MenuItemType.RootSubmenu)
      {
        if (null == popup)
        {
          popup = new Popup(this.Items, this._popupShadow, this.ActualWidth);
          foreach (MenuItemBase itemBase in this.Items)
          {
            if (itemBase is MenuItem)
            {
              ((MenuItem) itemBase).ParentItem = this;
            }
          }

          popup.AddHandler(Window.ClosedEvent, new EventHandler(OnPopupClosed));
        }

        var windowsHost = VisualTreeHelper.FindClosestParent<WindowsHost>(this);
        var point = TranslatePoint(this, new Point(0, 0), windowsHost);
        popup.X = point.X;
        popup.Y = point.Y;
        windowsHost.ShowModal(popup, true);
        expanded = true;
      }
    }

    private void OnPopupClosed(object sender, EventArgs eventArgs)
    {
      assert(expanded);
      expanded = false;
    }

    public string Title { get; set; }

    private string _titleRight;

    public string TitleRight
    {
      get
      {
        if (_titleRight == null && Type == MenuItemType.Submenu)
        {
          return new string(UnicodeTable.ArrowRight, 1);
        }

        return _titleRight;
      }
      set { _titleRight = value; }
    }

    public string Description { get; set; }

    public MenuItemType Type { get; set; }

    private readonly ObservableList<MenuItemBase> items = new ObservableList<MenuItemBase>(new List<MenuItemBase>());

    public IList<MenuItemBase> Items
    {
      get { return items; }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      var length = 2;
      if (!string.IsNullOrEmpty(Title))
      {
        length += getTitleLength(Title);
      }

      if (!string.IsNullOrEmpty(TitleRight))
      {
        length += TitleRight.Length;
      }

      if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(TitleRight))
      {
        length++;
      }

      return new Size(length, 1);
    }

    /// <summary>
    /// Counts length of string to be rendered with underscore prefixes on.
    /// </summary>
    private static int getTitleLength(String title)
    {
      var underscore = false;
      var len = 0;
      foreach (var c in title)
      {
        if (underscore)
        {
          len++;
          underscore = false;
        }
        else
        {
          if (c == '_')
          {
            underscore = true;
          }
          else
          {
            len++;
          }
        }
      }

      return len;
    }

    public override void Render(RenderingBuffer buffer)
    {
      Attr captionAttrs;
      Attr specialAttrs;
      if (HasFocus || this.expanded)
      {
        captionAttrs = Colors.Blend(Color.Black, Color.DarkGreen);
        specialAttrs = Colors.Blend(Color.DarkRed, Color.DarkGreen);
      }
      else
      {
        captionAttrs = Colors.Blend(Color.Black, Color.Gray);
        specialAttrs = Colors.Blend(Color.DarkRed, Color.Gray);
      }

      if (_disabled)
      {
        captionAttrs = Colors.Blend(Color.DarkGray, Color.Gray);
      }

      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', captionAttrs);
      if (null != Title)
      {
        renderString(Title, buffer, 1, 0, ActualWidth, captionAttrs, Disabled ? captionAttrs : specialAttrs);
      }

      if (null != TitleRight)
      {
        RenderString(TitleRight, buffer, ActualWidth - TitleRight.Length - 1, 0, TitleRight.Length, captionAttrs);
      }
    }

    /// <summary>
    /// Renders string using attr, but if character is prefixed with underscore,
    /// symbol will use specialAttrs instead. To render underscore pass two underscores.
    /// Example: "_File" renders File when 'F' is rendered using specialAttrs.
    /// </summary>
    private static int renderString(string s, RenderingBuffer buffer, int x, int y, int maxWidth, Attr attr, Attr specialAttr)
    {
      var underscore = false;
      var j = 0;
      for (var i = 0; i < s.Length && j < maxWidth; i++)
      {
        char c;
        if (underscore)
        {
          c = s[i];
        }
        else
        {
          if (s[i] == '_')
          {
            underscore = true;
            continue;
          }
          else
          {
            c = s[i];
          }
        }

        Attr a;
        if (j + 2 >= maxWidth && j >= 2 && s.Length > maxWidth)
        {
          c = '.';
          a = attr;
        }
        else
        {
          a = underscore ? specialAttr : attr;
        }

        buffer.SetPixel(x + j, y, c, a);

        j++;
        underscore = false;
      }

      return j;
    }

    internal void Close()
    {
      assert(expanded);
      popup.Close();
    }

    internal void Expand()
    {
      OpenMenu();
    }

    private ICommand _command;

    public ICommand Command
    {
      get { return _command; }
      set
      {
        if (_command != value)
        {
          if (_command != null)
          {
            _command.CanExecuteChanged -= OnCommandCanExecuteChanged;
          }

          _command = value;
          _command.CanExecuteChanged += OnCommandCanExecuteChanged;

          RefreshCanExecute();
        }
      }
    }

    private void OnCommandCanExecuteChanged(object sender, EventArgs args)
    {
      RefreshCanExecute();
    }

    private void RefreshCanExecute()
    {
      if (_command == null)
      {
        this.Disabled = false;
        return;
      }

      this.Disabled = !_command.CanExecute(CommandParameter);
    }

    public object CommandParameter { get; set; }

    internal void RaiseClick()
    {
      RaiseEvent(ClickEvent, new RoutedEventArgs(this, ClickEvent));
      if (_command != null && _command.CanExecute(CommandParameter))
      {
        _command.Execute(CommandParameter);
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// В свёрнутом состоянии представляет собой однострочный контрол. При разворачивании списка
  /// создаётся всплывающее модальное кастомное окошко и показывается пользователю, причём первая
  /// строчка этого окна - прозрачная и через неё видно сам комбобокс (это нужно для того, чтобы
  /// обрабатывать клики по комбобоксу - при клике на прозрачную область комбобокс должен сворачиваться).
  /// Если этого бы не было, то с учётом того, что модальное окно показывается с флагом
  /// outsideClickWillCloseWindow = true, клик по самому комбобоксу приводил бы к мгновенному закрытию
  /// и открытию комбобокса заново.
  /// </summary>
  public class ComboBox : Control
  {
    private readonly bool shadow;

    public ComboBox() :
      this(true)
    {
    }

    /// <summary>
    /// Creates combobox instance.
    /// </summary>
    /// <param name="shadow">Display shadow or not</param>
    public ComboBox(bool shadow)
    {
      this.shadow = shadow;
      Focusable = true;
      AddHandler(MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
      AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
    }

    private bool Opened
    {
      get { return m_opened; }
      set
      {
        m_opened = value;
        Invalidate();
      }
    }

    public int? ShownItemsCount { get; set; }

    private void openPopup()
    {
      if (Opened) throw new InvalidOperationException("Assertion failed.");
      Window popup = new PopupWindow(Items, SelectedItemIndex ?? 0, shadow,
        ShownItemsCount != null ? ShownItemsCount.Value - 1 : (int?) null);
      Point popupCoord = TranslatePoint(this, new Point(0, 0),
        VisualTreeHelper.FindClosestParent<WindowsHost>(this));
      popup.X = popupCoord.X;
      popup.Y = popupCoord.Y;
      popup.Width = shadow ? ActualWidth + 1 : ActualWidth;
      if (Items.Count != 0)
        popup.Height = (ShownItemsCount != null ? ShownItemsCount.Value : Items.Count)
                       + (shadow ? 2 : 1); // 1 row for transparent "header"
      else popup.Height = shadow ? 3 : 2;
      WindowsHost windowsHost = VisualTreeHelper.FindClosestParent<WindowsHost>(this);
      windowsHost.ShowModal(popup, true);
      Opened = true;
      EventManager.AddHandler(popup, Window.ClosedEvent, new EventHandler(OnPopupClosed));
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
      if (args.VirtualKeyCode == VirtualKeys.Return)
      {
        openPopup();
      }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
      if (!Opened)
        openPopup();
    }

    private void OnPopupClosed(object o, EventArgs args)
    {
      if (!Opened) throw new InvalidOperationException("Assertion failed.");
      Opened = false;
      this.SelectedItemIndex = ((PopupWindow) o).IndexSelected;
      EventManager.RemoveHandler(o, Window.ClosedEvent, new EventHandler(OnPopupClosed));
    }

    private readonly List<String> items = new List<string>();

    public List<String> Items
    {
      get { return items; }
    }


    public int? SelectedItemIndex
    {
      get { return selectedItemIndex; }
      set
      {
        if (selectedItemIndex != value)
        {
          selectedItemIndex = value;
          Invalidate();
          RaisePropertyChanged("SelectedItemIndex");
        }
      }
    }

    private bool m_opened;
    private int? selectedItemIndex;

    public static Size EMPTY_SIZE = new Size(3, 1);

    protected override Size MeasureOverride(Size availableSize)
    {
      if (Items.Count == 0) return EMPTY_SIZE;
      int maxLen = Items.Max(s => s.Length);
      // 1 pixel from left, 1 from right, then arrow and 1 more empty pixel
      Size size = new Size(Math.Min(maxLen + 4, availableSize.Width), 1);
      return size;
    }

    public override void Render(RenderingBuffer buffer)
    {
      Attr attrs;
      if (HasFocus)
      {
        attrs = Colors.Blend(Color.White, Color.DarkGreen);
      }
      else attrs = Colors.Blend(Color.Black, Color.DarkCyan);

      buffer.SetPixel(0, 0, ' ', attrs);
      int usedForCurrentItem = 0;
      if (Items.Count != 0 && ActualWidth > 4)
      {
        usedForCurrentItem = RenderString(Items[SelectedItemIndex ?? 0], buffer, 1, 0, ActualWidth - 4, attrs);
      }

      buffer.FillRectangle(1 + usedForCurrentItem, 0, ActualWidth - (usedForCurrentItem + 1), 1, ' ', attrs);
      if (ActualWidth > 2)
      {
        buffer.SetPixel(ActualWidth - 2, 0, Opened ? '^' : 'v', attrs);
      }
    }
  }
}

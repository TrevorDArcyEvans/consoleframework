using System;
using System.Collections.Generic;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Класс, служащий хост-панелью для набора перекрывающихся окон.
  /// Хранит в себе список окон в порядке их Z-Order и отрисовывает рамки,
  /// управляет их перемещением.
  /// </summary>
  public class WindowsHost : Control
  {
    private Menu mainMenu;

    public Menu MainMenu
    {
      get { return mainMenu; }
      set
      {
        if (mainMenu != value)
        {
          if (mainMenu != null)
          {
            RemoveChild(mainMenu);
          }

          if (value != null)
          {
            InsertChildAt(0, value);
          }

          mainMenu = value;
        }
      }
    }

    public WindowsHost()
    {
      AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown), true);
      AddHandler(PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove), true);
      AddHandler(PreviewMouseUpEvent, new MouseEventHandler(OnPreviewMouseUp), true);
      AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
      AddHandler(PreviewMouseWheelEvent, new MouseWheelEventHandler(OnPreviewMouseWheel));
    }

    /// <summary>
    /// Interrupts wheel event propagation if its source window is not on top now.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
    {
      var windowsStartIndex = 0;
      if (mainMenu != null)
      {
        assert(Children[0] == mainMenu);
        windowsStartIndex++;
      }

      if (windowsStartIndex < Children.Count)
      {
        var topWindow = (Window) Children[Children.Count - 1];
        var sourceWindow = VisualTreeHelper.FindClosestParent<Window>((Control) args.Source);
        if (topWindow != sourceWindow)
        {
          args.Handled = true;
        }
      }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs args)
    {
      if (mainMenu != null)
      {
        if (mainMenu.TryMatchGesture(args))
        {
          args.Handled = true;
        }
      }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      var windowsStartIndex = 0;
      if (mainMenu != null)
      {
        assert(Children[0] == mainMenu);
        mainMenu.Measure(new Size(availableSize.Width, 1));
        windowsStartIndex++;
      }

      // Дочерние окна могут занимать сколько угодно пространства,
      // но при заданных Width/Height их размеры будут учтены
      // системой размещения автоматически
      for (var index = windowsStartIndex; index < Children.Count; index++)
      {
        var control = Children[index];
        var window = (Window) control;
        window.Measure(new Size(int.MaxValue, int.MaxValue));
      }

      return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      var windowsStartIndex = 0;
      if (mainMenu != null)
      {
        assert(Children[0] == mainMenu);
        mainMenu.Arrange(new Rect(0, 0, finalSize.Width, 1));
        windowsStartIndex++;
      }

      // сколько дочерние окна хотели - столько и получают
      for (var index = windowsStartIndex; index < Children.Count; index++)
      {
        var control = Children[index];
        var window = (Window) control;
        int x;
        if (window.X.HasValue)
        {
          x = window.X.Value;
        }
        else
        {
          x = (finalSize.Width - window.DesiredSize.Width) / 2;
        }

        int y;
        if (window.Y.HasValue)
        {
          y = window.Y.Value;
        }
        else
        {
          y = (finalSize.Height - window.DesiredSize.Height) / 2;
        }

        window.Arrange(new Rect(x, y, window.DesiredSize.Width, window.DesiredSize.Height));
      }

      return finalSize;
    }

    public override void Render(RenderingBuffer buffer)
    {
      buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', Attr.BACKGROUND_BLUE);
    }

    /// <summary>
    /// Делает указанное окно активным. Если оно до этого не было активным, то
    /// по Z-индексу оно будет перемещено на самый верх, и получит клавиатурный фокус ввода.
    /// </summary>
    public void ActivateWindow(Window window)
    {
      var index = Children.IndexOf(window);
      if (-1 == index)
        throw new InvalidOperationException("Could not find window");

      var oldTopWindow = Children[Children.Count - 1];
      for (var i = index; i < Children.Count - 1; i++)
      {
        SwapChildsZOrder(i, i + 1);
      }

      // If need to change top window
      if (oldTopWindow != window)
      {
        oldTopWindow.RaiseEvent(Window.DeactivatedEvent, new RoutedEventArgs(oldTopWindow, Window.DeactivatedEvent));
        window.RaiseEvent(Window.ActivatedEvent, new RoutedEventArgs(window, Window.ActivatedEvent));
      }

      // If need to change focus (it is not only when need to change top window)
      // It may be need to change focus from menu to window, for example
      if (ConsoleApplication.Instance.FocusManager.CurrentScope != window)
      {
        InitializeFocusOnActivatedWindow(window);
      }
    }

    private bool IsTopWindowModal()
    {
      var windowsStartIndex = 0;
      if (mainMenu != null)
      {
        assert(Children[0] == mainMenu);
        windowsStartIndex++;
      }

      if (Children.Count == windowsStartIndex)
      {
        return false;
      }
      return windowInfos[(Window) Children[Children.Count - 1]].Modal;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs args)
    {
      OnPreviewMouseEvents(args, 2);
    }

    private void OnPreviewMouseDown(object sender, MouseEventArgs args)
    {
      OnPreviewMouseEvents(args, 0);
    }

    private void OnPreviewMouseUp(object sender, MouseEventArgs args)
    {
      OnPreviewMouseEvents(args, 1);
    }

    /// <summary>
    /// Обработчик отвечает за вывод на передний план неактивных окон, на которые нажали мышкой,
    /// и за обработку мыши, когда имеется модальное окно - в этом случае обработчик не пропускает
    /// события, которые идут мимо модального окна, дальше по дереву (Tunneling) - устанавливая
    /// Handled в True, либо закрывает модальное окно, если оно было показано с флагом
    /// OutsideClickClosesWindow.
    /// eventType = 0 - PreviewMouseDown
    /// eventType = 1 - PreviewMouseUp
    /// eventType = 2 - PreviewMouseMove
    /// </summary>
    private void OnPreviewMouseEvents(MouseEventArgs args, int eventType)
    {
      var handle = false;
      check:
      if (IsTopWindowModal())
      {
        var modalWindow = (Window) Children[Children.Count - 1];
        var windowClicked = VisualTreeHelper.FindClosestParent<Window>((Control) args.Source);
        if (windowClicked != modalWindow)
        {
          if (windowInfos[modalWindow].OutsideClickClosesWindow
              && (eventType == 0 || eventType == 2 && args.LeftButton == MouseButtonState.Pressed))
          {
            // закрываем текущее модальное окно
            CloseWindow(modalWindow);

            // далее обрабатываем событие как обычно
            handle = true;

            // Если дальше снова модальное окно, проверку нужно повторить, и закрыть
            // его тоже, и так далее. Можно отрефакторить как вызов подпрограммы
            // вида while (closeTopModalWindowIfNeed()) ;
            goto check;
          }
          else
          {
            // прекращаем распространение события (правда, контролы, подписавшиеся с флагом
            // handledEventsToo, получат его в любом случае) и генерацию соответствующего
            // парного не-preview события
            args.Handled = true;
          }
        }
      }
      else
      {
        handle = true;
      }

      if (handle && (eventType == 0 || eventType == 2 && args.LeftButton == MouseButtonState.Pressed))
      {
        var windowClicked = VisualTreeHelper.FindClosestParent<Window>((Control) args.Source);
        if (null != windowClicked)
        {
          ActivateWindow(windowClicked);
        }
        else
        {
          var menu = VisualTreeHelper.FindClosestParent<Menu>((Control) args.Source);
          if (null != menu)
          {
            ActivateMenu();
          }
        }
      }
    }

    private void ActivateMenu()
    {
      assert(mainMenu != null);
      if (ConsoleApplication.Instance.FocusManager.CurrentScope != mainMenu)
      {
        ConsoleApplication.Instance.FocusManager.SetFocusScope(mainMenu);
      }
    }

    private static void InitializeFocusOnActivatedWindow(Window window)
    {
      ConsoleApplication.Instance.FocusManager.SetFocusScope(window);
      // todo : add window.ChildToFocus support again
    }
    
    private readonly Dictionary<Window, WindowInfo> windowInfos = new Dictionary<Window, WindowInfo>();

    /// <summary>
    /// Adds window to window host children and shows it as modal window.
    /// </summary>
    public void ShowModal(Window window, bool outsideClickWillCloseWindow = false)
    {
      ShowCore(window, true, outsideClickWillCloseWindow);
    }

    /// <summary>
    /// Adds window to window host children and shows it.
    /// </summary>
    public void Show(Window window)
    {
      ShowCore(window, false, false);
    }

    public Window TopWindow => GetTopWindow();

    private Window GetTopWindow()
    {
      var windowsStartIndex = 0;
      if (mainMenu != null)
      {
        assert(Children[0] == mainMenu);
        windowsStartIndex++;
      }

      if (Children.Count > windowsStartIndex)
      {
        return (Window) Children[Children.Count - 1];
      }

      return null;
    }

    private void ShowCore(Window window, bool modal, bool outsideClickWillCloseWindow)
    {
      Control topWindow = GetTopWindow();
      if (null != topWindow)
      {
        topWindow.RaiseEvent(Window.DeactivatedEvent, new RoutedEventArgs(topWindow, Window.DeactivatedEvent));
      }

      AddChild(window);
      window.RaiseEvent(Window.ActivatedEvent, new RoutedEventArgs(window, Window.ActivatedEvent));
      InitializeFocusOnActivatedWindow(window);
      windowInfos.Add(window, new WindowInfo(modal, outsideClickWillCloseWindow));
    }

    /// <summary>
    /// Removes window from window host.
    /// </summary>
    public void CloseWindow(Window window)
    {
      windowInfos.Remove(window);
      window.RaiseEvent(Window.DeactivatedEvent, new RoutedEventArgs(window, Window.DeactivatedEvent));
      RemoveChild(window);
      window.RaiseEvent(Window.ClosedEvent, new RoutedEventArgs(window, Window.ClosedEvent));
      // после удаления окна активизировать то, которое было активным до него
      var childrenOrderedByZIndex = GetChildrenOrderedByZIndex();

      var windowsStartIndex = 0;
      if (mainMenu != null)
      {
        assert(Children[0] == mainMenu);
        windowsStartIndex++;
      }

      if (childrenOrderedByZIndex.Count > windowsStartIndex)
      {
        Window topWindow = (Window) childrenOrderedByZIndex[childrenOrderedByZIndex.Count - 1];
        topWindow.RaiseEvent(Window.ActivatedEvent, new RoutedEventArgs(topWindow, Window.ActivatedEvent));
        InitializeFocusOnActivatedWindow(topWindow);
        Invalidate();
      }
    }
  }
}

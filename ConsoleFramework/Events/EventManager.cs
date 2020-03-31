using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ConsoleFramework.Controls;
using ConsoleFramework.Core;
using ConsoleFramework.Native;

namespace ConsoleFramework.Events
{
  /// <summary>
  /// Central point of events management routine.
  /// Provides events routing.
  /// </summary>
  public sealed class EventManager
  {
    private readonly Stack<Control> _inputCaptureStack = new Stack<Control>();

    private class DelegateInfo
    {
      public readonly Delegate @delegate;
      public readonly bool handledEventsToo;

      public DelegateInfo(Delegate @delegate, bool handledEventsToo)
      {
        this.@delegate = @delegate;
        this.handledEventsToo = handledEventsToo;
      }
    }

    private class RoutedEventTargetInfo
    {
      public readonly object target;
      public List<DelegateInfo> handlersList;

      public RoutedEventTargetInfo(object target)
      {
        if (null == target)
          throw new ArgumentNullException("target");
        this.target = target;
      }
    }

    private class RoutedEventInfo
    {
      public List<RoutedEventTargetInfo> targetsList;

      public RoutedEventInfo(RoutedEvent routedEvent)
      {
        if (null == routedEvent)
          throw new ArgumentNullException("routedEvent");
      }
    }

    private static readonly Dictionary<RoutedEventKey, RoutedEventInfo> _routedEvents = new Dictionary<RoutedEventKey, RoutedEventInfo>();

    public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType)
    {
      if (string.IsNullOrEmpty(name))
      {
        throw new ArgumentException("name");
      }

      if (null == handlerType)
      {
        throw new ArgumentNullException("handlerType");
      }

      if (null == ownerType)
      {
        throw new ArgumentNullException("ownerType");
      }

      var key = new RoutedEventKey(name, ownerType);
      if (_routedEvents.ContainsKey(key))
      {
        throw new InvalidOperationException("This routed event is already registered.");
      }

      var routedEvent = new RoutedEvent(handlerType, name, ownerType, routingStrategy);
      var routedEventInfo = new RoutedEventInfo(routedEvent);
      _routedEvents.Add(key, routedEventInfo);
      return routedEvent;
    }

    public static void AddHandler(object target, RoutedEvent routedEvent, Delegate handler)
    {
      AddHandler(target, routedEvent, handler, false);
    }

    public static void AddHandler(object target, RoutedEvent routedEvent, Delegate handler, bool handledEventsToo)
    {
      if (null == target)
      {
        throw new ArgumentNullException("target");
      }

      if (null == routedEvent)
      {
        throw new ArgumentNullException("routedEvent");
      }

      if (null == handler)
      {
        throw new ArgumentNullException("handler");
      }

      var key = routedEvent.Key;
      if (!_routedEvents.ContainsKey(key))
      {
        throw new ArgumentException("Specified routed event is not registered.", "routedEvent");
      }

      var routedEventInfo = _routedEvents[key];
      var needAddTarget = true;
      if (routedEventInfo.targetsList != null)
      {
        var targetInfo = routedEventInfo.targetsList.FirstOrDefault(info => info.target == target);
        if (null != targetInfo)
        {
          if (targetInfo.handlersList == null)
          {
            targetInfo.handlersList = new List<DelegateInfo>();
          }

          targetInfo.handlersList.Add(new DelegateInfo(handler, handledEventsToo));
          needAddTarget = false;
        }
      }

      if (needAddTarget)
      {
        var targetInfo = new RoutedEventTargetInfo(target);
        targetInfo.handlersList = new List<DelegateInfo>();
        targetInfo.handlersList.Add(new DelegateInfo(handler, handledEventsToo));
        if (routedEventInfo.targetsList == null)
        {
          routedEventInfo.targetsList = new List<RoutedEventTargetInfo>();
        }

        routedEventInfo.targetsList.Add(targetInfo);
      }
    }

    public static void RemoveHandler(object target, RoutedEvent routedEvent, Delegate handler)
    {
      if (null == target)
      {
        throw new ArgumentNullException("target");
      }

      if (null == routedEvent)
      {
        throw new ArgumentNullException("routedEvent");
      }

      if (null == handler)
      {
        throw new ArgumentNullException("handler");
      }

      var key = routedEvent.Key;
      if (!_routedEvents.ContainsKey(key))
      {
        throw new ArgumentException("Specified routed event is not registered.", "routedEvent");
      }

      var routedEventInfo = _routedEvents[key];
      if (routedEventInfo.targetsList == null)
      {
        throw new InvalidOperationException("Targets list is empty.");
      }

      var targetInfo = routedEventInfo.targetsList.FirstOrDefault(info => info.target == target);
      if (null == targetInfo)
      {
        throw new ArgumentException("Target not found in targets list of specified routed event.", "target");
      }

      if (null == targetInfo.handlersList)
      {
        throw new InvalidOperationException("Handlers list is empty.");
      }

      var findIndex = targetInfo.handlersList.FindIndex(info => info.@delegate == handler);
      if (-1 == findIndex)
      {
        throw new ArgumentException("Specified handler not found.", "handler");
      }

      targetInfo.handlersList.RemoveAt(findIndex);
    }

    /// <summary>
    /// Возвращает список таргетов, подписанных на указанное RoutedEvent.
    /// </summary>
    private static List<RoutedEventTargetInfo> GetTargetsSubscribedTo(RoutedEvent routedEvent)
    {
      if (null == routedEvent)
      {
        throw new ArgumentNullException("routedEvent");
      }

      var key = routedEvent.Key;
      if (!_routedEvents.ContainsKey(key))
      {
        throw new ArgumentException("Specified routed event is not registered.", "routedEvent");
      }

      var routedEventInfo = _routedEvents[key];
      return routedEventInfo.targetsList;
    }

    public void BeginCaptureInput(Control control)
    {
      if (null == control)
      {
        throw new ArgumentNullException("control");
      }

      _inputCaptureStack.Push(control);
    }

    public void EndCaptureInput(Control control)
    {
      if (null == control)
      {
        throw new ArgumentNullException("control");
      }

      if (_inputCaptureStack.Peek() != control)
      {
        throw new InvalidOperationException("Last control captured the input differs from specified in argument.");
      }

      _inputCaptureStack.Pop();
    }

    private readonly Queue<RoutedEventArgs> _eventsQueue = new Queue<RoutedEventArgs>();

    private static MouseButtonState GetLeftButtonState(MOUSE_BUTTON_STATE rawState)
    {
      return (rawState & MOUSE_BUTTON_STATE.FROM_LEFT_1ST_BUTTON_PRESSED) ==
             MOUSE_BUTTON_STATE.FROM_LEFT_1ST_BUTTON_PRESSED
        ? MouseButtonState.Pressed
        : MouseButtonState.Released;
    }

    private static MouseButtonState GetMiddleButtonState(MOUSE_BUTTON_STATE rawState)
    {
      return (rawState & MOUSE_BUTTON_STATE.FROM_LEFT_2ND_BUTTON_PRESSED) ==
             MOUSE_BUTTON_STATE.FROM_LEFT_2ND_BUTTON_PRESSED
        ? MouseButtonState.Pressed
        : MouseButtonState.Released;
    }

    private static MouseButtonState GetRightButtonState(MOUSE_BUTTON_STATE rawState)
    {
      return (rawState & MOUSE_BUTTON_STATE.RIGHTMOST_BUTTON_PRESSED) ==
             MOUSE_BUTTON_STATE.RIGHTMOST_BUTTON_PRESSED
        ? MouseButtonState.Pressed
        : MouseButtonState.Released;
    }

    private MouseButtonState _lastLeftMouseButtonState = MouseButtonState.Released;
    private MouseButtonState _lastMiddleMouseButtonState = MouseButtonState.Released;
    private MouseButtonState _lastRightMouseButtonState = MouseButtonState.Released;

    private readonly List<Control> _prevMouseOverStack = new List<Control>();

    private Point _lastMousePosition;

    // Auto-repeating mouse left click when holding _pressed button
    private bool _autoRepeatTimerRunning = false;
    private Timer _timer;
    private MouseButtonEventArgs _lastMousePressEventArgs;

    private void StartAutoRepeatTimer(MouseButtonEventArgs eventArgs)
    {
      _lastMousePressEventArgs = eventArgs;
      _timer = new Timer(state =>
      {
        ConsoleApplication.Instance.RunOnUiThread(() =>
        {
          if (_autoRepeatTimerRunning)
          {
            _eventsQueue.Enqueue(new MouseButtonEventArgs(
              _lastMousePressEventArgs.Source,
              Control.MouseDownEvent,
              _lastMousePosition,
              _lastMousePressEventArgs.LeftButton,
              _lastMousePressEventArgs.MiddleButton,
              _lastMousePressEventArgs.RightButton,
              MouseButton.Left,
              1,
              true
            ));
          }
        });
        // todo : make this constants configurable
      }, null, TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(100));
      _autoRepeatTimerRunning = true;
    }

    private void StopAutoRepeatTimer()
    {
      _timer.Dispose();
      _timer = null;
      _autoRepeatTimerRunning = false;
      _lastMousePressEventArgs = null;
    }

    public void ParseInputEvent(INPUT_RECORD inputRecord, Control rootElement)
    {
      if (inputRecord.EventType == EventType.MOUSE_EVENT)
      {
        MOUSE_EVENT_RECORD mouseEvent = inputRecord.MouseEvent;

        if (mouseEvent.dwEventFlags != MouseEventFlags.PRESSED_OR_RELEASED &&
            mouseEvent.dwEventFlags != MouseEventFlags.MOUSE_MOVED &&
            mouseEvent.dwEventFlags != MouseEventFlags.DOUBLE_CLICK &&
            mouseEvent.dwEventFlags != MouseEventFlags.MOUSE_WHEELED &&
            mouseEvent.dwEventFlags != MouseEventFlags.MOUSE_HWHEELED)
        {
          throw new InvalidOperationException("Flags combination in mouse event was not expected.");
        }

        Point rawPosition;
        if (mouseEvent.dwEventFlags == MouseEventFlags.MOUSE_MOVED ||
            mouseEvent.dwEventFlags == MouseEventFlags.PRESSED_OR_RELEASED)
        {
          rawPosition = new Point(mouseEvent.dwMousePosition.X, mouseEvent.dwMousePosition.Y);
          _lastMousePosition = rawPosition;
        }
        else
        {
          // При событии MOUSE_WHEELED в Windows некорректно устанавливается mouseEvent.dwMousePosition
          // Поэтому для определения элемента, над которым производится прокручивание колёсика, мы
          // вынуждены сохранять координаты, полученные при предыдущем событии мыши
          rawPosition = _lastMousePosition;
        }

        var topMost = VisualTreeHelper.FindTopControlUnderMouse(rootElement, Control.TranslatePoint(null, rawPosition, rootElement));

        // если мышь захвачена контролом, то события перемещения мыши доставляются только ему,
        // события, связанные с нажатием мыши - тоже доставляются только ему, вместо того
        // контрола, над которым событие было зарегистрировано. Такой механизм необходим,
        // например, для корректной обработки перемещений окон (вверх или в стороны)
        var source = (_inputCaptureStack.Count != 0) ? _inputCaptureStack.Peek() : topMost;

        // No sense to further process event with no source control
        if (source == null) return;

        if (mouseEvent.dwEventFlags == MouseEventFlags.MOUSE_MOVED)
        {
          MouseButtonState leftMouseButtonState = GetLeftButtonState(mouseEvent.dwButtonState);
          MouseButtonState middleMouseButtonState = GetMiddleButtonState(mouseEvent.dwButtonState);
          MouseButtonState rightMouseButtonState = GetRightButtonState(mouseEvent.dwButtonState);
          //
          MouseEventArgs mouseEventArgs = new MouseEventArgs(source, Control.PreviewMouseMoveEvent,
            rawPosition,
            leftMouseButtonState,
            middleMouseButtonState,
            rightMouseButtonState
          );
          _eventsQueue.Enqueue(mouseEventArgs);

          _lastLeftMouseButtonState = leftMouseButtonState;
          _lastMiddleMouseButtonState = middleMouseButtonState;
          _lastRightMouseButtonState = rightMouseButtonState;

          // detect mouse enter / mouse leave events

          // path to source from root element down
          var mouseOverStack = new List<Control>();
          var current = topMost;
          while (null != current)
          {
            mouseOverStack.Insert(0, current);
            current = current.Parent;
          }

          int index;
          for (index = 0; index < Math.Min(mouseOverStack.Count, _prevMouseOverStack.Count); index++)
          {
            if (mouseOverStack[index] != _prevMouseOverStack[index])
            {
              break;
            }
          }

          for (var i = _prevMouseOverStack.Count - 1; i >= index; i--)
          {
            var control = _prevMouseOverStack[i];
            var args = new MouseEventArgs(control, Control.MouseLeaveEvent,
              rawPosition,
              leftMouseButtonState,
              middleMouseButtonState,
              rightMouseButtonState
            );
            _eventsQueue.Enqueue(args);
          }

          for (var i = index; i < mouseOverStack.Count; i++)
          {
            // enqueue MouseEnter event
            var control = mouseOverStack[i];
            var args = new MouseEventArgs(control, Control.MouseEnterEvent,
              rawPosition,
              leftMouseButtonState,
              middleMouseButtonState,
              rightMouseButtonState
            );
            _eventsQueue.Enqueue(args);
          }

          _prevMouseOverStack.Clear();
          _prevMouseOverStack.AddRange(mouseOverStack);
        }

        if (mouseEvent.dwEventFlags == MouseEventFlags.PRESSED_OR_RELEASED)
        {
          var leftMouseButtonState = GetLeftButtonState(mouseEvent.dwButtonState);
          var middleMouseButtonState = GetMiddleButtonState(mouseEvent.dwButtonState);
          var rightMouseButtonState = GetRightButtonState(mouseEvent.dwButtonState);

          MouseButtonEventArgs eventArgs = null;
          if (leftMouseButtonState != _lastLeftMouseButtonState)
          {
            eventArgs = new MouseButtonEventArgs(source,
              leftMouseButtonState == MouseButtonState.Pressed ? Control.PreviewMouseDownEvent : Control.PreviewMouseUpEvent,
              rawPosition,
              leftMouseButtonState,
              _lastMiddleMouseButtonState,
              _lastRightMouseButtonState,
              MouseButton.Left
            );
          }

          if (middleMouseButtonState != _lastMiddleMouseButtonState)
          {
            eventArgs = new MouseButtonEventArgs(source,
              middleMouseButtonState == MouseButtonState.Pressed ? Control.PreviewMouseDownEvent : Control.PreviewMouseUpEvent,
              rawPosition,
              _lastLeftMouseButtonState,
              middleMouseButtonState,
              _lastRightMouseButtonState,
              MouseButton.Middle
            );
          }

          if (rightMouseButtonState != _lastRightMouseButtonState)
          {
            eventArgs = new MouseButtonEventArgs(source,
              rightMouseButtonState == MouseButtonState.Pressed ? Control.PreviewMouseDownEvent : Control.PreviewMouseUpEvent,
              rawPosition,
              _lastLeftMouseButtonState,
              _lastMiddleMouseButtonState,
              rightMouseButtonState,
              MouseButton.Right
            );
          }

          if (eventArgs != null)
          {
            _eventsQueue.Enqueue(eventArgs);
          }

          _lastLeftMouseButtonState = leftMouseButtonState;
          _lastMiddleMouseButtonState = middleMouseButtonState;
          _lastRightMouseButtonState = rightMouseButtonState;

          if (leftMouseButtonState == MouseButtonState.Pressed)
          {
            if (eventArgs != null && !_autoRepeatTimerRunning)
            {
              StartAutoRepeatTimer(eventArgs);
            }
          }
          else
          {
            if (eventArgs != null && _autoRepeatTimerRunning)
            {
              StopAutoRepeatTimer();
            }
          }
        }

        if (mouseEvent.dwEventFlags == MouseEventFlags.MOUSE_WHEELED)
        {
          var args = new MouseWheelEventArgs(
            topMost,
            Control.PreviewMouseWheelEvent,
            rawPosition,
            _lastLeftMouseButtonState, _lastMiddleMouseButtonState,
            _lastRightMouseButtonState,
            mouseEvent.dwButtonState > 0 ? 1 : -1
          );
          _eventsQueue.Enqueue(args);
        }
      }

      if (inputRecord.EventType == EventType.KEY_EVENT)
      {
        var keyEvent = inputRecord.KeyEvent;
        var eventArgs = new KeyEventArgs(
          ConsoleApplication.Instance.FocusManager.FocusedElement,
          keyEvent.bKeyDown ? Control.PreviewKeyDownEvent : Control.PreviewKeyUpEvent);
        eventArgs.UnicodeChar = keyEvent.UnicodeChar;
        eventArgs.KeyDown = keyEvent.bKeyDown;
        eventArgs.ControlKeyState = keyEvent.dwControlKeyState;
        eventArgs.RepeatCount = keyEvent.wRepeatCount;
        eventArgs.VirtualKeyCode = keyEvent.wVirtualKeyCode;
        eventArgs.VirtualScanCode = keyEvent.wVirtualScanCode;
        _eventsQueue.Enqueue(eventArgs);
      }
    }

    /// <summary>
    /// Processes all routed events in queue.
    /// </summary>
    public void ProcessEvents()
    {
      while (_eventsQueue.Count != 0)
      {
        var routedEventArgs = _eventsQueue.Dequeue();
        ProcessRoutedEvent(routedEventArgs.RoutedEvent, routedEventArgs);
      }
    }

    public bool IsQueueEmpty()
    {
      return _eventsQueue.Count == 0;
    }

    private static bool IsControlAllowedToReceiveEvents(Control control, Control capturingControl)
    {
      var c = control;
      while (true)
      {
        if (c == capturingControl)
        {
          return true;
        }

        if (c == null)
        {
          return false;
        }

        c = c.Parent;
      }
    }

    internal bool ProcessRoutedEvent(RoutedEvent routedEvent, RoutedEventArgs args)
    {
      if (null == routedEvent)
      {
        throw new ArgumentNullException("routedEvent");
      }

      if (null == args)
      {
        throw new ArgumentNullException("args");
      }

      var subscribedTargets = GetTargetsSubscribedTo(routedEvent);

      var capturingControl = _inputCaptureStack.Count != 0 ? _inputCaptureStack.Peek() : null;
      if (routedEvent.RoutingStrategy == RoutingStrategy.Direct)
      {
        if (null == subscribedTargets)
        {
          return false;
        }

        var targetInfo = subscribedTargets.FirstOrDefault(info => info.target == args.Source);
        if (null == targetInfo)
        {
          return false;
        }

        // если имеется контрол, захватывающий события, события получает только он сам
        // и его дочерние контролы
        if (capturingControl != null)
        {
          if (!(args.Source is Control))
          {
            return false;
          }

          if (!IsControlAllowedToReceiveEvents((Control) args.Source, capturingControl))
          {
            return false;
          }
        }

        // copy handlersList to local list to avoid modifications when enumerating
        foreach (var delegateInfo in new List<DelegateInfo>(targetInfo.handlersList))
        {
          if (!args.Handled || delegateInfo.handledEventsToo)
          {
            if (delegateInfo.@delegate is RoutedEventHandler)
            {
              ((RoutedEventHandler) delegateInfo.@delegate).Invoke(targetInfo.target, args);
            }
            else
            {
              delegateInfo.@delegate.DynamicInvoke(targetInfo.target, args);
            }
          }
        }
      }

      var source = (Control) args.Source;
      // path to source from root element down to Source
      var path = new List<Control>();
      var current = source;
      while (null != current)
      {
        // та же логика с контролом, захватившим обработку сообщений
        // если имеется контрол, захватывающий события, события получает только он сам
        // и его дочерние контролы
        if (capturingControl == null || IsControlAllowedToReceiveEvents(current, capturingControl))
        {
          path.Insert(0, current);
          current = current.Parent;
        }
        else
        {
          break;
        }
      }

      if (routedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
      {
        if (subscribedTargets != null)
        {
          foreach (var potentialTarget in path)
          {
            var target = potentialTarget;
            var targetInfo = subscribedTargets.FirstOrDefault(info => info.target == target);
            if (null != targetInfo)
            {
              foreach (var delegateInfo in new List<DelegateInfo>(targetInfo.handlersList))
              {
                if (!args.Handled || delegateInfo.handledEventsToo)
                {
                  if (delegateInfo.@delegate is RoutedEventHandler)
                  {
                    ((RoutedEventHandler) delegateInfo.@delegate).Invoke(target, args);
                  }
                  else
                  {
                    delegateInfo.@delegate.DynamicInvoke(target, args);
                  }
                }
              }
            }
          }
        }

        // для парных Preview-событий запускаем соответствующие настоящие события,
        // сохраняя при этом Handled (если Preview событие помечено как Handled=true,
        // то и настоящее событие будет маршрутизировано с Handled=true)
        if (routedEvent == Control.PreviewMouseDownEvent)
        {
          var mouseArgs = ((MouseButtonEventArgs) args);
          var argsNew = new MouseButtonEventArgs(
            args.Source, Control.MouseDownEvent, mouseArgs.RawPosition,
            mouseArgs.LeftButton, mouseArgs.MiddleButton, mouseArgs.RightButton,
            mouseArgs.ChangedButton
          );
          argsNew.Handled = args.Handled;
          _eventsQueue.Enqueue(argsNew);
        }

        if (routedEvent == Control.PreviewMouseUpEvent)
        {
          var mouseArgs = ((MouseButtonEventArgs) args);
          var argsNew = new MouseButtonEventArgs(
            args.Source, Control.MouseUpEvent, mouseArgs.RawPosition,
            mouseArgs.LeftButton, mouseArgs.MiddleButton, mouseArgs.RightButton,
            mouseArgs.ChangedButton
          );
          argsNew.Handled = args.Handled;
          _eventsQueue.Enqueue(argsNew);
        }

        if (routedEvent == Control.PreviewMouseMoveEvent)
        {
          var mouseArgs = ((MouseEventArgs) args);
          var argsNew = new MouseEventArgs(
            args.Source, Control.MouseMoveEvent, mouseArgs.RawPosition,
            mouseArgs.LeftButton, mouseArgs.MiddleButton, mouseArgs.RightButton
          );
          argsNew.Handled = args.Handled;
          _eventsQueue.Enqueue(argsNew);
        }

        if (routedEvent == Control.PreviewMouseWheelEvent)
        {
          var oldArgs = ((MouseWheelEventArgs) args);
          MouseEventArgs argsNew = new MouseWheelEventArgs(
            args.Source, Control.MouseWheelEvent, oldArgs.RawPosition,
            oldArgs.LeftButton, oldArgs.MiddleButton, oldArgs.RightButton,
            oldArgs.Delta
          );
          argsNew.Handled = args.Handled;
          _eventsQueue.Enqueue(argsNew);
        }

        if (routedEvent == Control.PreviewKeyDownEvent)
        {
          var argsNew = new KeyEventArgs(args.Source, Control.KeyDownEvent);
          var keyEventArgs = ((KeyEventArgs) args);
          argsNew.UnicodeChar = keyEventArgs.UnicodeChar;
          argsNew.KeyDown = keyEventArgs.KeyDown;
          argsNew.ControlKeyState = keyEventArgs.ControlKeyState;
          argsNew.RepeatCount = keyEventArgs.RepeatCount;
          argsNew.VirtualKeyCode = keyEventArgs.VirtualKeyCode;
          argsNew.VirtualScanCode = keyEventArgs.VirtualScanCode;
          argsNew.Handled = args.Handled;
          _eventsQueue.Enqueue(argsNew);
        }

        if (routedEvent == Control.PreviewKeyUpEvent)
        {
          var argsNew = new KeyEventArgs(args.Source, Control.KeyUpEvent);
          var keyEventArgs = ((KeyEventArgs) args);
          argsNew.UnicodeChar = keyEventArgs.UnicodeChar;
          argsNew.KeyDown = keyEventArgs.KeyDown;
          argsNew.ControlKeyState = keyEventArgs.ControlKeyState;
          argsNew.RepeatCount = keyEventArgs.RepeatCount;
          argsNew.VirtualKeyCode = keyEventArgs.VirtualKeyCode;
          argsNew.VirtualScanCode = keyEventArgs.VirtualScanCode;
          argsNew.Handled = args.Handled;
          _eventsQueue.Enqueue(argsNew);
        }
      }

      if (routedEvent.RoutingStrategy == RoutingStrategy.Bubble)
      {
        if (subscribedTargets != null)
        {
          for (var i = path.Count - 1; i >= 0; i--)
          {
            var target = path[i];
            var targetInfo = subscribedTargets.FirstOrDefault(info => info.target == target);
            if (null != targetInfo)
            {
              foreach (var delegateInfo in new List<DelegateInfo>(targetInfo.handlersList))
              {
                if (!args.Handled || delegateInfo.handledEventsToo)
                {
                  if (delegateInfo.@delegate is RoutedEventHandler)
                  {
                    ((RoutedEventHandler) delegateInfo.@delegate).Invoke(target, args);
                  }
                  else
                  {
                    delegateInfo.@delegate.DynamicInvoke(target, args);
                  }
                }
              }
            }
          }
        }
      }

      return args.Handled;
    }

    /// <summary>
    /// Adds specified routed event to event queue. This event will be processed in next pass.
    /// </summary>
    internal void QueueEvent(RoutedEvent routedEvent, RoutedEventArgs args)
    {
      if (routedEvent != args.RoutedEvent)
      {
        throw new ArgumentException("Routed event doesn't match to routedEvent passed.", "args");
      }

      this._eventsQueue.Enqueue(args);
    }
  }
}

using System;
using ConsoleFramework.Events;
using ConsoleFramework.Native;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Base class for buttons and toggle buttons (checkboxes and radio buttons).
  /// </summary>
  public abstract class ButtonBase : Control, ICommandSource
  {
    /// <summary>
    /// Is button in _clicking mode (when mouse _pressed but not released yet).
    /// </summary>
    private bool _clicking;

    /// <summary>
    /// Is button _pressed using mouse now.
    /// </summary>
    protected bool _pressed;

    /// <summary>
    /// True in some time after user has _pressed button using keyboard
    /// (~ 0.5 second) - just for animate pressing
    /// </summary>
    protected bool _pressedUsingKeyboard;

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

    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click",
      RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ButtonBase));

    public event RoutedEventHandler OnClick
    {
      add { AddHandler(ClickEvent, value); }
      remove { RemoveHandler(ClickEvent, value); }
    }

    protected ButtonBase()
    {
      AddHandler(MouseDownEvent, new MouseButtonEventHandler(Button_OnMouseDown));
      AddHandler(MouseUpEvent, new MouseButtonEventHandler(Button_OnMouseUp));
      AddHandler(MouseEnterEvent, new MouseEventHandler(Button_MouseEnter));
      AddHandler(MouseLeaveEvent, new MouseEventHandler(Button_MouseLeave));
      AddHandler(KeyDownEvent, new KeyEventHandler(Button_KeyDown));
      Focusable = true;
    }

    private void Button_KeyDown(object sender, KeyEventArgs args)
    {
      if (Disabled)
      {
        return;
      }

      if (args.wVirtualKeyCode == VirtualKeys.Space
          || args.wVirtualKeyCode == VirtualKeys.Return)
      {
        RaiseEvent(ClickEvent, new RoutedEventArgs(this, ClickEvent));
        if (_command != null && _command.CanExecute(CommandParameter))
        {
          _command.Execute(CommandParameter);
        }

        _pressedUsingKeyboard = true;
        Invalidate();
        ConsoleApplication.Instance.Post(() =>
        {
          _pressedUsingKeyboard = false;
          Invalidate();
        }, TimeSpan.FromMilliseconds(300));
        args.Handled = true;
      }
    }

    private void Button_MouseEnter(object sender, MouseEventArgs args)
    {
      if (_clicking)
      {
        if (!_pressed)
        {
          _pressed = true;
          Invalidate();
        }
      }
    }

    private void Button_MouseLeave(object sender, MouseEventArgs args)
    {
      if (_clicking)
      {
        if (_pressed)
        {
          _pressed = false;
          Invalidate();
        }
      }
    }

    private void Button_OnMouseDown(object sender, MouseButtonEventArgs args)
    {
      if (!_clicking && !Disabled)
      {
        _clicking = true;
        _pressed = true;
        ConsoleApplication.Instance.BeginCaptureInput(this);
        this.Invalidate();
        args.Handled = true;
      }
    }

    private void Button_OnMouseUp(object sender, MouseButtonEventArgs args)
    {
      if (_clicking && !Disabled)
      {
        _clicking = false;
        if (_pressed)
        {
          _pressed = false;
          this.Invalidate();
        }

        if (HitTest(args.RawPosition))
        {
          RaiseEvent(ClickEvent, new RoutedEventArgs(this, ClickEvent));
          if (_command != null && _command.CanExecute(CommandParameter))
          {
            _command.Execute(CommandParameter);
          }
        }

        ConsoleApplication.Instance.EndCaptureInput(this);
        args.Handled = true;
      }
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
  }
}

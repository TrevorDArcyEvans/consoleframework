using System;

namespace ConsoleFramework.Events
{
  /// <summary>
  /// Command constructed from delegates.
  /// CanExecute = True by default if no canExecute functor provided.
  /// </summary>
  public class RelayCommand : ICommand
  {
    private readonly Action<object> _action;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> action)
    {
      if (null == action)
      {
        throw new ArgumentNullException("action");
      }

      this._action = action;
    }

    public RelayCommand(Action<object> action, Func<object, bool> canExecute)
      : this(action)
    {
      if (null == canExecute)
      {
        throw new ArgumentNullException("canExecute");
      }

      this._canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
      return _canExecute == null || _canExecute.Invoke(parameter);
    }

    public void Execute(object parameter)
    {
      if (CanExecute(parameter))
        _action.Invoke(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
      if (CanExecuteChanged != null)
        CanExecuteChanged.Invoke(this, EventArgs.Empty);
    }
  }
}

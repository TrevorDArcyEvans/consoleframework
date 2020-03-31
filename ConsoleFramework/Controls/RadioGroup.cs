using ConsoleFramework.Events;

namespace ConsoleFramework.Controls
{
  public class RadioGroup : Panel
  {
    private int? _selectedItemIndex;

    public int? SelectedItemIndex
    {
      get { return _selectedItemIndex; }
      set
      {
        if (_selectedItemIndex != value)
        {
          _selectedItemIndex = value;
          RaisePropertyChanged("SelectedItemIndex");
          RaisePropertyChanged("SelectedItem");
        }
      }
    }

    public RadioButton SelectedItem
    {
      get { return _selectedItemIndex.HasValue ? (RadioButton) ((Control) this).Children[_selectedItemIndex.Value] : null; }
    }

    public RadioGroup()
    {
      Children.ControlAdded += OnControlAdded;
      Children.ControlRemoved -= OnControlRemoved;
    }

    private void OnControlRemoved(Control control)
    {
      if (!(control is RadioButton)) return;
      var radioButton = (RadioButton) control;
      radioButton.OnClick -= RadioButton_OnClick;
    }

    private void OnControlAdded(Control control)
    {
      if (!(control is RadioButton)) return;
      var radioButton = (RadioButton) control;
      radioButton.OnClick += RadioButton_OnClick;
      int index = ((Control) this).Children.IndexOf(radioButton);
      radioButton.Checked = _selectedItemIndex != null && (_selectedItemIndex == index);
    }

    private void RadioButton_OnClick(object sender, RoutedEventArgs args)
    {
      foreach (var child in Children)
      {
        if (child is RadioButton && child != sender)
        {
          ((RadioButton) child).Checked = false;
        }
      }

      ((RadioButton) sender).Checked = true;
      var index = ((Control) this).Children.IndexOf((Control) sender);
      SelectedItemIndex = index;
    }
  }
}

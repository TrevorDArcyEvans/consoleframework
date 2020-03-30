namespace ConsoleFramework.Controls
{
  public class ColumnDefinition
  {
    public ColumnDefinition()
    {
      Width = new GridLength(GridUnitType.Auto, 0);
    }

    public GridLength Width { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
  }
}

namespace ConsoleFramework.Controls
{
  public class RowDefinition
  {
    public RowDefinition()
    {
      Height = new GridLength(GridUnitType.Auto, 0);
    }

    public GridLength Height { get; set; }
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
  }
}

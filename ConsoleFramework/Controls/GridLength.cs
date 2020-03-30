using Xaml;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Represents the length of elements that explicitly support Star unit types.
  /// </summary>
  [TypeConverter(typeof(GridLengthTypeConverter))]
  public struct GridLength
  {
    private readonly GridUnitType _gridUnitType;
    private readonly int _value;

    public GridLength(GridUnitType unitType, int value)
    {
      this._gridUnitType = unitType;
      this._value = value;
    }

    public GridUnitType GridUnitType
    {
      get { return _gridUnitType; }
    }

    public int Value
    {
      get { return _value; }
    }
  }
}

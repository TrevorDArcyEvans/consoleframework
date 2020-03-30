namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Describes the kind of value that a GridLength object is holding.
  /// </summary>
  public enum GridUnitType
  {
    /// <summary>
    /// The size is determined by the size properties of the content object.
    /// </summary>
    Auto,

    /// <summary>
    /// The value is expressed as a pixel.
    /// </summary>
    Pixel,

    /// <summary>
    /// The value is expressed as a weighted proportion of available space.
    /// </summary>
    Star
  }
}

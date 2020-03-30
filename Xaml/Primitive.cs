namespace Xaml
{
  /// <summary>
  /// Available in XAML markup as "string", "int", "double",
  /// "float", "char", "bool" elements.
  /// </summary>
  internal class Primitive<T> : IFactory
  {
    public T Content { get; set; }

    public object GetObject()
    {
      return Content;
    }
  }
}

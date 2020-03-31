using System;
using System.Globalization;

namespace Binding.Converters
{
  /// <summary>
  /// Converter between String and Integer.
  /// </summary>
  public class StringToIntegerConverter : IBindingConverter
  {
    public Type FirstType => typeof(string);

    public Type SecondType => typeof(int);

    public ConversionResult Convert(object s)
    {
      try
      {
        if (s == null)
        {
          return new ConversionResult(false, "String is null");
        }

        var value = int.Parse((string) s);
        return new ConversionResult(value);
      }
      catch (FormatException e)
      {
        return new ConversionResult(false, "Incorrect number");
      }
    }

    public ConversionResult ConvertBack(Object integer)
    {
      return new ConversionResult(((int) integer).ToString(CultureInfo.InvariantCulture));
    }
  }
}

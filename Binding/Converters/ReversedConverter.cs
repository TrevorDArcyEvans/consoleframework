using System;

namespace Binding.Converters
{
  public class ReversedConverter : IBindingConverter
  {
    private readonly IBindingConverter _converter;

    public ReversedConverter(IBindingConverter converter)
    {
      this._converter = converter;
    }

    public Type FirstType
    {
      get { return _converter.SecondType; }
    }

    public Type SecondType
    {
      get { return _converter.FirstType; }
    }

    public ConversionResult Convert(object tFirst)
    {
      return _converter.ConvertBack(tFirst);
    }

    public ConversionResult ConvertBack(object tSecond)
    {
      return _converter.Convert(tSecond);
    }
  }
}

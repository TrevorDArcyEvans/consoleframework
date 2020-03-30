using System;
using Xaml;

namespace ConsoleFramework.Controls
{
  public class GridLengthTypeConverter : ITypeConverter
  {
    public bool CanConvertFrom(Type sourceType)
    {
      switch (Type.GetTypeCode(sourceType))
      {
        case TypeCode.Int16:
        case TypeCode.UInt16:
        case TypeCode.Int32:
        case TypeCode.UInt32:
        case TypeCode.Int64:
        case TypeCode.UInt64:
        case TypeCode.Single:
        case TypeCode.Double:
        case TypeCode.Decimal:
        case TypeCode.String:
          return true;
      }

      return false;
    }

    public bool CanConvertTo(Type destinationType)
    {
      return destinationType == typeof(string);
    }

    public object ConvertFrom(object value)
    {
      if (value is string)
      {
        var s = (string) value;
        if (s == "Auto")
        {
          return new GridLength(GridUnitType.Auto, 0);
        }
        else if (s.EndsWith("*"))
        {
          if (s == "*")
            return new GridLength(GridUnitType.Star, 1);

          int num = Int32.Parse(s.Substring(0, s.Length - 1));
          return new GridLength(GridUnitType.Star, num);
        }
        else
        {
          return new GridLength(GridUnitType.Pixel, Int32.Parse(s));
        }
      }
      else
      {
        var num = Convert.ToInt32(value);
        return new GridLength(GridUnitType.Pixel, num);
      }
    }

    public object ConvertTo(object value, Type destinationType)
    {
      if (destinationType == typeof(string))
      {
        var gl = (GridLength) value;
        switch (gl.GridUnitType)
        {
          case GridUnitType.Auto:
            return "Auto";

          case GridUnitType.Star:
            if (gl.Value == 1)
            {
              return "*";
            }

            return (Convert.ToString(gl.Value) + "*");
        }

        return Convert.ToString(gl.Value);
      }

      throw new NotSupportedException();
    }
  }
}

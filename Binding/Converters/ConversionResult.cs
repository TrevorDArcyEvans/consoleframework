using System;

namespace Binding.Converters
{
  /// <summary>
  /// Represents value conversion result.
  /// </summary>
  public class ConversionResult
  {
    private readonly object _value;

    public object Value
    {
      get { return _value; }
    }

    private readonly bool _success;

    public bool Success
    {
      get { return _success; }
    }

    private readonly string _failReason;

    public string FailReason
    {
      get { return _failReason; }
    }

    public ConversionResult(object value)
    {
      this._value = value;
      this._success = true;
    }

    public ConversionResult(bool success, String failReason)
    {
      this._success = success;
      this._failReason = failReason;
    }
  }
}

using System;

namespace Binding.Validators
{
  /// <summary>
  /// Represents the result of data binding validation.
  /// </summary>
  public class ValidationResult
  {
    private readonly bool _valid;

    public bool Valid
    {
      get { return _valid; }
    }

    private readonly string _message;

    public string Message
    {
      get { return _message; }
    }

    public ValidationResult(bool valid)
    {
      this._valid = valid;
    }

    public ValidationResult(bool valid, String message)
    {
      this._valid = valid;
      this._message = message;
    }
  }
}

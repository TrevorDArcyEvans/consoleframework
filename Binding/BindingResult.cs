using System;

namespace Binding
{
  /// <summary>
  /// Represents result of one synchronization operation from Target to Source.
  /// If hasConversionError is true, message will represent conversion error message.
  /// If hasValidationError is true, message will represent validation error message.
  /// Both hasConversionError and hasValidationError cannot be set to true.
  /// </summary>
  public class BindingResult
  {
    public bool HasError;
    public bool HasConversionError;
    public bool HasValidationError;
    public string Message;

    public BindingResult(bool hasError)
    {
      this.HasError = hasError;
    }

    public BindingResult(bool hasConversionError, bool hasValidationError, String message)
    {
      this.HasConversionError = hasConversionError;
      this.HasValidationError = hasValidationError;
      this.HasError = hasConversionError || hasValidationError;
      this.Message = message;
    }
  }
}

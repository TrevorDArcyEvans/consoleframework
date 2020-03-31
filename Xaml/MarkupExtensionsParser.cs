using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Xaml
{
  public class MarkupExtensionsParser
  {
    private readonly IMarkupExtensionsResolver resolver;

    public MarkupExtensionsParser(IMarkupExtensionsResolver resolver, string text)
    {
      this.resolver = resolver;
      this._text = text;
    }

    private string _text;
    private int _index;

    private bool HasNextChar()
    {
      return _index < _text.Length;
    }

    private char ConsumeChar()
    {
      return _text[_index++];
    }

    private char PeekNextChar()
    {
      return _text[_index];
    }

    public Object ProcessMarkupExtension(IMarkupExtensionContext context)
    {
      // interpret as markup extension expression
      var result = ProcessMarkupExtensionCore(context);
      if (result is IFixupToken)
      {
        return result;
      }

      if (HasNextChar())
      {
        throw new InvalidOperationException(String.Format("Syntax error: unexpected characters at {0}", _index));
      }

      return result;
    }

    /// <summary>
    /// Consumes all whitespace characters. If necessary is true, at least one
    /// whitespace character should be consumed.
    /// </summary>
    private void ProcessWhitespace(bool necessary = true)
    {
      if (necessary)
      {
        // at least one whitespace should be
        if (PeekNextChar() != ' ')
        {
          throw new InvalidOperationException(String.Format("Syntax error: whitespace expected at {0}.", _index));
        }
      }

      while (PeekNextChar() == ' ') ConsumeChar();
    }

    /// <summary>
    /// Recursive method. Consumes next characters as markup extension definition.
    /// Resolves type, ctor arguments and properties of markup extension,
    /// constructs and initializes it, and returns ProvideValue method result.
    /// </summary>
    /// <param name="context">Context object passed to ProvideValue method.</param>
    private Object ProcessMarkupExtensionCore(IMarkupExtensionContext context)
    {
      if (ConsumeChar() != '{')
      {
        throw new InvalidOperationException("Syntax error: '{{' token expected at 0.");
      }

      ProcessWhitespace(false);
      var markupExtensionName = ProcessQualifiedName();
      if (markupExtensionName.Length == 0)
      {
        throw new InvalidOperationException("Syntax error: markup extension name is empty.");
      }

      ProcessWhitespace();

      var type = resolver.Resolve(markupExtensionName);

      object obj = null;
      var ctorArgs = new List<object>();

      for (;;)
      {
        if (PeekNextChar() == '{')
        {
          // inner markup extension processing

          // syntax error if ctor arg defined after any property
          if (obj != null)
          {
            throw new InvalidOperationException("Syntax error: constructor argument cannot be after property assignment.");
          }

          var value = ProcessMarkupExtensionCore(context);
          if (value is IFixupToken)
          {
            return value;
          }

          ctorArgs.Add(value);
        }
        else
        {
          var membernameOrString = ProcessString();

          if (membernameOrString.Length == 0)
          {
            throw new InvalidOperationException($"Syntax error: member name or string expected at {_index}");
          }

          if (PeekNextChar() == '=')
          {
            ConsumeChar();
            var value = PeekNextChar() == '{'
              ? ProcessMarkupExtensionCore(context)
              : ProcessString();

            if (value is IFixupToken)
            {
              return value;
            }

            // construct object if not constructed yet
            if (obj == null)
            {
              obj = Construct(type, ctorArgs);
            }

            // assign value to specified member
            AssignProperty(type, obj, membernameOrString, value);
          }
          else if (PeekNextChar() == ',' || PeekNextChar() == '}')
          {
            // syntax error if ctor arg defined after any property
            if (obj != null)
            {
              throw new InvalidOperationException("Syntax error: constructor argument cannot be after property assignment.");
            }

            // store membernameOrString as string argument of ctor
            ctorArgs.Add(membernameOrString);
          }
          else
          {
            // it is '{' token, throw syntax error
            throw new InvalidOperationException($"Syntax error : unexpected '{{' token at {_index}.");
          }
        }

        // after ctor arg or property assignment should be , or }
        if (PeekNextChar() == ',')
        {
          ConsumeChar();
        }
        else if (PeekNextChar() == '}')
        {
          ConsumeChar();

          // construct object
          if (obj == null)
          {
            obj = Construct(type, ctorArgs);
          }

          // markup extension is finished
          break;
        }
        else
        {
          // it is '{' token (without whitespace), throw syntax error
          throw new InvalidOperationException($"Syntax error : unexpected '{{' token at {_index}.");
        }

        ProcessWhitespace(false);
      }

      return ((IMarkupExtension) obj).ProvideValue(context);
    }

    private static void AssignProperty(Type type, Object obj, string propertyName, object value)
    {
      var property = type.GetProperty(propertyName);
      property.SetValue(obj, value, null);
    }

    /// <summary>
    /// Constructs object of specified type using specified ctor arguments list.
    /// </summary>
    private static object Construct(Type type, List<Object> ctorArgs)
    {
      var constructors = type.GetConstructors();
      var constructorInfos = constructors.Where(info => info.GetParameters().Length == ctorArgs.Count).ToList();
      if (constructorInfos.Count == 0)
      {
        throw new InvalidOperationException("No suitable constructor");
      }

      if (constructorInfos.Count > 1)
      {
        throw new InvalidOperationException("Ambiguous constructor call");
      }

      var ctor = constructorInfos[0];
      var parameters = ctor.GetParameters();
      var convertedArgs = new object[ctorArgs.Count];
      for (var i = 0; i < parameters.Length; i++)
      {
        convertedArgs[i] = ctorArgs[i];
      }

      return ctor.Invoke(convertedArgs);
    }

    /// <summary>
    /// Возвращает строку, в которой могут содержаться любые символы кроме {},=.
    /// Как только встречается один из этих символов без экранирования обратным слешем,
    /// парсинг прекращается.
    /// </summary>
    private string ProcessString()
    {
      var sb = new StringBuilder();
      var escaping = false;
      for (;;)
      {
        if (!HasNextChar())
        {
          if (escaping)
          {
            throw new InvalidOperationException("Invalid syntax.");
          }

          break;
        }

        var c = PeekNextChar();
        if (escaping)
        {
          sb.Append(c);
          ConsumeChar();
          escaping = false;
        }
        else
        {
          if (c == '\\')
          {
            escaping = true;
            ConsumeChar();
          }
          else
          {
            if (c == '{' || c == '}' || c == ',' || c == '=')
            {
              // break without consuming it
              break;
            }
            else
            {
              sb.Append(c);
              ConsumeChar();
            }
          }
        }
      }

      return sb.ToString();
    }

    private string ProcessQualifiedName()
    {
      var sb = new StringBuilder();
      for (;;)
      {
        var c = PeekNextChar();
        if (c != ':' && !Char.IsLetterOrDigit(c))
        {
          break;
        }

        ConsumeChar();
        sb.Append(c);
      }

      return sb.ToString();
    }
  }
}

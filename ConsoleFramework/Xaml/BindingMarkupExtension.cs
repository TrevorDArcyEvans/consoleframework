using System;
using System.Reflection;
using System.ComponentModel;
using Binding;
using Binding.Converters;
using Xaml;

namespace ConsoleFramework.Xaml
{
  [MarkupExtension("Binding")]
  internal class BindingMarkupExtension : IMarkupExtension
  {
    public BindingMarkupExtension()
    {
    }

    public BindingMarkupExtension(string path)
    {
      Path = path;
    }

    public string Path { get; set; }

    public string Mode { get; set; }

    public object Source { get; set; }

    /// <summary>
    /// Converter to be used.
    /// </summary>
    public IBindingConverter Converter { get; set; }

    public object ProvideValue(IMarkupExtensionContext context)
    {
      var realSource = Source ?? context.DataContext;
      if (null != realSource && !(realSource is INotifyPropertyChanged))
      {
        throw new ArgumentException("Source must be INotifyPropertyChanged to use bindings");
      }

      if (null != realSource)
      {
        var mode = BindingMode.Default;
        if (Path != null)
        {
          var enumType = typeof(BindingMode);
          var enumNames = enumType.GetTypeInfo().GetEnumNames();
          for (int i = 0, len = enumNames.Length; i < len; i++)
          {
            if (enumNames[i] == Mode)
            {
              mode = (BindingMode) Enum.ToObject(enumType, enumType.GetTypeInfo().GetEnumValues().GetValue(i));
              break;
            }
          }
        }

        var binding = new BindingBase(context.Object, context.PropertyName, (INotifyPropertyChanged) realSource, Path, mode);
        if (Converter != null)
        {
          binding.Converter = Converter;
        }

        binding.Bind();
        // mb return actual property value ?
        return null;
      }

      return null;
    }
  }
}

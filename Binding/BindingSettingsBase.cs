using System;
using System.Collections.Generic;
using Binding.Adapters;
using Binding.Converters;

namespace Binding
{
  /// <summary>
  /// Contains converters, validators and adapters.
  /// </summary>
  public class BindingSettingsBase
  {
    public static BindingSettingsBase DEFAULT_SETTINGS;

    static BindingSettingsBase()
    {
      DEFAULT_SETTINGS = new BindingSettingsBase();
      DEFAULT_SETTINGS.InitializeDefault();
    }

    private readonly Dictionary<Type, Dictionary<Type, IBindingConverter>> _converters = new Dictionary<Type, Dictionary<Type, IBindingConverter>>();
    private readonly Dictionary<Type, IBindingAdapter> _adapters = new Dictionary<Type, IBindingAdapter>();

    public BindingSettingsBase()
    {
    }

    /// <summary>
    /// Adds default set of converters and ui adapters.
    /// </summary>
    public void InitializeDefault()
    {
      AddConverter(new StringToIntegerConverter());
    }

    public void AddAdapter(IBindingAdapter adapter)
    {
      var targetClazz = adapter.TargetType;
      if (_adapters.ContainsKey(targetClazz))
      {
        throw new Exception(String.Format("Adapter for class {0} is already registered.", targetClazz.Name));
      }

      _adapters.Add(targetClazz, adapter);
    }

    public IBindingAdapter GetAdapterFor(Type clazz)
    {
      var adapter = _adapters[clazz];
      if (null == adapter)
      {
        throw new Exception(String.Format("Adapter for class {0} not found.", clazz.Name));
      }

      return adapter;
    }

    public void AddConverter(IBindingConverter converter)
    {
      RegisterConverter(converter);
      RegisterConverter(new ReversedConverter(converter));
    }

    private void RegisterConverter(IBindingConverter converter)
    {
      var first = converter.FirstType;
      var second = converter.SecondType;
      if (_converters.ContainsKey(first))
      {
        var firstClassConverters = _converters[first];
        if (firstClassConverters.ContainsKey(second))
        {
          throw new Exception(String.Format("Converter for {0} -> {1} classes is already registered.", first.Name, second.Name));
        }

        firstClassConverters.Add(second, converter);
      }
      else
      {
        var firstClassConverters = new Dictionary<Type, IBindingConverter>();
        firstClassConverters.Add(second, converter);
        _converters.Add(first, firstClassConverters);
      }
    }

    public IBindingConverter GetConverterFor(Type first, Type second)
    {
      if (!_converters.ContainsKey(first))
      {
        return null;
      }

      var firstClassConverters = _converters[first];
      if (!firstClassConverters.ContainsKey(second))
      {
        return null;
      }

      return firstClassConverters[second];
    }
  }
}

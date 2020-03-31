using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Binding.Adapters;
using Binding.Converters;
using Binding.Observables;
using Binding.Validators;
using ListChangedEventArgs = Binding.Observables.ListChangedEventArgs;

namespace Binding
{
  /// <summary>
  /// Handler of binding operation when data is transferred from Target to Source.
  /// </summary>
  public delegate void OnBindingHandler(BindingResult result);

  /// <summary>
  /// Provides data sync connection between two objects - source and target. Both source and target can be just objects,
  /// but if you want to bind to object that does not implement <see cref="INotifyPropertyChanged"/>,
  /// you should use it as target and use appropriate adapter (<see cref="IBindingAdapter"/> implementation). One Binding instance connects
  /// one source property and one target property.
  /// </summary>
  public class BindingBase
  {
    private object _target;
    private readonly string _targetProperty;
    private INotifyPropertyChanged _source;
    private readonly string _sourceProperty;
    private bool _bound;
    private readonly BindingMode _mode;
    private BindingMode _realMode;
    private readonly BindingSettingsBase _settings;

    // This may be initialized using true in inherited classes for specialized binding
    private bool _needAdapterAnyway = false;

    private IBindingAdapter _adapter;
    private PropertyInfo _targetPropertyInfo;
    private PropertyInfo _sourcePropertyInfo;

    // Converts target to source and back
    private IBindingConverter _converter;

    // Used instead targetListener if target does not implement INotifyPropertyChanged
    private object _targetListenerWrapper;

    // Flags used to avoid infinite recursive loop
    private bool _ignoreSourceListener;
    private bool _ignoreTargetListener;

    // Collections synchronization support
    private bool _sourceIsObservable;
    private IList _targetList;

    private bool _targetIsObservable;
    private IList _sourceList;

    private bool _updateSourceIfBindingFails = true;

    /// <summary>
    /// If target value conversion or validation fails, the source property will be set to null
    /// if this flag is set to true. Otherwise the source property setter won't be called.
    /// Default value is true
    /// </summary>
    public bool UpdateSourceIfBindingFails
    {
      get { return _updateSourceIfBindingFails; }
      set { _updateSourceIfBindingFails = value; }
    }

    /// <summary>
    /// Event will be invoked when data goes from Target to Source.
    /// </summary>
    public event OnBindingHandler OnBinding;

    private IBindingValidator _validator;

    /// <summary>
    /// Validator triggered when data flows from Target to Source.
    /// </summary>
    public IBindingValidator Validator
    {
      get { return _validator; }
      set
      {
        if (_bound)
        {
          throw new InvalidOperationException("Cannot change validator when binding is active.");
        }

        _validator = value;
      }
    }

    /// <summary>
    /// BindingAdapter used as bridge to Target if Target doesn't
    /// implement INotifyPropertyChanged.
    /// </summary>
    public IBindingAdapter Adapter
    {
      get { return _adapter; }
      set
      {
        if (_bound)
        {
          throw new InvalidOperationException("Cannot change adapter when binding is active.");
        }

        _adapter = value;
      }
    }

    /// <summary>
    /// Converter used for values conversion between Source and Target.
    /// </summary>
    public IBindingConverter Converter
    {
      get { return _converter; }
      set
      {
        if (_bound) throw new InvalidOperationException("Cannot change converter when binding is active.");
        _converter = value;
      }
    }

    public BindingBase(object target, string targetProperty, INotifyPropertyChanged source, string sourceProperty) :
      this(target, targetProperty, source, sourceProperty, BindingMode.Default)
    {
    }

    public BindingBase(object target, string targetProperty, INotifyPropertyChanged source, string sourceProperty, BindingMode mode) :
      this(target, targetProperty, source, sourceProperty, mode, BindingSettingsBase.DEFAULT_SETTINGS)
    {
    }

    public BindingBase(object target, string targetProperty, INotifyPropertyChanged source, string sourceProperty, BindingMode mode, BindingSettingsBase settings)
    {
      if (null == target)
      {
        throw new ArgumentNullException("target");
      }

      if (string.IsNullOrEmpty(targetProperty))
      {
        throw new ArgumentException("targetProperty is null or empty");
      }

      if (null == source)
      {
        throw new ArgumentNullException("source");
      }

      if (string.IsNullOrEmpty(sourceProperty))
      {
        throw new ArgumentException("sourceProperty is null or empty");
      }

      this._target = target;
      this._targetProperty = targetProperty;
      this._source = source;
      this._sourceProperty = sourceProperty;
      this._mode = mode;
      this._bound = false;
      this._settings = settings;
    }

    /// <summary>
    /// Forces a data transfer from the binding source property to the binding target property.
    /// </summary>
    public void UpdateTarget()
    {
      if (_realMode != BindingMode.OneTime && _realMode != BindingMode.OneWay && _realMode != BindingMode.TwoWay)
      {
        throw new Exception(String.Format("Cannot update target in {0} binding mode.", _realMode));
      }

      _ignoreTargetListener = true;
      try
      {
        Object sourceValue = _sourcePropertyInfo.GetGetMethod().Invoke(
          _source, null);
        if (_sourceIsObservable)
        {
          // work with observable list
          // We should take target list and initialize it using source items
          IList targetListNow;
          if (_adapter == null)
          {
            targetListNow = (IList) _targetPropertyInfo.GetGetMethod().Invoke(_target, null);
          }
          else
          {
            targetListNow = (IList) _adapter.GetValue(_target, _targetProperty);
          }

          if (sourceValue == null)
          {
            if (null != targetListNow) targetListNow.Clear();
          }
          else
          {
            if (null != targetListNow)
            {
              targetListNow.Clear();
              foreach (Object x in ((IEnumerable) sourceValue))
              {
                targetListNow.Add(x);
              }

              // Subscribe
              if (_sourceList != null)
              {
                ((IObservableList) _sourceList).ListChanged -= SourceListChanged;
              }

              _sourceList = (IList) sourceValue;
              _targetList = targetListNow;
              ((IObservableList) _sourceList).ListChanged += SourceListChanged;
            }
            else
            {
              // Nothing to do : target list is null, ignoring sync operation
            }
          }
        }
        else
        {
          // Work with usual property
          var converted = sourceValue;
          // Convert back if need
          if (null != _converter)
          {
            var result = _converter.ConvertBack(sourceValue);
            if (!result.Success)
            {
              return;
            }

            converted = result.Value;
          }

          //
          if (_adapter == null)
          {
            _targetPropertyInfo.GetSetMethod().Invoke(_target, new object[] { converted });
          }
          else
          {
            _adapter.SetValue(_target, _targetProperty, converted);
          }
        }
      }
      finally
      {
        _ignoreTargetListener = false;
      }
    }

    /// <summary>
    /// Synchronizes changes of srcList, applying them to destList.
    /// Changes are described in args.
    /// </summary>
    public static void ApplyChanges(IList destList, IList srcList, ListChangedEventArgs args)
    {
      switch (args.Type)
      {
        case ListChangedEventType.ItemsInserted:
        {
          for (var i = 0; i < args.Count; i++)
          {
            destList.Insert(args.Index + i, srcList[args.Index + i]);
          }

          break;
        }

        case ListChangedEventType.ItemsRemoved:
          for (int i = 0; i < args.Count; i++)
          {
            destList.RemoveAt(args.Index);
          }

          break;

        case ListChangedEventType.ItemReplaced:
        {
          destList[args.Index] = srcList[args.Index];
          break;
        }
      }
    }

    private void SourceListChanged(object sender, ListChangedEventArgs args)
    {
      // To avoid side effects from old listeners
      // (can be reproduced if call raisePropertyChanged inside another ObservableList handler)
      // propertyChanged will cause re-subscription to ListChanged, but
      // old list still can call ListChanged when enumerates event handlers
      if (!ReferenceEquals(sender, _sourceList))
      {
        return;
      }

      _ignoreTargetListener = true;
      try
      {
        ApplyChanges(_targetList, _sourceList, args);
      }
      finally
      {
        _ignoreTargetListener = false;
      }
    }

    private void TargetListChanged(object sender, ListChangedEventArgs args)
    {
      // To avoid side effects from old listeners
      // (can be reproduced if call raisePropertyChanged inside another ObservableList handler)
      // propertyChanged will cause re-subscription to ListChanged, but
      // old list still can call ListChanged when enumerates event handlers
      if (!ReferenceEquals(sender, _targetList))
      {
        return;
      }

      _ignoreSourceListener = true;
      try
      {
        ApplyChanges(_sourceList, _targetList, args);
      }
      finally
      {
        _ignoreSourceListener = false;
      }
    }

    /// <summary>
    /// Sends the current binding target value to the binding source property in TwoWay or OneWayToSource bindings.
    /// </summary>
    public void UpdateSource()
    {
      if (_realMode != BindingMode.OneWayToSource && _realMode != BindingMode.TwoWay)
      {
        throw new Exception(String.Format("Cannot update source in {0} binding mode.", _realMode));
      }

      _ignoreSourceListener = true;
      try
      {
        object targetValue;
        if (null == _adapter)
        {
          targetValue = _targetPropertyInfo.GetGetMethod().Invoke(_target, null);
        }
        else
        {
          targetValue = _adapter.GetValue(_target, _targetProperty);
        }

        //
        if (_targetIsObservable)
        {
          // Work with collection
          var sourceListNow = (IList) _sourcePropertyInfo.GetGetMethod().Invoke(_source, null);
          if (targetValue == null)
          {
            if (null != sourceListNow) sourceListNow.Clear();
          }
          else
          {
            if (null != sourceListNow)
            {
              sourceListNow.Clear();
              foreach (object item in (IEnumerable) targetValue)
              {
                sourceListNow.Add(item);
              }

              // Subscribe
              if (_targetList != null)
              {
                ((IObservableList) _targetList).ListChanged -= TargetListChanged;
              }

              _targetList = (IList) targetValue;
              _sourceList = sourceListNow;
              ((IObservableList) _targetList).ListChanged += TargetListChanged;
            }
            else
            {
              // Nothing to do : source list is null, ignoring sync operation
            }
          }
        }
        else
        {
          // Work with usual property
          var convertedValue = targetValue;
          // Convert if need
          if (null != _converter)
          {
            var result = _converter.Convert(targetValue);
            if (!result.Success)
            {
              if (null != OnBinding)
              {
                OnBinding.Invoke(new BindingResult(true, false, result.FailReason));
              }

              if (_updateSourceIfBindingFails)
              {
                // Will update source using null or default(T) if T is primitive
                _sourcePropertyInfo.GetSetMethod().Invoke(_source, new object[] { null });
              }

              return;
            }

            convertedValue = result.Value;
          }

          // Validate if need
          if (null != Validator)
          {
            var validationResult = Validator.Validate(convertedValue);
            if (!validationResult.Valid)
            {
              if (null != OnBinding)
              {
                OnBinding.Invoke(new BindingResult(false, true, validationResult.Message));
              }

              if (_updateSourceIfBindingFails)
              {
                // Will update source using null or default(T) if T is primitive
                _sourcePropertyInfo.GetSetMethod().Invoke(_source, new object[] { null });
              }

              return;
            }
          }

          _sourcePropertyInfo.GetSetMethod().Invoke(_source, new object[] { convertedValue });
          if (null != OnBinding)
          {
            OnBinding.Invoke(new BindingResult(false));
          }

          //
        }
      }
      finally
      {
        _ignoreSourceListener = false;
      }
    }

    /// <summary>
    /// Connects Source and Target objects.
    /// </summary>
    public void Bind()
    {
      // Resolve binding mode and search converter if need
      if (_needAdapterAnyway)
      {
        if (_adapter == null)
        {
          _adapter = _settings.GetAdapterFor(_target.GetType());
        }

        _realMode = _mode == BindingMode.Default ? _adapter.DefaultMode : _mode;
      }
      else
      {
        _realMode = _mode == BindingMode.Default ? BindingMode.TwoWay : _mode;
        if (_realMode == BindingMode.TwoWay || _realMode == BindingMode.OneWayToSource)
        {
          if (!(_target is INotifyPropertyChanged))
          {
            if (_adapter == null)
            {
              _adapter = _settings.GetAdapterFor(_target.GetType());
            }
          }
        }
      }

      // Get properties info and check if they are collections
      _sourcePropertyInfo = _source.GetType().GetProperty(_sourceProperty);
      if (null == _adapter)
      {
        _targetPropertyInfo = _target.GetType().GetProperty(_targetProperty);
      }

      var targetPropertyClass = (null == _adapter) ? _targetPropertyInfo.PropertyType : _adapter.GetTargetPropertyClazz(_targetProperty);

      _sourceIsObservable = typeof(IObservableList).IsAssignableFrom(_sourcePropertyInfo.PropertyType);
      _targetIsObservable = typeof(IObservableList).IsAssignableFrom(targetPropertyClass);

      // We need converter if data will flow from non-observable property to property of another class
      if (targetPropertyClass != _sourcePropertyInfo.PropertyType)
      {
        var needConverter = false;
        if (_realMode == BindingMode.OneTime || _realMode == BindingMode.OneWay || _realMode == BindingMode.TwoWay)
        {
          if (!_sourceIsObservable)
          {
            needConverter |= !targetPropertyClass.IsAssignableFrom(_sourcePropertyInfo.PropertyType);
          }
        }

        if (_realMode == BindingMode.OneWayToSource || _realMode == BindingMode.TwoWay)
        {
          if (!_targetIsObservable)
          {
            needConverter |= !_sourcePropertyInfo.PropertyType.IsAssignableFrom(targetPropertyClass);
          }
        }

        //
        if (needConverter)
        {
          if (_converter == null)
          {
            _converter = _settings.GetConverterFor(targetPropertyClass, _sourcePropertyInfo.PropertyType);
          }
          else
          {
            // Check if converter must be reversed
            if (_converter.FirstType.IsAssignableFrom(targetPropertyClass) &&
                _converter.SecondType.IsAssignableFrom(_sourcePropertyInfo.PropertyType))
            {
              // Nothing to do, it's ok
            }
            else if (_converter.SecondType.IsAssignableFrom(targetPropertyClass) &&
                     _converter.FirstType.IsAssignableFrom(_sourcePropertyInfo.PropertyType))
            {
              // Should be reversed
              _converter = new ReversedConverter(_converter);
            }
            else
            {
              throw new Exception("Provided converter doesn't support conversion between specified properties.");
            }
          }

          if (_converter == null)
          {
            throw new Exception($"Converter for {targetPropertyClass.Name} -> {_sourcePropertyInfo.PropertyType.Name} classes not found.");
          }
        }
      }

      // Verify properties getters and setters for specified binding mode
      if (_realMode == BindingMode.OneTime || _realMode == BindingMode.OneWay || _realMode == BindingMode.TwoWay)
      {
        if (_sourcePropertyInfo.GetGetMethod() == null)
        {
          throw new Exception("Source property getter not found");
        }

        if (_sourceIsObservable)
        {
          if (null == _adapter && _targetPropertyInfo.GetGetMethod() == null)
          {
            throw new Exception("Target property getter not found");
          }

          if (!typeof(IList).IsAssignableFrom(targetPropertyClass))
          {
            throw new Exception("Target property class have to implement IList");
          }
        }
        else
        {
          if (null == _adapter && _targetPropertyInfo.GetSetMethod() == null)
          {
            throw new Exception("Target property setter not found");
          }
        }
      }

      if (_realMode == BindingMode.OneWayToSource || _realMode == BindingMode.TwoWay)
      {
        if (null == _adapter && _targetPropertyInfo.GetGetMethod() == null)
        {
          throw new Exception("Target property getter not found");
        }

        if (_targetIsObservable)
        {
          if (_sourcePropertyInfo.GetGetMethod() == null)
          {
            throw new Exception("Source property getter not found");
          }

          if (!typeof(IList).IsAssignableFrom(_sourcePropertyInfo.PropertyType))
          {
            throw new Exception("Source property class have to implement IList");
          }
        }
        else
        {
          if (_sourcePropertyInfo.GetSetMethod() == null)
          {
            throw new Exception("Source property setter not found");
          }
        }
      }

      // Subscribe to listeners
      ConnectSourceAndTarget();

      // Initial flush values
      if (_realMode == BindingMode.OneTime || _realMode == BindingMode.OneWay || _realMode == BindingMode.TwoWay)
      {
        UpdateTarget();
      }

      if (_realMode == BindingMode.OneWayToSource || _realMode == BindingMode.TwoWay)
      {
        UpdateSource();
      }

      this._bound = true;
    }

    protected void ConnectSourceAndTarget()
    {
      switch (_realMode)
      {
        case BindingMode.OneTime:
          break;

        case BindingMode.OneWay:
          _source.PropertyChanged += SourceListener;
          break;

        case BindingMode.OneWayToSource:
          if (null == _adapter)
          {
            ((INotifyPropertyChanged) _target).PropertyChanged += TargetListener;
          }
          else
          {
            _targetListenerWrapper = _adapter.AddPropertyChangedListener(_target, TargetListener);
          }

          break;

        case BindingMode.TwoWay:
          _source.PropertyChanged += SourceListener;
          //
          if (null == _adapter)
          {
            ((INotifyPropertyChanged) _target).PropertyChanged += TargetListener;
          }
          else
          {
            _targetListenerWrapper = _adapter.AddPropertyChangedListener(_target, TargetListener);
          }

          break;
      }
    }

    private void TargetListener(object sender, PropertyChangedEventArgs args)
    {
      if (!_ignoreTargetListener && args.PropertyName == _targetProperty)
      {
        UpdateSource();
      }
    }

    private void SourceListener(object sender, PropertyChangedEventArgs args)
    {
      if (!_ignoreSourceListener && args.PropertyName == _sourceProperty)
      {
        UpdateTarget();
      }
    }

    /// <summary>
    /// Disconnects Source and Target objects.
    /// </summary>
    public void Unbind()
    {
      if (!this._bound)
      {
        return;
      }

      DisconnectSourceAndTarget();

      this._sourcePropertyInfo = null;
      this._targetPropertyInfo = null;

      this._bound = false;
    }

    protected void DisconnectSourceAndTarget()
    {
      if (_realMode == BindingMode.OneWay || _realMode == BindingMode.TwoWay)
      {
        // Remove source listener
        _source.PropertyChanged -= SourceListener;
      }

      if (_realMode == BindingMode.OneWayToSource || _realMode == BindingMode.TwoWay)
      {
        // Remove target listener
        if (_adapter == null)
        {
          ((INotifyPropertyChanged) _target).PropertyChanged -= TargetListener;
        }
        else
        {
          _adapter.RemovePropertyChangedListener(_target, _targetListenerWrapper);
          _targetListenerWrapper = null;
        }
      }

      if (_sourceList != null && _sourceIsObservable)
      {
        ((IObservableList) _sourceList).ListChanged -= SourceListChanged;
        _sourceList = null;
      }

      if (_targetList != null && _targetIsObservable)
      {
        ((IObservableList) _targetList).ListChanged -= TargetListChanged;
        _targetList = null;
      }
    }

    /// <summary>
    /// Changes the binding Source object. If current binding state is bound,
    /// the <see cref="Unbind"/> and <see cref="Bind"/> methods will be called automatically.
    /// <param name="source">New Source object</param>
    /// </summary>
    public void SetSource(INotifyPropertyChanged source)
    {
      if (null == source)
      {
        throw new ArgumentNullException("source");
      }

      if (_bound)
      {
        Unbind();
        this._source = source;
        Bind();
      }
      else
      {
        this._source = source;
      }
    }

    /// <summary>
    /// Changes the binding Target object. If current binding state is bound,
    /// the <see cref="Unbind"/> and <see cref="Bind"/> methods will be called automatically.
    /// @param target New Target object
    /// </summary>
    public void SetTarget(Object target)
    {
      if (null == target)
      {
        throw new ArgumentNullException("target");
      }

      if (_bound)
      {
        Unbind();
        this._target = target;
        Bind();
      }
      else
      {
        this._target = target;
      }
    }
  }
}

using System;

namespace ConsoleFramework.Events
{
  /// <summary>
  /// Represents event that supports routing through visual tree.
  /// </summary>
  public sealed class RoutedEvent
  {
    public RoutedEvent(Type handlerType, string name, Type ownerType, RoutingStrategy routingStrategy)
    {
      this._handlerType = handlerType;
      this._name = name;
      this._ownerType = ownerType;
      this._routingStrategy = routingStrategy;
    }

    private readonly Type _handlerType;

    /// <summary>
    /// Тип делегата - обработчика события.
    /// </summary>
    public Type HandlerType
    {
      get { return _handlerType; }
    }

    private readonly string _name;

    /// <summary>
    /// Имя события - должно быть уникальным в рамках указанного <see cref="OwnerType"/>.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    private readonly Type _ownerType;

    /// <summary>
    /// Тип владельца события.
    /// </summary>
    public Type OwnerType
    {
      get { return _ownerType; }
    }

    private readonly RoutingStrategy _routingStrategy;

    /// <summary>
    /// Стратегия маршрутизации события.
    /// </summary>
    public RoutingStrategy RoutingStrategy
    {
      get { return _routingStrategy; }
    }

    public RoutedEventKey Key
    {
      get
      {
        // note : mb cache this
        return new RoutedEventKey(_name, _ownerType);
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xaml
{
  /// <summary>
  /// Allows to construct an object of a specified type, with specified constructor
  /// arguments and properties values. In XAML it can be used with convenient alias "object".
  /// You can read documentation to find usage examples.
  /// </summary>
  [ContentProperty("ParametersAndProperties")]
  internal class ObjectFactory : IFactory
  {
    /// <summary>
    /// Full type name, with generics allowed like this:
    /// System.Collections.Generic.Dictionary`2[System.String,MyType]
    /// View Type.GetType(string name) method documentation to find syntax examples.
    /// </summary>
    public String TypeName { get; set; }

    private readonly Dictionary<String, Object> parametersAndProperties = new Dictionary<string, object>();

    public Dictionary<String, Object> ParametersAndProperties
    {
      get { return parametersAndProperties; }
    }

    private struct CtorArg
    {
      public int index;
      public Object obj;
    }

    public object GetObject()
    {
      if (string.IsNullOrEmpty(TypeName))
        throw new InvalidOperationException("TypeName is not specified.");

      Type type = Type.GetType(TypeName);
      if (null == type)
        throw new TypeLoadException(string.Format(
          "Type {0} not found. Try to use assembly-qualified type name.",
          TypeName));

      // Construct object accoring to passed ctor arguments
      List<CtorArg> ctorArgs = new List<CtorArg>();
      foreach (var pair in parametersAndProperties)
      {
        string name = pair.Key;
        int result;
        if (int.TryParse(name, out result))
        {
          ctorArgs.Add(new CtorArg()
          {
            index = result,
            obj = pair.Value
          });
        }
      }

      // Using OrderBy instead List<T>.Sort() because the last one is unstable
      // and can reorder elements with equal keys
      ctorArgs = ctorArgs.OrderBy(arg => arg.index).ToList();

      ConstructorInfo[] constructors = type.GetConstructors();
      ConstructorInfo ctorInfo = constructors.Single(ctor => ctor.GetParameters().Length == ctorArgs.Count);
      object createdObject = ctorInfo.Invoke(ctorArgs.Select(arg => arg.obj).ToArray());

      // Fill properties using XamlParser's default conversion rules
      foreach (var pair in parametersAndProperties)
      {
        string name = pair.Key;
        int result;
        if (!int.TryParse(name, out result))
        {
          PropertyInfo propertyInfo = type.GetProperty(name);
          object value = pair.Value;
          if (null != value)
          {
            object convertedValue = XamlParser.ConvertValueIfNeed(value.GetType(),
              propertyInfo.PropertyType,
              value);
            propertyInfo.SetValue(createdObject, convertedValue, null);
          }
        }
      }

      return createdObject;
    }
  }
}

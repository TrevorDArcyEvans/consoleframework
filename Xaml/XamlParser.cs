using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace Xaml
{
  /// <summary>
  /// Provides XAML parsing and simultaneous object graph creation.
  /// </summary>
  public partial class XamlParser
  {
    /// <summary>
    /// <para>Creates the object graph using provided xaml and dataContext.</para>
    /// <para>DataContext will be passed to markup extensions and can be null if you don't want to
    /// use binding markup extensions.</para>
    /// <para>Default namespaces are used to search types (by tag name) and
    /// markup extensions (all classes marked with MarkupExtensionAttribute are scanned).
    /// If don't specify default namespaces, you should specify namespaces (with prefixes)
    /// explicitly in XAML root element.</para>
    /// <para>Example of defaultNamespaces item:</para>
    /// <para><code>clr-namespace:TestProject1.Xaml.EnumsTest;assembly=TestProject1</code></para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xaml">Xaml markup</param>
    /// <param name="dataContext">Object that will be passed to markup extensions</param>
    /// <param name="defaultNamespaces">Namespaces can be used without explicit prefixes</param>
    /// <returns></returns>
    public static T CreateFromXaml<T>(string xaml, object dataContext, List<string> defaultNamespaces)
    {
      if (null == xaml)
      {
        throw new ArgumentNullException("xaml");
      }

      var parser = new XamlParser(defaultNamespaces);
      return (T) parser.CreateFromXaml(xaml, dataContext);
    }

    private XamlParser(List<string> defaultNamespaces)
    {
      if (null == defaultNamespaces)
      {
        throw new ArgumentNullException("defaultNamespaces");
      }

      this._defaultNamespaces = defaultNamespaces;
    }

    private readonly List<string> _defaultNamespaces;

    /// <summary>
    /// Если str начинается с одинарной открывающей фигурной скобки, то метод обрабатывает его
    /// как вызов расширения разметки, и возвращает результат, или выбрасывает исключение,
    /// если при парсинге или выполнении возникли ошибки. Если же str начинается c комбинации
    /// {}, то остаток строки возвращается просто строкой.
    /// </summary>
    private object ProcessText(string text, string currentProperty, object currentObject, object rootDataContext)
    {
      if (string.IsNullOrEmpty(text))
      {
        return string.Empty;
      }

      if (text[0] != '{')
      {
        // interpret whole text as string
        return text;
      }
      else if (text.Length > 1 && text[1] == '}')
      {
        // interpret the rest as string
        return text.Length > 2 ? text.Substring(2) : String.Empty;
      }
      else
      {
        var markupExtensionsParser = new MarkupExtensionsParser(new MarkupExtensionsResolver(this), text);
        var context = new MarkupExtensionContext(this, text, currentProperty, currentObject, FindClosestDataContext() ?? rootDataContext);
        var providedValue = markupExtensionsParser.ProcessMarkupExtension(context);
        if (providedValue is IFixupToken)
        {
          _fixupTokens.Add((FixupToken) providedValue);
          // Null means no value will be assigned to target property
          return null;
        }

        return providedValue;
      }
    }

    /// <summary>
    /// Tries to find the closest object in the stack with not null DataContext property.
    /// Returns first found data context object or null if no suitable objects found.
    /// </summary>
    private object FindClosestDataContext()
    {
      foreach (var objectInfo in _objects)
      {
        var type = objectInfo.obj.GetType();
        var dataContextProp = type.GetProperty(GetDataContextPropertyName(type));
        if (dataContextProp == null)
        {
          continue;
        }

        var dataContextValue = dataContextProp.GetValue(objectInfo.obj);
        if (dataContextValue != null)
        {
          return dataContextValue;
        }
      }

      return null;
    }

    private string GetDataContextPropertyName(Type type)
    {
      var attributes = type
        .GetTypeInfo()
        .GetCustomAttributes(typeof(DataContextPropertyAttribute), true)
        .ToArray();
      if (attributes.Length == 0)
      {
        // Default value
        return "DataContext";
      }

      if (attributes.Length > 1)
      {
        throw new InvalidOperationException("Ambiguous data context property definition - more than one DataContextPropertyAttribute found.");
      }

      return ((DataContextPropertyAttribute) attributes[0]).Name;
    }

    // { prefix -> namespace }
    private readonly Dictionary<String, String> _namespaces = new Dictionary<string, string>();

    private object _dataContext;

    /// <summary>
    /// Стек конфигурируемых объектов. На верху стека всегда лежит
    /// текущий конфигурируемый объект.
    /// </summary>
    private readonly Stack<ObjectInfo> _objects = new Stack<ObjectInfo>();

    /// <summary>
    /// Возвращает текущий конфигурируемый объект или null, если такового нет.
    /// </summary>
    private ObjectInfo Top
    {
      get { return _objects.Count > 0 ? _objects.Peek() : null; }
    }

    // Result object
    private object _result;

    /// <summary>
    /// Map { x:Id -> object } of fully configured objects available to reference from
    /// markup extensions.
    /// </summary>
    private readonly Dictionary<String, Object> _objectsById = new Dictionary<string, object>();

    /// <summary>
    /// List of fixup tokens used to defer objects by id resolving if markup extension
    /// has forward references to objects declared later.
    /// </summary>
    private readonly List<FixupToken> _fixupTokens = new List<FixupToken>();

    /// <summary>
    /// Creates the object graph using provided xaml.
    /// </summary>
    /// <param name="xaml"></param>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    private object CreateFromXaml(string xaml, object dataContext)
    {
      this._dataContext = dataContext;

      using (var xmlReader = XmlReader.Create(new StringReader(xaml)))
      {
        while (xmlReader.Read())
        {
          if (xmlReader.NodeType == XmlNodeType.Element)
          {
            var name = xmlReader.Name;

            // explicit property syntax
            if (Top != null && name.Contains("."))
            {
              // type may be qualified with xmlns namespace
              var typePrefix = name.Substring(0, name.IndexOf('.'));
              var type = ResolveType(typePrefix);
              if (type != Top.type)
              {
                throw new Exception($"Property {name} doesn't match current object {Top.type}");
              }

              if (Top.currentProperty != null)
              {
                throw new Exception("Illegal syntax in property value definition");
              }

              var propertyName = name.Substring(name.IndexOf('.') + 1);
              Top.currentProperty = propertyName;
            }
            else
            {
              var processingRootObject = (_objects.Count == 0);

              // Process namespace attributes if processing root object
              if (processingRootObject && xmlReader.HasAttributes)
              {
                if (xmlReader.HasAttributes)
                {
                  while (xmlReader.MoveToNextAttribute())
                  {
                    var attributePrefix = xmlReader.Prefix;
                    var attributeName = xmlReader.LocalName;
                    var attributeValue = xmlReader.Value;

                    // If we have found xmlns-attributes on root object, register them
                    // in namespaces dictionary
                    if (attributePrefix == "xmlns")
                    {
                      _namespaces.Add(attributeName, attributeValue);
                    }

                    //
                  }

                  xmlReader.MoveToElement();
                }
              }

              _objects.Push(CreateObject(name));

              // Process attributes
              if (xmlReader.HasAttributes)
              {
                while (xmlReader.MoveToNextAttribute())
                {
                  var attributePrefix = xmlReader.Prefix;
                  var attributeName = xmlReader.LocalName;
                  var attributeValue = xmlReader.Value;

                  // Skip xmls attributes of root object
                  if (attributePrefix == "xmlns" && processingRootObject)
                  {
                    continue;
                  }

                  ProcessAttribute(attributePrefix, attributeName, attributeValue);
                }

                xmlReader.MoveToElement();
              }

              if (xmlReader.IsEmptyElement)
              {
                ProcessEndElement();
              }
            }
          }

          if (xmlReader.NodeType == XmlNodeType.Text)
          {
            // this call moves xmlReader current element forward
            Top.currentPropertyText = xmlReader.ReadContentAsString();
          }

          if (xmlReader.NodeType == XmlNodeType.EndElement)
          {
            ProcessEndElement();
          }
        }
      }

      // После обработки всех элементов в последний раз обращаемся к
      // расширениям разметки, ожидающим свои forward-references
      ProcessFixupTokens();

      return _result;
    }

    /// <summary>
    /// Алиасы для объектов-примитивов, чтобы не писать в XAML длинные формулировки вида
    /// &lt;xaml:Primitive x:TypeArg1="{Type System.Double}"&gt;&lt;/xaml:Primitive&gt;
    /// </summary>
    private static readonly Dictionary<String, Type> _aliases = new Dictionary<string, Type>()
    {
      { "object", typeof(ObjectFactory) },
      { "string", typeof(Primitive<string>) },
      { "int", typeof(Primitive<int>) },
      { "double", typeof(Primitive<double>) },
      { "float", typeof(Primitive<float>) },
      { "char", typeof(Primitive<char>) },
      { "bool", typeof(Primitive<bool>) }
    };

    private ObjectInfo CreateObject(string name)
    {
      var type = _aliases.ContainsKey(name) ? _aliases[name] : ResolveType(name);

      var constructorInfo = type.GetConstructor(new Type[0]);
      if (null == constructorInfo)
      {
        throw new Exception(String.Format("Type {0} has no default constructor.", type.FullName));
      }

      var invoke = constructorInfo.Invoke(new object[0]);
      return new ObjectInfo()
      {
        obj = invoke,
        type = type
      };
    }

    private void ProcessAttribute(string attributePrefix, string attributeName, string attributeValue)
    {
      if (attributePrefix != string.Empty)
      {
        if (!_namespaces.ContainsKey(attributePrefix))
        {
          throw new InvalidOperationException(string.Format("Unknown prefix {0}", attributePrefix));
        }

        var namespaceUrl = _namespaces[attributePrefix];
        if (namespaceUrl == "http://consoleframework.org/xaml.xsd")
        {
          if (attributeName == "Key")
          {
            Top.key = attributeValue;
          }
          else if (attributeName == "Id")
          {
            Top.id = attributeValue;
          }
        }
      }
      else
      {
        // Process attribute as property assignment
        var propertyInfo = Top.type.GetProperty(attributeName);
        if (null == propertyInfo)
        {
          throw new InvalidOperationException(string.Format("Property {0} not found.", attributeName));
        }

        var value = ProcessText(attributeValue, attributeName, Top.obj, _dataContext);
        if (null != value)
        {
          var convertedValue = ConvertValueIfNeed(value.GetType(), propertyInfo.PropertyType, value);
          propertyInfo.SetValue(Top.obj, convertedValue, null);
        }
      }
    }

    private static string GetContentPropertyName(Type type)
    {
      var attributes = type.GetTypeInfo().GetCustomAttributes(typeof(ContentPropertyAttribute), true).ToArray();
      if (attributes.Length == 0)
      {
        return "Content";
      }

      if (attributes.Length > 1)
      {
        throw new InvalidOperationException("Ambiguous content property definition - more than one ContentPropertyAttribute found.");
      }

      return ((ContentPropertyAttribute) attributes[0]).Name;
    }

    /// <summary>
    /// Finishes configuring current object and assigns it to property of parent object.
    /// </summary>
    private void ProcessEndElement()
    {
      bool assignToParent;

      // closed element having text content
      if (Top.currentPropertyText != null)
      {
        var property = Top.currentProperty != null
          ? Top.type.GetProperty(Top.currentProperty)
          : Top.type.GetProperty(GetContentPropertyName(Top.type));
        var value = ProcessText(Top.currentPropertyText, Top.currentProperty, Top.obj, _dataContext);
        if (value != null)
        {
          var convertedValue = ConvertValueIfNeed(value.GetType(), property.PropertyType, value);
          property.SetValue(Top.obj, convertedValue, null);
        }

        if (Top.currentProperty != null)
        {
          Top.currentProperty = null;
          assignToParent = false;
        }
        else
        {
          // Для объектов, задаваемых текстом ( <MyObject>text</MyObject> )
          // currentProperty равно null, и при встрече закрывающего тега </MyObject>
          // мы должны не только присвоить Content-свойству значение text, но и
          // присвоить созданный объект свойству родительского объекта, таким образом эта
          // запись будет эквивалентна выражению
          // <MyObject><MyObject.Content>text</MyObject.Content></MyObject>
          assignToParent = true;
        }

        Top.currentPropertyText = null;
      }
      else
      {
        assignToParent = true;
      }

      if (!assignToParent)
      {
        return;
      }

      // closed element having sub-element content
      if (Top.currentProperty != null)
      {
        // был закрыт один из тегов-свойств, дочерний элемент
        // уже присвоен свойству, поэтому ничего делать не нужно, кроме
        // обнуления currentProperty
        Top.currentProperty = null;
      }
      else
      {
        // был закрыт основной тег текущего конструируемого объекта
        // нужно получить объект уровнем выше и присвоить себя свойству этого
        // объекта, либо добавить в свойство-коллекцию, если это коллекция
        var initialized = _objects.Pop();

        if (initialized.obj is IFactory)
        {
          initialized.obj = ((IFactory) initialized.obj).GetObject();
        }

        if (_objects.Count == 0)
        {
          _result = initialized.obj;
        }
        else
        {
          var propertyName = Top.currentProperty ?? GetContentPropertyName(Top.type);

          // If parent object property is ICollection<T>,
          // add current object into them as T (will conversion if need)
          var property = Top.type.GetProperty(propertyName);
          var typeArg1 = property.PropertyType.GetTypeInfo().IsGenericType
            ? property.PropertyType.GetGenericArguments()[0]
            : null;
          if (null != typeArg1 &&
              typeof(ICollection<>).MakeGenericType(typeArg1).IsAssignableFrom(property.PropertyType))
          {
            var collection = property.GetValue(Top.obj, null);
            var methodInfo = collection.GetType().GetMethod("Add");
            var converted = ConvertValueIfNeed(initialized.obj.GetType(), typeArg1, initialized.obj);
            methodInfo.Invoke(collection, new[] { converted });
          }
          else
          {
            // If parent object property is IList add current object into them without conversion
            if (typeof(IList).IsAssignableFrom(property.PropertyType))
            {
              var list = (IList) property.GetValue(Top.obj, null);
              list.Add(initialized.obj);
            }
            else
            {
              // If parent object property is IDictionary<string, T>,
              // add current object into them (by x:Key value) 
              // with conversion to T if need
              var typeArg2 = property.PropertyType.GetTypeInfo().IsGenericType &&
                             property.PropertyType.GetGenericArguments().Length > 1
                ? property.PropertyType.GetGenericArguments()[1]
                : null;
              if (null != typeArg1 &&
                  typeArg1 == typeof(string) &&
                  null != typeArg2 &&
                  typeof(IDictionary<,>).MakeGenericType(typeArg1, typeArg2)
                    .IsAssignableFrom(property.PropertyType))
              {
                var dictionary = property.GetValue(Top.obj, null);
                var methodInfo = dictionary.GetType().GetMethod("Add");
                var converted = ConvertValueIfNeed(initialized.obj.GetType(), typeArg2, initialized.obj);
                if (null == initialized.key)
                {
                  throw new InvalidOperationException("Key is not specified for item of dictionary");
                }

                methodInfo.Invoke(dictionary, new[] { initialized.key, converted });
              }
              else
              {
                // Handle as property - call setter with conversion if need
                property.SetValue(Top.obj, ConvertValueIfNeed(
                    initialized.obj.GetType(), property.PropertyType, initialized.obj),
                  null);
              }
            }
          }
        }

        // Если у объекта задан x:Id, добавить его в objectsById
        if (initialized.id != null)
        {
          if (_objectsById.ContainsKey(initialized.id))
          {
            throw new InvalidOperationException(string.Format("Object with Id={0} redefinition.", initialized.id));
          }

          _objectsById.Add(initialized.id, initialized.obj);

          ProcessFixupTokens();
        }
      }
    }

    private void ProcessFixupTokens()
    {
      // Выполнить поиск fixup tokens, желания которых удовлетворены,
      // и вызвать расширения разметки для них снова
      var tokens = new List<FixupToken>(_fixupTokens);
      _fixupTokens.Clear();
      foreach (var token in tokens)
      {
        if (token.Ids.All(id => _objectsById.ContainsKey(id)))
        {
          var markupExtensionsParser = new MarkupExtensionsParser(new MarkupExtensionsResolver(this), token.Expression);
          var context = new MarkupExtensionContext(this, token.Expression, token.PropertyName, token.Object, token.DataContext);
          var providedValue = markupExtensionsParser.ProcessMarkupExtension(context);
          if (providedValue is IFixupToken)
          {
            _fixupTokens.Add((FixupToken) providedValue);
          }
          else
          {
            // assign providedValue to property of object
            if (null != providedValue)
            {
              var propertyInfo = token.Object.GetType().GetProperty(token.PropertyName);
              var convertedValue = ConvertValueIfNeed(providedValue.GetType(), propertyInfo.PropertyType, providedValue);
              propertyInfo.SetValue(token.Object, convertedValue, null);
            }
          }
        }
        else
        {
          _fixupTokens.Add(token);
        }
      }
    }

    /// <summary>
    /// Converts the value from source type to destination if need
    /// using default conversion strategies and registered type converters.
    /// </summary>
    /// <param name="source">Type of source value</param>
    /// <param name="dest">Type of destination</param>
    /// <param name="value">Source value</param>
    internal static object ConvertValueIfNeed(Type source, Type dest, object value)
    {
      if (dest.IsAssignableFrom(source))
      {
        return value;
      }

      // Process enumerations
      // todo : add TypeConverterAttribute support on enum, and unit tests
      if (source == typeof(string) && dest.GetTypeInfo().IsEnum)
      {
        var enumNames = Enum.GetNames(dest);
        for (int i = 0, len = enumNames.Length; i < len; i++)
        {
          if (enumNames[i] == (String) value)
          {
            return Enum.GetValues(dest).GetValue(i);
          }
        }

        throw new Exception("Specified enum value not found.");
      }

      // todo : default converters for primitives
      if (source == typeof(string) && dest == typeof(bool))
      {
        return bool.Parse((string) value);
      }

      if (source == typeof(string) && dest == typeof(int))
      {
        return int.Parse((string) value);
      }

      if (source == typeof(string) && dest == typeof(int?))
      {
        return int.Parse((string) value);
      }

      // Process TypeConverterAttribute attributes if exist
      if (Type.GetTypeCode(source) == TypeCode.Object)
      {
        var attributes = source.GetTypeInfo().GetCustomAttributes(typeof(TypeConverterAttribute), true).ToArray();
        if (attributes.Length > 1)
        {
          throw new InvalidOperationException("Ambiguous attribute: more than one TypeConverterAttribute");
        }

        if (attributes.Length == 1)
        {
          var attribute = (TypeConverterAttribute) attributes[0];
          var typeConverterType = attribute.Type;
          var ctor = typeConverterType.GetConstructor(new Type[0]);
          if (null == ctor)
          {
            throw new InvalidOperationException($"No default constructor in {typeConverterType.Name} type");
          }

          var converter = (ITypeConverter) ctor.Invoke(new object[0]);
          if (converter.CanConvertTo(dest))
          {
            return converter.ConvertTo(value, dest);
          }
        }
      }

      if (Type.GetTypeCode(dest) == TypeCode.Object)
      {
        var attributes = dest.GetTypeInfo().GetCustomAttributes(typeof(TypeConverterAttribute), true).ToArray();
        if (attributes.Length > 1)
        {
          throw new InvalidOperationException("Ambiguous attribute: more than one TypeConverterAttribute");
        }

        if (attributes.Length == 1)
        {
          var attribute = (TypeConverterAttribute) attributes[0];
          var typeConverterType = attribute.Type;
          var ctor = typeConverterType.GetConstructor(new Type[0]);
          if (null == ctor)
          {
            throw new InvalidOperationException(string.Format("No default constructor in {0} type", typeConverterType.Name));
          }

          var converter = (ITypeConverter) ctor.Invoke(new object[0]);
          if (converter.CanConvertFrom(source))
          {
            return converter.ConvertFrom(value);
          }
        }
      }

      throw new NotSupportedException();
    }

    private Type ResolveMarkupExtensionType(string name)
    {
      var namespacesToScan = GetNamespacesToScan(name, out var bindingName);

      // Scan namespaces todo : cache types lists
      Type resultType = null;
      foreach (var ns in namespacesToScan)
      {
        var regex = new Regex("clr-namespace:(.+);assembly=(.+)");
        var matchCollection = regex.Matches(ns);
        if (matchCollection.Count == 0)
        {
          throw new InvalidOperationException(string.Format("Invalid clr-namespace syntax: {0}", ns));
        }

        var namespaceName = matchCollection[0].Groups[1].Value;
        var assemblyName = matchCollection[0].Groups[2].Value;

        var assembly = Assembly.Load(new AssemblyName(assemblyName));
        var types = assembly.GetTypes().Where(type =>
        {
          if (type.Namespace != namespaceName)
          {
            return false;
          }

          var attributes = type.GetTypeInfo().GetCustomAttributes(typeof(MarkupExtensionAttribute), true).ToArray();
          return (attributes.Any(o => ((MarkupExtensionAttribute) o).Name == bindingName));
        }).ToList();

        if (types.Count > 1)
        {
          throw new InvalidOperationException(string.Format("More than one markup extension" + " for name {0} in namespace {1}.", name, ns));
        }
        else if (types.Count == 1)
        {
          resultType = types[0];
          break;
        }
      }

      if (resultType == null)
      {
        throw new InvalidOperationException(string.Format("Cannot resolve markup extension {0}.", name));
      }

      return resultType;
    }

    /// <summary>
    /// Принимает на вход название типа и возвращает объект Type, ему соответствующий.
    /// Название типа может быть как с префиксом (qualified), так и без него.
    /// Если название типа содержит префикс, то поиск будет осуществляться в соответствующем
    /// объявленном clr-namespace. Если же название префикса не содержит, поиск будет
    /// выполняться в наборе пространств имён по умолчанию (defaultNamespaces), которые
    /// задаются в конструкторе класса XamlParser.
    /// </summary>
    private Type ResolveType(string name)
    {
      var namespacesToScan = GetNamespacesToScan(name, out var typeName);

      // Scan namespaces todo : cache types lists
      Type resultType = null;
      foreach (var ns in namespacesToScan)
      {
        var regex = new Regex("clr-namespace:(.+);assembly=(.+)");
        var matchCollection = regex.Matches(ns);
        if (matchCollection.Count == 0)
        {
          throw new InvalidOperationException(string.Format("Invalid clr-namespace syntax: {0}", ns));
        }

        var namespaceName = matchCollection[0].Groups[1].Value;
        var assemblyName = matchCollection[0].Groups[2].Value;

        var assembly = Assembly.Load(new AssemblyName(assemblyName));
        var types = assembly
          .GetTypes()
          .Where(type => type.Namespace == namespaceName && type.Name == typeName)
          .ToList();
        if (types.Count > 1)
        {
          throw new InvalidOperationException("Assertion error.");
        }
        else if (types.Count == 1)
        {
          resultType = types[0];
          break;
        }
      }

      if (resultType == null)
      {
        throw new InvalidOperationException(string.Format("Cannot resolve type {0}", name));
      }

      return resultType;
    }

    /// <summary>
    /// Returns list of namespaces to scan for name.
    /// If name is prefixed, namespaces will be that was registered for this prefix.
    /// If name is without prefix, default namespaces will be returned.
    /// </summary>
    private IEnumerable<string> GetNamespacesToScan(string name, out string unprefixedName)
    {
      List<string> namespacesToScan;
      if (name.Contains(":"))
      {
        var prefix = name.Substring(0, name.IndexOf(':'));
        if (name.IndexOf(':') + 1 >= name.Length)
        {
          throw new InvalidOperationException(string.Format("Invalid type name {0}", name));
        }

        unprefixedName = name.Substring(name.IndexOf(':') + 1);
        if (!_namespaces.ContainsKey(prefix))
        {
          throw new InvalidOperationException(string.Format("Unknown prefix {0}", prefix));
        }

        namespacesToScan = new List<string>() { _namespaces[prefix] };
      }
      else
      {
        namespacesToScan = _defaultNamespaces;
        unprefixedName = name;
      }

      return namespacesToScan;
    }
  }
}

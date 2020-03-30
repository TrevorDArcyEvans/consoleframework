using System;

namespace Xaml
{
  public partial class XamlParser
  {
    private class ObjectInfo
    {
      /// <summary>
      /// Type of constructing object.
      /// </summary>
      public Type type;

      /// <summary>
      /// Object instance (or null if String is created).
      /// </summary>
      public object obj;

      /// <summary>
      /// Current property that defined using tag with dot in name.
      /// &lt;Window.Resources&gt; for example
      /// </summary>
      public string currentProperty;

      /// <summary>
      /// For tags which content is text.
      /// </summary>
      public string currentPropertyText;

      /// <summary>
      /// Ключ, задаваемый атрибутом x:Key (если есть) - по этому ключу объект будет
      /// положен в Dictionary-свойство родительского объекта.
      /// </summary>
      public string key;

      /// <summary>
      /// Ключ, задаваемый атрибутом x:Id (если есть). По этому ключу объект будет
      /// доступен из расширений разметки по ссылкам (например, через Ref).
      /// </summary>
      public string id;
    }
  }
}

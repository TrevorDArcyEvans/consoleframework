using System;
using System.Collections.Generic;

namespace Xaml
{
  public partial class XamlParser
  {
    private class MarkupExtensionContext : IMarkupExtensionContext
    {
      public string PropertyName { get; private set; }
      public object Object { get; private set; }
      public object DataContext { get; private set; }
      private readonly XamlParser self;
      private readonly string expression;

      public object GetObjectById(string id)
      {
        object value;
        return self._objectsById.TryGetValue(id, out value) ? value : null;
      }

      /// <summary>
      /// fixupTokensAvailable = true означает, что парсинг ещё не закончен, и ещё можно
      /// создать FixupToken, false означает, что парсинг уже завершён, и новых объектов
      /// уже не появится, поэтому если расширение разметки не может обнаружить ссылку на
      /// объект, то ему уже нечего делать, кроме как завершать работу выбросом исключения.
      /// </summary>
      public bool IsFixupTokenAvailable
      {
        get { return self._objects.Count != 0; }
      }

      public IFixupToken GetFixupToken(IEnumerable<string> ids)
      {
        if (!IsFixupTokenAvailable)
          throw new InvalidOperationException("Fixup tokens are not available now.");
        FixupToken fixupToken = new FixupToken();
        fixupToken.Expression = expression;
        fixupToken.PropertyName = PropertyName;
        fixupToken.Object = Object;
        fixupToken.DataContext = DataContext;
        fixupToken.Ids = ids;
        return fixupToken;
      }

      public MarkupExtensionContext(XamlParser self,
        string expression,
        string propertyName,
        object obj,
        object dataContext)
      {
        this.self = self;
        this.expression = expression;
        PropertyName = propertyName;
        Object = obj;
        DataContext = dataContext;
      }
    }
  }
}

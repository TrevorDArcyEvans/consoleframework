using System.Collections.Generic;

namespace Xaml
{
  public partial class XamlParser
  {
    private class FixupToken : IFixupToken
    {
      /// <summary>
      /// Строковое представление расширения разметки, которое вернуло этот токен.
      /// </summary>
      public string Expression;

      /// <summary>
      /// Имя свойства, которое задано этим расширением разметки.
      /// </summary>
      public string PropertyName;

      /// <summary>
      /// Объект, свойство которого определяется расширением разметки.
      /// </summary>
      public object Object;

      /// <summary>
      /// Переданный в расширение разметки dataContext.
      /// </summary>
      public object DataContext;

      /// <summary>
      /// Список x:Id, которые не были найдены в текущем состоянии графа объектов,
      /// но которые необходимы для полного выполнения ProvideValue.
      /// </summary>
      public IEnumerable<string> Ids;
    }
  }
}

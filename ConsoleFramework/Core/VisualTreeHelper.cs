using System;
using System.Collections.Generic;
using ConsoleFramework.Controls;

namespace ConsoleFramework.Core
{
  public class VisualTreeHelper
  {
    public static List<Control> FindAllChilds(Control control, Func<Control, bool> predicate)
    {
      if (null == control)
      {
        throw new ArgumentNullException("control");
      }

      if (null == predicate)
      {
        throw new ArgumentNullException("predicate");
      }

      var queue = new List<Control>();
      FindAllChildsRecursively(queue, control, predicate);

      return queue;
    }

    private static void FindAllChildsRecursively(List<Control> queue,
      Control control, Func<Control, bool> predicate)
    {
      foreach (var child in control.Children)
      {
        if (predicate(child))
        {
          queue.Add(child);
        }

        FindAllChildsRecursively(queue, child, predicate);
      }
    }

    /// <summary>
    /// Рекурсивно ищёт дочерний элемент по указанному Name.
    /// Если в результате поиска подходящий элемент не был найден, возвращается null.
    /// </summary>
    public static Control FindChildByName(Control control, string childName)
    {
      if (null == control)
      {
        throw new ArgumentNullException("control");
      }

      if (string.IsNullOrEmpty(childName))
      {
        throw new ArgumentException("String is null or empty", "childName");
      }

      return FindChildByNameRecursively(control, childName);
    }

    private static Control FindChildByNameRecursively(Control control, string childName)
    {
      var children = control.Children;
      foreach (var child in children)
      {
        if (child.Name == childName)
        {
          return child;
        }
        else
        {
          Control result = FindChildByNameRecursively(child, childName);
          if (null != result)
          {
            return result;
          }
        }
      }

      return null;
    }

    public static bool IsConnectedToRoot(Control control)
    {
      if (null == control)
      {
        throw new ArgumentNullException("control");
      }

      var root = ConsoleApplication.Instance.RootControl;
      var current = control;
      while (current != null)
      {
        if (current == root)
        {
          return true;
        }

        current = current.Parent;
      }

      return false;
    }

    /// <summary>
    /// Ищет ближайший родительский элемент контрола типа T.
    /// Возвращает его либо null, если такой не найден.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="control"></param>
    /// <returns></returns>
    public static T FindClosestParent<T>(Control control) where T : Control
    {
      var tmp = control;
      while (tmp != null && !(tmp is T))
      {
        tmp = tmp.Parent;
      }

      if (tmp is T)
      {
        return (T) tmp;
      }

      return null;
    }

    /// <summary>
    /// Находит самый верхний элемент под указателем мыши с координатами rawPoint.
    /// Учитывается прозрачность элементов - если пиксель, куда указывает мышь, отмечен как
    /// прозрачный для событий мыши (opacity от 4 до 7), то они будут проходить насквозь,
    /// к следующему контролу. Также учитывается видимость элементов - Hidden и Collapsed элементы
    /// будут проигнорированы.
    /// Так обрабатываются, например, тени окошек и прозрачные места контролов (первый столбец Combobox).
    /// </summary>
    /// <param name="localPoint">Координаты относительно control</param>
    /// <param name="control">RootElement для проверки всего визуального дерева.</param>
    /// <returns>Элемент управления или null, если событие мыши было за границами всех контролов, или
    /// если все контролы были прозрачны для событий мыши</returns>
    public static Control FindTopControlUnderMouse(Control control, Point localPoint)
    {
      if (null == control)
      {
        throw new ArgumentNullException("control");
      }

      var rawPoint = Control.TranslatePoint(control, localPoint, null);

      if (control.Children.Count != 0)
      {
        var childrenOrderedByZIndex = control.GetChildrenOrderedByZIndex();
        for (var i = childrenOrderedByZIndex.Count - 1; i >= 0; i--)
        {
          var child = childrenOrderedByZIndex[i];
          if (Control.HitTest(rawPoint, control, child))
          {
            var foundSource = FindTopControlUnderMouse(child, Control.TranslatePoint(control, localPoint, child));
            if (null != foundSource)
            {
              return foundSource;
            }
          }
        }
      }

      var controlRect = new Rect(new Point(0, 0), control.RenderSize);
      if (!controlRect.Contains(localPoint))
      {
        return null;
      }
      else
      {
        if (control.Visibility != Visibility.Visible)
        {
          return null;
        }

        var _opacity = ConsoleApplication.Instance.Renderer.GetControlOpacityAt(control, localPoint.X, localPoint.Y);
        if (_opacity >= 4 && _opacity <= 7)
        {
          return null;
        }
      }

      return control;
    }
  }
}

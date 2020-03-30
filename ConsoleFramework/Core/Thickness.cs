using System;
using Xaml;

namespace ConsoleFramework.Core
{
  /// <summary>
  /// WPF Thickness analog but using integers instead doubles.
  /// </summary>
  [TypeConverter(typeof(ThicknessConverter))]
  public struct Thickness : IEquatable<Thickness>
  {
    private int _left;

    public int Left
    {
      get { return _left; }
      set { _left = value; }
    }

    private int _top;

    public int Top
    {
      get { return _top; }
      set { _top = value; }
    }

    private int _right;

    public int Right
    {
      get { return _right; }
      set { _right = value; }
    }

    private int _bottom;

    public int Bottom
    {
      get { return _bottom; }
      set { _bottom = value; }
    }

    public Thickness(int uniformLength)
    {
      _left = _top = _right = _bottom = uniformLength;
    }

    public Thickness(int left, int top, int right, int bottom)
    {
      this._left = left;
      this._top = top;
      this._right = right;
      this._bottom = bottom;
    }

    internal bool IsZero()
    {
      return _left == 0 &&
             _right == 0 &&
             _top == 0 &&
             _bottom == 0;
    }

    internal bool IsUniform()
    {
      return _left == _top &&
             _left == _right &&
             _left == _bottom;
    }

    public static bool operator ==(Thickness t1, Thickness t2)
    {
      return t1._left == t2._left &&
             t1._top == t2._top &&
             t1._right == t2._right &&
             t1._bottom == t2._bottom;
    }

    public static bool operator !=(Thickness t1, Thickness t2)
    {
      return !(t1 == t2);
    }

    public bool Equals(Thickness other)
    {
      return other._left == _left &&
             other._top == _top &&
             other._right == _right &&
             other._bottom == _bottom;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }

      if (obj.GetType() != typeof(Thickness))
      {
        return false;
      }

      return Equals((Thickness) obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        int result = _left;
        result = (result * 397) ^ _top;
        result = (result * 397) ^ _right;
        result = (result * 397) ^ _bottom;
        return result;
      }
    }

    public override string ToString()
    {
      return string.Format("{0},{1},{2},{3}", _left, _top, _right, _bottom);
    }
  }
}

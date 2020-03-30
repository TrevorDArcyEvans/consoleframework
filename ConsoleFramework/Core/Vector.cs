using System;

namespace ConsoleFramework.Core
{
  public struct Vector
  {
    public override string ToString()
    {
      return string.Format("{0};{1}", _x, _y);
    }

    public static bool operator ==(Vector vector1, Vector vector2)
    {
      return vector1.X == vector2.X && 
             vector1.Y == vector2.Y;
    }

    public static bool operator !=(Vector vector1, Vector vector2)
    {
      return !(vector1 == vector2);
    }

    public static bool Equals(Vector vector1, Vector vector2)
    {
      return vector1.X.Equals(vector2.X) && 
             vector1.Y.Equals(vector2.Y);
    }

    public override bool Equals(object o)
    {
      if ((o == null) || !(o is Vector))
      {
        return false;
      }

      var vector = (Vector) o;
      return Equals(this, vector);
    }

    public bool Equals(Vector value)
    {
      return Equals(this, value);
    }

    public override int GetHashCode()
    {
      return (this.X.GetHashCode() ^ this.Y.GetHashCode());
    }

    internal int _x;
    public int X
    {
      get { return this._x; }
      set { this._x = value; }
    }

    internal int _y;
    public int Y
    {
      get { return this._y; }
      set { this._y = value; }
    }

    public Vector(int x, int y)
    {
      this._x = x;
      this._y = y;
    }

    public double Length
    {
      get { return Math.Sqrt(_x * _x + _y * _y); }
    }

    public double LengthSquared
    {
      get { return ((this._x * this._x) + (this._y * this._y)); }
    }

    public static double CrossProduct(Vector vector1, Vector vector2)
    {
      return ((vector1._x * vector2._y) - (vector1._y * vector2._x));
    }

    public static double AngleBetween(Vector vector1, Vector vector2)
    {
      double y = (vector1._x * vector2._y) - (vector2._x * vector1._y);
      double x = (vector1._x * vector2._x) + (vector1._y * vector2._y);
      return (Math.Atan2(y, x) * 57.295779513082323);
    }

    public static Vector operator -(Vector vector)
    {
      return new Vector(-vector._x, -vector._y);
    }

    public void Negate()
    {
      this._x = -this._x;
      this._y = -this._y;
    }

    public static Vector operator +(Vector vector1, Vector vector2)
    {
      return new Vector(vector1._x + vector2._x, vector1._y + vector2._y);
    }

    public static Vector Add(Vector vector1, Vector vector2)
    {
      return new Vector(vector1._x + vector2._x, vector1._y + vector2._y);
    }

    public static Vector operator -(Vector vector1, Vector vector2)
    {
      return new Vector(vector1._x - vector2._x, vector1._y - vector2._y);
    }

    public static Vector Subtract(Vector vector1, Vector vector2)
    {
      return new Vector(vector1._x - vector2._x, vector1._y - vector2._y);
    }

    public static Point operator +(Vector vector, Point point)
    {
      return new Point(point._x + vector._x, point._y + vector._y);
    }

    public static Point Add(Vector vector, Point point)
    {
      return new Point(point._x + vector._x, point._y + vector._y);
    }

    public static Vector operator *(Vector vector, int scalar)
    {
      return new Vector(vector._x * scalar, vector._y * scalar);
    }

    public static Vector Multiply(Vector vector, int scalar)
    {
      return new Vector(vector._x * scalar, vector._y * scalar);
    }

    public static Vector operator *(int scalar, Vector vector)
    {
      return new Vector(vector._x * scalar, vector._y * scalar);
    }

    public static Vector Multiply(int scalar, Vector vector)
    {
      return new Vector(vector._x * scalar, vector._y * scalar);
    }

    public static double operator *(Vector vector1, Vector vector2)
    {
      return ((vector1._x * vector2._x) + (vector1._y * vector2._y));
    }

    public static double Multiply(Vector vector1, Vector vector2)
    {
      return ((vector1._x * vector2._x) + (vector1._y * vector2._y));
    }

    public static double Determinant(Vector vector1, Vector vector2)
    {
      return vector1._x * vector2._y - vector1._y * vector2._x;
    }

    public static explicit operator Point(Vector vector)
    {
      return new Point(vector._x, vector._y);
    }
  }
}

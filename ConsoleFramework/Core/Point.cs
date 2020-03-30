namespace ConsoleFramework.Core
{
  public struct Point
  {
    public static bool operator ==(Point point1, Point point2)
    {
      return ((point1.X == point2.X) && (point1.Y == point2.Y));
    }

    public static bool operator !=(Point point1, Point point2)
    {
      return !(point1 == point2);
    }

    public static bool Equals(Point point1, Point point2)
    {
      return (point1.X.Equals(point2.X) && point1.Y.Equals(point2.Y));
    }

    public override bool Equals(object o)
    {
      if ((o == null) || !(o is Point))
      {
        return false;
      }

      Point point = (Point) o;
      return Equals(this, point);
    }

    public bool Equals(Point value)
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

    public Point(int x, int y)
    {
      this._x = x;
      this._y = y;
    }

    public void Offset(int offsetX, int offsetY)
    {
      this._x += offsetX;
      this._y += offsetY;
    }

    public static Point operator +(Point point, Vector vector)
    {
      return new Point(point._x + vector._x, point._y + vector._y);
    }

    public static Point Add(Point point, Vector vector)
    {
      return new Point(point._x + vector._x, point._y + vector._y);
    }

    public static Point operator -(Point point, Vector vector)
    {
      return new Point(point._x - vector._x, point._y - vector._y);
    }

    public static Point Subtract(Point point, Vector vector)
    {
      return new Point(point._x - vector._x, point._y - vector._y);
    }

    public static Vector operator -(Point point1, Point point2)
    {
      return new Vector(point1._x - point2._x, point1._y - point2._y);
    }

    public static Vector Subtract(Point point1, Point point2)
    {
      return new Vector(point1._x - point2._x, point1._y - point2._y);
    }

    public static explicit operator Vector(Point point)
    {
      return new Vector(point._x, point._y);
    }

    public override string ToString()
    {
      return string.Format("{0};{1}", _x, _y);
    }
  }
}

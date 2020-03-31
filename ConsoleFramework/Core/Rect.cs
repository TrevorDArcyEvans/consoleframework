using System;

namespace ConsoleFramework.Core
{
  public struct Rect : IFormattable
  {
    private static readonly Rect s_empty;

    public static bool operator ==(Rect rect1, Rect rect2)
    {
      return ((((rect1.X == rect2.X) && (rect1.Y == rect2.Y)) && (rect1.Width == rect2.Width)) &&
              (rect1.Height == rect2.Height));
    }

    public static bool operator !=(Rect rect1, Rect rect2)
    {
      return !(rect1 == rect2);
    }

    public static bool Equals(Rect rect1, Rect rect2)
    {
      if (rect1.IsEmpty)
      {
        return rect2.IsEmpty;
      }

      return (((rect1.X.Equals(rect2.X) && rect1.Y.Equals(rect2.Y)) && rect1.Width.Equals(rect2.Width)) &&
              rect1.Height.Equals(rect2.Height));
    }

    public override bool Equals(object o)
    {
      if ((o == null) || !(o is Rect))
      {
        return false;
      }

      Rect rect = (Rect) o;
      return Equals(this, rect);
    }

    public bool Equals(Rect value)
    {
      return Equals(this, value);
    }

    public override int GetHashCode()
    {
      if (this.IsEmpty)
      {
        return 0;
      }

      return (((this.X.GetHashCode() ^ this.Y.GetHashCode()) ^ this.Width.GetHashCode()) ^
              this.Height.GetHashCode());
    }

    public override string ToString()
    {
      return this.ConvertToString(null, null);
    }

    public string ToString(IFormatProvider provider)
    {
      return this.ConvertToString(null, provider);
    }

    string IFormattable.ToString(string format, IFormatProvider provider)
    {
      return this.ConvertToString(format, provider);
    }

    internal string ConvertToString(string format, IFormatProvider provider)
    {
      if (this.IsEmpty)
      {
        return "Empty";
      }

      const char numericListSeparator = ',';
      return string.Format(provider,
        "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}",
        new object[]
        {
          numericListSeparator, this._x, this._y, this._width, this._height
        });
    }

    public Rect(Rect copy)
    {
      this._x = copy._x;
      this._y = copy._y;
      this._width = copy._width;
      this._height = copy._height;
    }

    public Rect(Point location, Size size)
    {
      if (size.IsEmpty)
      {
        this = s_empty;
      }
      else
      {
        this._x = location.X;
        this._y = location.Y;
        this._width = size.Width;
        this._height = size.Height;
      }
    }

    public Rect(int x, int y, int width, int height)
    {
      if ((width < 0) || (height < 0))
      {
        throw new ArgumentException("Size_WidthAndHeightCannotBeNegative");
      }

      this._x = x;
      this._y = y;
      this._width = width;
      this._height = height;
    }

    public Rect(Point point1, Point point2)
    {
      this._x = Math.Min(point1.X, point2.X);
      this._y = Math.Min(point1.Y, point2.Y);
      this._width = Math.Max((Math.Max(point1.X, point2.X) - this._x), 0);
      this._height = Math.Max((Math.Max(point1.Y, point2.Y) - this._y), 0);
    }

    public Rect(Point point, Vector vector) : this(point, point + vector)
    {
    }

    public Rect(Size size)
    {
      if (size.IsEmpty)
      {
        this = s_empty;
      }
      else
      {
        this._x = this._y = 0;
        this._width = size.Width;
        this._height = size.Height;
      }
    }

    public static Rect Empty
    {
      get { return s_empty; }
    }

    public bool IsEmpty
    {
      get { return this._width == 0 || this._height == 0; }
    }

    public Point Location
    {
      get { return new Point(this._x, this._y); }
      set
      {
        if (this.IsEmpty)
        {
          throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
        }

        this._x = value.X;
        this._y = value.Y;
      }
    }

    public Size Size
    {
      get
      {
        if (this.IsEmpty)
        {
          return Size.Empty;
        }

        return new Size(this._width, this._height);
      }
      set
      {
        if (value.IsEmpty)
        {
          this = s_empty;
        }
        else
        {
          if (this.IsEmpty)
          {
            throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
          }

          this._width = value.Width;
          this._height = value.Height;
        }
      }
    }

    private int _x;
    public int X
    {
      get { return this._x; }
      set
      {
        if (this.IsEmpty)
        {
          throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
        }

        this._x = value;
      }
    }

    private int _y;
    public int Y
    {
      get { return this._y; }
      set
      {
        if (this.IsEmpty)
        {
          throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
        }

        this._y = value;
      }
    }

    private int _width;
    public int Width
    {
      get { return this._width; }
      set
      {
        if (this.IsEmpty)
        {
          throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
        }

        if (value < 0)
        {
          throw new ArgumentException("Size_WidthCannotBeNegative");
        }

        this._width = value;
      }
    }

    private int _height;
    public int Height
    {
      get { return this._height; }
      set
      {
        if (this.IsEmpty)
        {
          throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
        }

        if (value < 0)
        {
          throw new ArgumentException("Size_HeightCannotBeNegative");
        }

        this._height = value;
      }
    }

    public int Left
    {
      get { return this._x; }
    }

    public int Top
    {
      get { return this._y; }
    }

    public int Right
    {
      get
      {
        if (this.IsEmpty)
        {
          return 0;
        }

        return (this._x + this._width);
      }
    }

    public int Bottom
    {
      get
      {
        if (this.IsEmpty)
        {
          return 0;
        }

        return (this._y + this._height);
      }
    }

    public Point TopLeft
    {
      get { return new Point(this.Left, this.Top); }
    }

    public Point TopRight
    {
      get { return new Point(this.Right, this.Top); }
    }

    public Point BottomLeft
    {
      get { return new Point(this.Left, this.Bottom); }
    }

    public Point BottomRight
    {
      get { return new Point(this.Right, this.Bottom); }
    }

    public bool Contains(Point point)
    {
      return this.Contains(point.X, point.Y);
    }

    public bool Contains(int x, int y)
    {
      if (this.IsEmpty)
      {
        return false;
      }

      return this.ContainsInternal(x, y);
    }

    public bool Contains(Rect rect)
    {
      if (this.IsEmpty || rect.IsEmpty)
      {
        return false;
      }

      return ((((this._x <= rect._x) && (this._y <= rect._y)) && ((this._x + this._width) >= (rect._x + rect._width))) &&
              ((this._y + this._height) >= (rect._y + rect._height)));
    }

    public bool IntersectsWith(Rect rect)
    {
      if (this.IsEmpty || rect.IsEmpty)
      {
        return false;
      }

      return ((((rect.Left <= this.Right) && (rect.Right >= this.Left)) && (rect.Top <= this.Bottom)) &&
              (rect.Bottom >= this.Top));
    }

    public void Intersect(Rect rect)
    {
      if (!this.IntersectsWith(rect))
      {
        this = Empty;
      }
      else
      {
        int num = Math.Max(this.Left, rect.Left);
        int num2 = Math.Max(this.Top, rect.Top);
        this._width = Math.Max((Math.Min(this.Right, rect.Right) - num), 0);
        this._height = Math.Max((Math.Min(this.Bottom, rect.Bottom) - num2), 0);
        this._x = num;
        this._y = num2;
      }
    }

    public static Rect Intersect(Rect rect1, Rect rect2)
    {
      rect1.Intersect(rect2);
      return rect1;
    }

    public void Union(Rect rect)
    {
      if (this.IsEmpty)
      {
        this = rect;
      }
      else if (!rect.IsEmpty)
      {
        int num = Math.Min(this.Left, rect.Left);
        int num2 = Math.Min(this.Top, rect.Top);
        if ((rect.Width == int.MaxValue) || (this.Width == int.MaxValue))
        {
          this._width = int.MaxValue;
        }
        else
        {
          int num3 = Math.Max(this.Right, rect.Right);
          this._width = Math.Max((num3 - num), 0);
        }

        if ((rect.Height == int.MaxValue) || (this.Height == int.MaxValue))
        {
          this._height = int.MaxValue;
        }
        else
        {
          int num4 = Math.Max(this.Bottom, rect.Bottom);
          this._height = Math.Max((num4 - num2), 0);
        }

        this._x = num;
        this._y = num2;
      }
    }

    public static Rect Union(Rect rect1, Rect rect2)
    {
      rect1.Union(rect2);
      return rect1;
    }

    public void Union(Point point)
    {
      this.Union(new Rect(point, point));
    }

    public static Rect Union(Rect rect, Point point)
    {
      rect.Union(new Rect(point, point));
      return rect;
    }

    public void Offset(Vector offsetVector)
    {
      if (this.IsEmpty)
      {
        throw new InvalidOperationException("Rect_CannotCallMethod");
      }

      this._x += offsetVector.X;
      this._y += offsetVector.Y;
    }

    public void Offset(int offsetX, int offsetY)
    {
      if (this.IsEmpty)
      {
        throw new InvalidOperationException("Rect_CannotCallMethod");
      }

      this._x += offsetX;
      this._y += offsetY;
    }

    public static Rect Offset(Rect rect, Vector offsetVector)
    {
      rect.Offset(offsetVector.X, offsetVector.Y);
      return rect;
    }

    public static Rect Offset(Rect rect, int offsetX, int offsetY)
    {
      rect.Offset(offsetX, offsetY);
      return rect;
    }

    public void Inflate(Size size)
    {
      this.Inflate(size.Width, size.Height);
    }

    public void Inflate(int width, int height)
    {
      if (this.IsEmpty)
      {
        throw new InvalidOperationException("Rect_CannotCallMethod");
      }

      this._x -= width;
      this._y -= height;
      this._width += width;
      this._width += width;
      this._height += height;
      this._height += height;
      if ((this._width < 0) || (this._height < 0))
      {
        this = s_empty;
      }
    }

    public static Rect Inflate(Rect rect, Size size)
    {
      rect.Inflate(size.Width, size.Height);
      return rect;
    }

    public static Rect Inflate(Rect rect, int width, int height)
    {
      rect.Inflate(width, height);
      return rect;
    }

    private bool ContainsInternal(int x, int y)
    {
      // исправлено нестрогое условие на строгое
      // чтобы в rect(1;1;1;1) попадал только 1 пиксель (1;1) а не 4 пикселя (1;1)-(2;2)
      return x >= _x && 
             x - _width < _x && 
             y >= _y && 
             y - _height < _y;
    }

    private static Rect CreateEmptyRect()
    {
      Rect rect = new Rect
      {
        _x = 0,
        _y = 0,
        _width = 0,
        _height = 0
      };
      return rect;
    }

    static Rect()
    {
      s_empty = CreateEmptyRect();
    }
  }
}

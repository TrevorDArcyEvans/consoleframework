﻿using System;

namespace ConsoleFramework.Core
{
  public struct Size
  {
    public static bool operator ==(Size size1, Size size2)
    {
      return (size1.Width == size2.Width) &&
             size1.Height == size2.Height;
    }

    public static bool operator !=(Size size1, Size size2)
    {
      return !(size1 == size2);
    }

    public static bool Equals(Size size1, Size size2)
    {
      if (size1.IsEmpty)
      {
        return size2.IsEmpty;
      }

      return (size1.Width.Equals(size2.Width) && size1.Height.Equals(size2.Height));
    }

    public override bool Equals(object o)
    {
      if ((o == null) || !(o is Size))
      {
        return false;
      }

      Size size = (Size) o;
      return Equals(this, size);
    }

    public bool Equals(Size value)
    {
      return Equals(this, value);
    }

    public override int GetHashCode()
    {
      if (IsEmpty)
      {
        return 0;
      }

      return Width.GetHashCode() ^ Height.GetHashCode();
    }

    public Size(int width, int height)
    {
      if (width < 0 || height < 0)
      {
        throw new ArgumentException("Width and _height cannot be negative");
      }

      this._width = width;
      this._height = height;
    }

    public static Size MaxSize { get; } = new Size(int.MaxValue, int.MaxValue);

    public static Size Empty => CreateEmptySize();

    public bool IsEmpty => _width <= 0;

    internal int _width;

    public int Width
    {
      get => _width;
      set
      {
        if (value < 0)
        {
          throw new ArgumentException("Width cannot be negative");
        }

        _width = value;
      }
    }

    internal int _height;

    public int Height
    {
      get => this._height;
      set
      {
        if (value < 0)
        {
          throw new ArgumentException("Height cannot be negative");
        }

        this._height = value;
      }
    }

    public static explicit operator Vector(Size size)
    {
      return new Vector(size._width, size._height);
    }

    public static explicit operator Point(Size size)
    {
      return new Point(size._width, size._height);
    }

    private static Size CreateEmptySize()
    {
      return new Size
      {
        _width = 0,
        _height = 0
      };
    }

    public override string ToString()
    {
      return $"Size: {Width};{Height}";
    }
  }
}

using System;
using System.Collections.Generic;
using System.Text;
using ConsoleFramework.Core;
using ConsoleFramework.Native;

namespace ConsoleFramework.Rendering
{
  /// <summary>
  /// Represents the buffer prepared to output into terminal.
  /// Provides indexer-like access to buffer and method <see cref="Flush(ConsoleFramework.Core.Rect)"/>.
  /// </summary>
  public partial class PhysicalCanvas
  {
    private readonly IntPtr stdOutputHandle = IntPtr.Zero;

    public PhysicalCanvas(int width, int height)
    {
      this._size = new Size(width, height);
      this._buffer = new CHAR_INFO[height, width];
    }

    public PhysicalCanvas(int width, int height, IntPtr stdOutputHandle)
    {
      this._size = new Size(width, height);
      this.stdOutputHandle = stdOutputHandle;
      this._buffer = new CHAR_INFO[height, width];
    }

    /// <summary>
    /// Buffer to marshal between application and Win32 API layer.
    /// </summary>
    private CHAR_INFO[,] _buffer;

    /// <summary>
    /// Indexers cache to avoid objects creation on every [][] call.
    /// </summary>
    private readonly Dictionary<int, NestedIndexer> cachedIndexers = new Dictionary<int, NestedIndexer>();

    private Size _size;

    public Size Size
    {
      get { return _size; }
      set
      {
        if (value != _size)
        {
          CHAR_INFO[,] oldBuffer = _buffer;
          _buffer = new CHAR_INFO[value.Height, value.Width];
          for (int x = 0, w = Math.Min(_size.Width, value.Width); x < w; x++)
          {
            for (int y = 0, h = Math.Min(_size.Height, value.Height); y < h; y++)
            {
              _buffer[y, x] = oldBuffer[y, x];
            }
          }

          _size = value;
        }
      }
    }

    public NestedIndexer this[int index]
    {
      get
      {
        if (index < 0 || index >= _size._width)
        {
          throw new IndexOutOfRangeException("index exceeds specified buffer width.");
        }

        if (cachedIndexers.ContainsKey(index))
        {
          return cachedIndexers[index];
        }

        NestedIndexer res = new NestedIndexer(index, this);
        cachedIndexers[index] = res;
        return res;
      }
    }

    /// <summary>
    /// Writes collected data to console screen buffer.
    /// </summary>
    public void Flush()
    {
      Flush(new Rect(new Point(0, 0), _size));
    }

    /// <summary>
    /// Writes collected data to console screen buffer, but paints specified rect only.
    /// </summary>
    public virtual void Flush(Rect affectedRect)
    {
      if (stdOutputHandle != IntPtr.Zero)
      {
        // we are in windows environment
        SMALL_RECT rect = new SMALL_RECT((short) affectedRect.X, (short) affectedRect.Y,
          (short) (affectedRect.Width + affectedRect.X), (short) (affectedRect.Height + affectedRect.Y));
        if (!Win32.WriteConsoleOutputCore(stdOutputHandle, _buffer, new COORD((short) _size.Width, (short) _size.Height),
          new COORD((short) affectedRect.X, (short) affectedRect.Y), ref rect))
        {
          throw new InvalidOperationException(string.Format("Cannot write to console : {0}", Win32.GetLastErrorMessage()));
        }
      }
      else
      {
        // we are in linux
        for (int i = 0; i < affectedRect.Width; i++)
        {
          int x = i + affectedRect.X;
          for (int j = 0; j < affectedRect.Height; j++)
          {
            int y = j + affectedRect.Y;
            // todo : convert attributes and optimize rendering
            bool fgIntensity;
            short index = NCurses.winAttrsToNCursesAttrs(_buffer[y, x].Attributes,
              out fgIntensity);
            if (fgIntensity)
            {
              NCurses.attrset(
                (int) (NCurses.COLOR_PAIR(index) | NCurses.A_BOLD));
            }
            else
            {
              NCurses.attrset(
                (int) NCurses.COLOR_PAIR(index));
            }

            // TODO : optimize this
            char outChar = _buffer[y, x].UnicodeChar != '\0' ? (_buffer[y, x].UnicodeChar) : ' ';
            var bytes = UTF8Encoding.UTF8.GetBytes(new char[] { outChar });
            NCurses.mvaddstr(y, x, bytes);
          }
        }

        NCurses.refresh();
      }
    }
  }
}

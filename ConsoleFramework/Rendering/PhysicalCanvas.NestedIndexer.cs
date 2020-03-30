using System;
using System.Collections.Generic;
using ConsoleFramework.Native;

namespace ConsoleFramework.Rendering
{
  public partial class PhysicalCanvas
  {
    /// <summary>
    /// Flyweight to provide [][]-style access to buffer.
    /// </summary>
    public sealed class NestedIndexer
    {
      private readonly int x;
      private readonly PhysicalCanvas canvas;
      private readonly Dictionary<int, CHAR_INFO_ref> references = new Dictionary<int, CHAR_INFO_ref>();

      public NestedIndexer(int x, PhysicalCanvas canvas)
      {
        this.x = x;
        this.canvas = canvas;
      }

      public CHAR_INFO_ref this[int index]
      {
        get
        {
          if (index < 0 || index >= canvas._size.Height)
          {
            throw new IndexOutOfRangeException("index exceeds specified buffer _height.");
          }

          if (references.ContainsKey(index))
          {
            return references[index];
          }

          CHAR_INFO_ref res = new CHAR_INFO_ref(x, index, canvas);
          references[index] = res;
          return res;
        }
      }

      /// <summary>
      /// Wrapper to provide reference-style access to struct properties (assignment and change
      /// without temporary copying in user code).
      /// </summary>
      public sealed class CHAR_INFO_ref
      {
        private readonly int x;
        private readonly int y;
        private readonly PhysicalCanvas canvas;

        public CHAR_INFO_ref(int x, int y, PhysicalCanvas canvas)
        {
          this.x = x;
          this.y = y;
          this.canvas = canvas;
        }

        public char UnicodeChar
        {
          get { return canvas._buffer[y, x].UnicodeChar; }
          set
          {
            CHAR_INFO charInfo = canvas._buffer[y, x];
            charInfo.UnicodeChar = value;
            canvas._buffer[y, x] = charInfo;
          }
        }

        public char AsciiChar
        {
          get { return canvas._buffer[y, x].AsciiChar; }
          set
          {
            CHAR_INFO charInfo = canvas._buffer[y, x];
            charInfo.AsciiChar = value;
            canvas._buffer[y, x] = charInfo;
          }
        }

        public Attr Attributes
        {
          get { return canvas._buffer[y, x].Attributes; }
          set
          {
            CHAR_INFO charInfo = canvas._buffer[y, x];
            charInfo.Attributes = value;
            canvas._buffer[y, x] = charInfo;
          }
        }

        public void Assign(CHAR_INFO charInfo)
        {
          canvas._buffer[y, x] = charInfo;
        }

        public void Assign(CHAR_INFO_ref charInfoRef)
        {
          canvas._buffer[y, x] = charInfoRef.canvas._buffer[charInfoRef.y, charInfoRef.x];
        }
      }
    }
  }
}

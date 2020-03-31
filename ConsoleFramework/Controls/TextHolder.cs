using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  public class TextHolder
  {
    // TODO : change to more appropriate data structure
    private List<string> _lines;
    private readonly string _newLine = Environment.NewLine;

    public TextHolder(string text, string newLine)
    {
      this._newLine = newLine;
      SetText(text);
    }

    public TextHolder(string text)
    {
      SetText(text);
    }

    private void SetText(string text)
    {
      _lines = new List<string>(text.Split(new[] { _newLine }, StringSplitOptions.None));
    }

    public string Text
    {
      get => string.Join(_newLine, _lines);
      set => SetText(value);
    }

    public IList<string> Lines => _lines.AsReadOnly();

    public int LinesCount => _lines.Count;
    public int ColumnsCount => _lines.Max(it => it.Length);

    /// <summary>
    /// Inserts string after specified position with respect to newline symbols.
    /// Returns the coords (col+ln) of next symbol after inserted.
    /// TODO : write unit test to check return value
    /// </summary>
    public Point Insert(int ln, int col, string s)
    {
      // There are at least one empty line even if no text at all
      if (ln >= _lines.Count)
      {
        throw new ArgumentException("ln is out of range", nameof(ln));
      }

      var currentLine = _lines[ln];
      if (col > currentLine.Length)
      {
        throw new ArgumentException("col is out of range", nameof(col));
      }

      var leftPart = currentLine.Substring(0, col);
      var rightPart = currentLine.Substring(col);

      var linesToInsert = s.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

      if (linesToInsert.Length == 1)
      {
        _lines[ln] = leftPart + linesToInsert[0] + rightPart;
        return new Point(leftPart.Length + linesToInsert[0].Length, ln);
      }
      else
      {
        _lines[ln] = leftPart + linesToInsert[0];
        _lines.InsertRange(ln + 1, linesToInsert.Skip(1).Take(linesToInsert.Length - 1));
        var lastStrLeftPart = _lines[ln + linesToInsert.Length - 1];
        _lines[ln + linesToInsert.Length - 1] = lastStrLeftPart + rightPart;
        return new Point(lastStrLeftPart.Length, ln + linesToInsert.Length - 1);
      }
    }

    /// <summary>
    /// Will write the content of text editor to matrix constrained with width/height,
    /// starting from (left, top) coord. Coords may be negative.
    /// If there are any gap before (or after) text due to margin, window will be filled
    /// with spaces there.
    /// Window size should be equal to width/height passed.
    /// </summary>
    public void WriteToWindow(int left, int top, int width, int height, char[,] window)
    {
      if (window.GetLength(0) != height)
      {
        throw new ArgumentException("window height differs from viewport height");
      }

      if (window.GetLength(1) != width)
      {
        throw new ArgumentException("window width differs from viewport width");
      }

      for (var y = top; y < 0; y++)
      {
        for (var x = 0; x < width; x++)
        {
          window[y - top, x] = ' ';
        }
      }

      for (var y = Math.Max(0, top); y < Math.Min(top + height, _lines.Count); y++)
      {
        var line = _lines[y];
        for (var x = left; x < 0; x++)
        {
          window[y - top, x - left] = ' ';
        }

        for (var x = Math.Max(0, left); x < Math.Min(left + width, line.Length); x++)
        {
          window[y - top, x - left] = line[x];
        }

        for (var x = Math.Max(line.Length, left); x < left + width; x++)
        {
          window[y - top, x - left] = ' ';
        }
      }

      for (var y = _lines.Count; y < top + height; y++)
      {
        for (var x = 0; x < width; x++)
        {
          window[y - top, x] = ' ';
        }
      }
    }

    /// <summary>
    /// Deletes text from lnFrom+colFrom to lnTo+colTo (exclusive).
    /// </summary>
    public void Delete(int lnFrom, int colFrom, int lnTo, int colTo)
    {
      if (lnFrom > lnTo)
      {
        throw new ArgumentException("lnFrom should be <= lnTo");
      }

      if (lnFrom == lnTo && colFrom >= colTo)
      {
        throw new ArgumentException("colFrom should be < colTo on the same line");
      }

      _lines[lnFrom] = _lines[lnFrom].Substring(0, colFrom) + _lines[lnTo].Substring(colTo);
      _lines.RemoveRange(lnFrom + 1, lnTo - lnFrom);
    }
  }
}

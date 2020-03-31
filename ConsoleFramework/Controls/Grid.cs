using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Core;
using ConsoleFramework.Rendering;
using Xaml;

namespace ConsoleFramework.Controls
{
  [ContentProperty("Controls")]
  public class Grid : Control
  {
    private readonly List<ColumnDefinition> _columnDefinitions = new List<ColumnDefinition>();
    private readonly List<RowDefinition> _rowDefinitions = new List<RowDefinition>();
    private readonly UIElementCollection _children;
    private int[] _columnsWidths;
    private int[] _rowsHeights;

    public List<ColumnDefinition> ColumnDefinitions
    {
      get { return _columnDefinitions; }
    }

    public List<RowDefinition> RowDefinitions
    {
      get { return _rowDefinitions; }
    }

    public UIElementCollection Controls
    {
      get { return _children; }
    }

    public Grid()
    {
      _children = new UIElementCollection(this);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
      if (ColumnDefinitions.Count == 0 || RowDefinitions.Count == 0)
      {
        return Size.Empty;
      }

      var matrix = new Control[ColumnDefinitions.Count, RowDefinitions.Count];
      for (var x = 0; x < ColumnDefinitions.Count; x++)
      {
        for (var y = 0; y < RowDefinitions.Count; y++)
        {
          if (Children.Count > y * ColumnDefinitions.Count + x)
          {
            matrix[x, y] = Children[y * ColumnDefinitions.Count + x];
          }
          else
          {
            matrix[x, y] = null;
          }
        }
      }

      // Если в качестве availableSize передано PositiveInfinity, мы просто игнорируем Star-элементы,
      // работая с ними так же как с Auto и производим обычное размещение
      var interpretStarAsAuto = availableSize.Width == int.MaxValue ||
                                availableSize.Height == int.MaxValue;

      // Сначала выполняем Measure всех контролов с учётом ограничений,
      // определённых в ColumnDefinitions и RowDefinitions
      for (var x = 0; x < ColumnDefinitions.Count; x++)
      {
        var columnDefinition = ColumnDefinitions[x];

        var width = columnDefinition.Width.GridUnitType == GridUnitType.Pixel
          ? columnDefinition.Width.Value
          : int.MaxValue;

        for (var y = 0; y < RowDefinitions.Count; y++)
        {
          var rowDefinition = RowDefinitions[y];

          var height = rowDefinition.Height.GridUnitType == GridUnitType.Pixel
            ? rowDefinition.Height.Value
            : int.MaxValue;

          // Apply min-max constraints
          if (columnDefinition.MinWidth != null && width < columnDefinition.MinWidth.Value)
          {
            width = columnDefinition.MinWidth.Value;
          }

          if (columnDefinition.MaxWidth != null && width > columnDefinition.MaxWidth.Value)
          {
            width = columnDefinition.MaxWidth.Value;
          }

          if (rowDefinition.MinHeight != null && height < rowDefinition.MinHeight.Value)
          {
            height = rowDefinition.MinHeight.Value;
          }

          if (rowDefinition.MaxHeight != null && height > rowDefinition.MaxHeight.Value)
          {
            height = rowDefinition.MaxHeight.Value;
          }

          if (matrix[x, y] != null)
          {
            matrix[x, y].Measure(new Size(width, height));
          }
        }
      }

      // Теперь для каждого столбца (не-Star) нужно вычислить максимальный Width, а для
      // каждой строки - максимальный Height - эти значения и станут соответственно
      // шириной и высотой ячеек, определяемых координатами строки и столбца

      _columnsWidths = new int[ColumnDefinitions.Count];

      for (var x = 0; x < ColumnDefinitions.Count; x++)
      {
        if (ColumnDefinitions[x].Width.GridUnitType != GridUnitType.Star || interpretStarAsAuto)
        {
          var maxWidth = ColumnDefinitions[x].Width.GridUnitType == GridUnitType.Pixel
            ? ColumnDefinitions[x].Width.Value
            : 0;
          // Учитываем MinWidth. MaxWidth учитывать специально не нужно, поскольку мы это
          // уже сделали при первом Measure, и DesiredSize не может быть больше MaxWidth
          if (ColumnDefinitions[x].MinWidth != null && maxWidth < ColumnDefinitions[x].MinWidth.Value)
          {
            maxWidth = ColumnDefinitions[x].MinWidth.Value;
          }

          for (var y = 0; y < RowDefinitions.Count; y++)
          {
            if (matrix[x, y] != null)
            {
              if (matrix[x, y].DesiredSize.Width > maxWidth)
              {
                maxWidth = matrix[x, y].DesiredSize.Width;
              }
            }
          }

          _columnsWidths[x] = maxWidth;
        }
      }

      _rowsHeights = new int[RowDefinitions.Count];

      for (var y = 0; y < RowDefinitions.Count; y++)
      {
        if (RowDefinitions[y].Height.GridUnitType != GridUnitType.Star || interpretStarAsAuto)
        {
          var maxHeight = RowDefinitions[y].Height.GridUnitType == GridUnitType.Pixel
            ? RowDefinitions[y].Height.Value
            : 0;
          if (RowDefinitions[y].MinHeight != null && maxHeight < RowDefinitions[y].MinHeight.Value)
          {
            maxHeight = RowDefinitions[y].MinHeight.Value;
          }

          for (int x = 0; x < ColumnDefinitions.Count; x++)
          {
            if (matrix[x, y] != null)
            {
              if (matrix[x, y].DesiredSize.Height > maxHeight)
              {
                maxHeight = matrix[x, y].DesiredSize.Height;
              }
            }
          }

          _rowsHeights[y] = maxHeight;
        }
      }

      // Теперь вычислим размеры Star-столбцов и Star-строк
      if (!interpretStarAsAuto)
      {
        var totalWidthStars = 0;
        foreach (var columnDefinition in ColumnDefinitions)
        {
          if (columnDefinition.Width.GridUnitType == GridUnitType.Star)
          {
            totalWidthStars += columnDefinition.Width.Value;
          }
        }

        var remainingWidth = Math.Max(0, availableSize.Width - _columnsWidths.Sum());
        for (var x = 0; x < ColumnDefinitions.Count; x++)
        {
          var columnDefinition = ColumnDefinitions[x];
          if (columnDefinition.Width.GridUnitType == GridUnitType.Star)
          {
            _columnsWidths[x] = remainingWidth * columnDefinition.Width.Value / totalWidthStars;
          }
        }

        var totalHeightStars = 0;
        foreach (var rowDefinition in RowDefinitions)
        {
          if (rowDefinition.Height.GridUnitType == GridUnitType.Star)
          {
            totalHeightStars += rowDefinition.Height.Value;
          }
        }

        var remainingHeight = Math.Max(0, availableSize.Height - _rowsHeights.Sum());
        for (var y = 0; y < RowDefinitions.Count; y++)
        {
          var rowDefinition = RowDefinitions[y];
          if (rowDefinition.Height.GridUnitType == GridUnitType.Star)
          {
            _rowsHeights[y] = remainingHeight * rowDefinition.Height.Value / totalHeightStars;
          }
        }
      }

      // Окончательный повторный вызов Measure для всех детей с уже определёнными размерами,
      // теми, которые будут использоваться при размещении
      for (var x = 0; x < ColumnDefinitions.Count; x++)
      {
        var width = _columnsWidths[x];
        for (var y = 0; y < RowDefinitions.Count; y++)
        {
          var height = _rowsHeights[y];
          if (matrix[x, y] != null)
          {
            matrix[x, y].Measure(new Size(width, height));
          }
        }
      }

      return new Size(_columnsWidths.Sum(), _rowsHeights.Sum());
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
      var currentX = 0;
      for (var x = 0; x < _columnsWidths.Length; x++)
      {
        var currentY = 0;
        for (var y = 0; y < _rowsHeights.Length; y++)
        {
          if (Children.Count > y * _columnsWidths.Length + x)
          {
            Children[y * _columnsWidths.Length + x].Arrange(new Rect(
              new Point(currentX, currentY),
              new Size(_columnsWidths[x], _rowsHeights[y])
            ));
          }

          currentY += _rowsHeights[y];
        }

        currentX += _columnsWidths[x];
      }

      return new Size(_columnsWidths.Sum(), _rowsHeights.Sum());
    }

    public override void Render(RenderingBuffer buffer)
    {
      buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 2);
    }
  }
}

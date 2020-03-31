using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Полностью описывает состояние лайаута контрола.
  /// </summary>
  internal class LayoutInfo : IEquatable<LayoutInfo>
  {
    public Size MeasureArgument;

    // если это поле не изменилось, то можно считать, что контрол не поменял своего размера
    public Size UnclippedDesiredSize;

    public Size DesiredSize;

    // по сути это arrangeArgument
    public Rect RenderSlotRect;
    public Size RenderSize;
    public Rect LayoutClip;
    public Vector ActualOffset;
    public LayoutValidity Validity = LayoutValidity.Nothing;

    public void CopyValuesFrom(LayoutInfo layoutInfo)
    {
      this.MeasureArgument = layoutInfo.MeasureArgument;
      this.UnclippedDesiredSize = layoutInfo.UnclippedDesiredSize;
      this.DesiredSize = layoutInfo.DesiredSize;
      this.RenderSlotRect = layoutInfo.RenderSlotRect;
      this.RenderSize = layoutInfo.RenderSize;
      this.LayoutClip = layoutInfo.LayoutClip;
      this.ActualOffset = layoutInfo.ActualOffset;
      this.Validity = layoutInfo.Validity;
    }

    public void ClearValues()
    {
      this.MeasureArgument = new Size();
      this.UnclippedDesiredSize = new Size();
      this.DesiredSize = new Size();
      this.RenderSlotRect = new Rect();
      this.RenderSize = new Size();
      this.LayoutClip = new Rect();
      this.ActualOffset = new Vector();
      this.Validity = LayoutValidity.Nothing;
    }

    // All members except 'validity'
    public bool Equals(LayoutInfo other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }

      if (ReferenceEquals(this, other))
      {
        return true;
      }

      return other.MeasureArgument.Equals(MeasureArgument)
             && other.UnclippedDesiredSize.Equals(UnclippedDesiredSize)
             && other.DesiredSize.Equals(DesiredSize)
             && other.RenderSlotRect.Equals(RenderSlotRect)
             && other.RenderSize.Equals(RenderSize)
             && other.LayoutClip.Equals(LayoutClip)
             && other.ActualOffset.Equals(ActualOffset);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      if (obj.GetType() != typeof(LayoutInfo))
      {
        return false;
      }

      return Equals((LayoutInfo) obj);
    }

    public override int GetHashCode()
    {
      var hashCode = -474869825;
      hashCode = hashCode * -1521134295 + MeasureArgument.GetHashCode();
      hashCode = hashCode * -1521134295 + UnclippedDesiredSize.GetHashCode();
      hashCode = hashCode * -1521134295 + DesiredSize.GetHashCode();
      hashCode = hashCode * -1521134295 + RenderSlotRect.GetHashCode();
      hashCode = hashCode * -1521134295 + RenderSize.GetHashCode();
      hashCode = hashCode * -1521134295 + LayoutClip.GetHashCode();
      hashCode = hashCode * -1521134295 + ActualOffset.GetHashCode();
      hashCode = hashCode * -1521134295 + Validity.GetHashCode();
      return hashCode;
    }
  }
}

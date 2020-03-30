using System;
using ConsoleFramework.Core;

namespace ConsoleFramework.Controls
{
  /// <summary>
  /// Полностью описывает состояние лайаута контрола.
  /// </summary>
  internal class LayoutInfo : IEquatable<LayoutInfo> {
    public Size measureArgument;
    // если это поле не изменилось, то можно считать, что контрол не поменял своего размера
    public Size unclippedDesiredSize;
    public Size desiredSize;
    // по сути это arrangeArgument
    public Rect renderSlotRect;
    public Size renderSize;
    public Rect layoutClip;
    public Vector actualOffset;
    public LayoutValidity validity = LayoutValidity.Nothing;

    public void CopyValuesFrom(LayoutInfo layoutInfo) {
      this.measureArgument = layoutInfo.measureArgument;
      this.unclippedDesiredSize = layoutInfo.unclippedDesiredSize;
      this.desiredSize = layoutInfo.desiredSize;
      this.renderSlotRect = layoutInfo.renderSlotRect;
      this.renderSize = layoutInfo.renderSize;
      this.layoutClip = layoutInfo.layoutClip;
      this.actualOffset = layoutInfo.actualOffset;
      this.validity = layoutInfo.validity;
    }

    public void ClearValues() {
      this.measureArgument = new Size();
      this.unclippedDesiredSize = new Size();
      this.desiredSize = new Size();
      this.renderSlotRect = new Rect();
      this.renderSize = new Size();
      this.layoutClip = new Rect();
      this.actualOffset = new Vector();
      this.validity = LayoutValidity.Nothing;
    }

    // All members except 'validity'
    public bool Equals(LayoutInfo other) {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return other.measureArgument.Equals(measureArgument)
             && other.unclippedDesiredSize.Equals(unclippedDesiredSize)
             && other.desiredSize.Equals(desiredSize)
             && other.renderSlotRect.Equals(renderSlotRect)
             && other.renderSize.Equals(renderSize)
             && other.layoutClip.Equals(layoutClip)
             && other.actualOffset.Equals(actualOffset);
    }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != typeof (LayoutInfo)) return false;
      return Equals((LayoutInfo) obj);
    }

    public override int GetHashCode(){
      int hashCode = -474869825;
      hashCode = hashCode * -1521134295 + measureArgument.GetHashCode();
      hashCode = hashCode * -1521134295 + unclippedDesiredSize.GetHashCode();
      hashCode = hashCode * -1521134295 + desiredSize.GetHashCode();
      hashCode = hashCode * -1521134295 + renderSlotRect.GetHashCode();
      hashCode = hashCode * -1521134295 + renderSize.GetHashCode();
      hashCode = hashCode * -1521134295 + layoutClip.GetHashCode();
      hashCode = hashCode * -1521134295 + actualOffset.GetHashCode();
      hashCode = hashCode * -1521134295 + validity.GetHashCode();
      return hashCode;
    }
  }
}

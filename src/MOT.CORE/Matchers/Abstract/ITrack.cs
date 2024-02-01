using System.Collections.Generic;
using System.Drawing;
using MOT.Core.YOLO;

namespace MOT.Core.Matchers.Abstract
{
    public interface ITrack
    {
        public int Id { get; set; }
        public Color Color { get; }
        public IReadOnlyList<RectangleF> History { get; }
        public RectangleF CurrentBoundingBox { get; }
        public DetectionObjectType DetectionObjectType { get; }

        public void RegisterTracked(RectangleF trackedRectangle);
    }
}

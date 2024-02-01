using System.Drawing;

namespace MOT.Core.YOLO
{
    public interface IPrediction
    {
        public DetectionObjectType DetectionObjectType { get; }
        public Rectangle CurrentBoundingBox { get; }
        public float Confidence { get; }
    }
}

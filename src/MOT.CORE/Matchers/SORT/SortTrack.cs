using System.Drawing;
using MOT.Core.Matchers.Abstract;
using MOT.Core.Matchers.Base;

namespace MOT.Core.Matchers.SORT
{
    public class SortTrack : TrackDecorator
    {
        public SortTrack(ITrack track) : base(track) {  }

        public RectangleF PredictedBoundingBox { get; set; }

        protected override void RegisterTrackedInternal(RectangleF trackedRectangle)
        {
            WrappedTrack.RegisterTracked(trackedRectangle);
        }
    }
}

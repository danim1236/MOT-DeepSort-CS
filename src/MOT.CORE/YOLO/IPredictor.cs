using System;
using System.Collections.Generic;
using System.Drawing;

namespace MOT.Core.YOLO
{
    public interface IPredictor : IDisposable
    {
        public abstract IReadOnlyList<IPrediction> Predict(Bitmap image, float targetConfidence, params DetectionObjectType[] targetDetectionTypes);
    }
}

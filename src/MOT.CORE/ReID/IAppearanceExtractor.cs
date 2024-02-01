using System;
using System.Collections.Generic;
using System.Drawing;
using MOT.Core.Utils.DataStructs;
using MOT.Core.YOLO;

namespace MOT.Core.ReID
{
    public interface IAppearanceExtractor : IDisposable
    {
        public abstract IReadOnlyList<Vector> Predict(Bitmap image, IPrediction[] detectedBounds);
    }
}

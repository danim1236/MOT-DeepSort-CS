using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using MOT.Core.Matchers.Abstract;
using MOT.Core.Matchers.Base;
using MOT.Core.Matchers.Trackers;
using MOT.Core.Utils;
using MOT.Core.Utils.Algorithms.Hungarian;
using MOT.Core.Utils.Pool;
using MOT.Core.YOLO;

namespace MOT.Core.Matchers.SORT
{
    public class SortMatcher : Matcher
    {
        private readonly Pool<KalmanTracker<SortTrack>> _pool;

        private List<PoolObject<KalmanTracker<SortTrack>>> _trackers = new List<PoolObject<KalmanTracker<SortTrack>>>();

        public SortMatcher(float iouThreshold = 0.3f, int maxMisses = 15,
            int minStreak = 3, int poolCapacity = 50)
            : base(maxMisses, minStreak)
        {
            IouThreshold = iouThreshold;
            _pool = new Pool<KalmanTracker<SortTrack>>(poolCapacity);
        }

        public float IouThreshold { get; private init; }

        public override IReadOnlyList<ITrack> Track(Bitmap frame, IPrediction[] detectedObjects)
        {
            if (_trackers.Count == 0)
                return Init(detectedObjects);

            PredictBoundingBoxes();

            (List<(int TrackIndex, int DetectionIndex)> matchedPairs, var unmatched) = MatchDetections(detectedObjects);

            UpdateMatched(matchedPairs, detectedObjects);

            foreach (var t in unmatched)
                AddNewTrack(detectedObjects, t);

            var tracks = ConfirmTracks<KalmanTracker<SortTrack>, SortTrack>(_trackers);
            RemoveOutdatedTracks<KalmanTracker<SortTrack>, SortTrack>(ref _trackers);

            return tracks;
        }

        public override void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<SortTrack> Init(IReadOnlyList<IPrediction> detectedObjects)
        {
            for (int i = 0; i < detectedObjects.Count; i++)
                AddNewTrack(detectedObjects, i);

            return new List<SortTrack>();
        }

        private void AddNewTrack(IReadOnlyList<IPrediction> detectedObjects, int index)
        {
            PoolObject<KalmanTracker<SortTrack>> tracker = _pool.Get();
            SortTrack track = new SortTrack(new Track(detectedObjects[index].CurrentBoundingBox,
                                                    detectedObjects[index].DetectionObjectType));

            InitNewTrack(tracker.Object, track);

            _trackers.Add(tracker);
        }

        private void PredictBoundingBoxes()
        {
            var toRemove = new List<PoolObject<KalmanTracker<SortTrack>>>();

            for (int i = 0; i < _trackers.Count; i++)
            {
                RectangleF predictedBounds = _trackers[i].Object.Predict();

                if (predictedBounds.X >= 0 && predictedBounds.Y >= 0)
                {
                    _trackers[i].Object.Track.PredictedBoundingBox = predictedBounds;
                    continue;
                }

                toRemove.Add(_trackers[i]);
                _trackers[i].Release();
            }

            if (toRemove.Count != 0)
                _trackers = _trackers.Except(toRemove).ToList();
        }

        private void UpdateMatched(IReadOnlyList<(int TrackIndex, int DetectionIndex)> matchedPairs, IReadOnlyList<IPrediction> detectedObjects)
        {
            for (int i = 0; i < matchedPairs.Count; i++)
            {
                int trackIndex = matchedPairs[i].TrackIndex;
                int detectionIndex = matchedPairs[i].DetectionIndex;

                _trackers[trackIndex].Object.Track.RegisterTracked(detectedObjects[detectionIndex].CurrentBoundingBox);
                _trackers[trackIndex].Object.Update(detectedObjects[detectionIndex].CurrentBoundingBox);
            }
        }

        private (List<(int, int)> MatchedPairs, List<int> UnmatchedDetectionIndexes) MatchDetections(IReadOnlyList<IPrediction> detections)
        {
            float[,] ioUMatrix = new float[_trackers.Count, detections.Count];

            for (int i = 0; i < _trackers.Count; i++)
                for (int j = 0; j < detections.Count; j++)
                    ioUMatrix[i, j] = Metrics.IntersectionOverUnionLoss(_trackers[i].Object.Track.PredictedBoundingBox, detections[j].CurrentBoundingBox);

            HungarianAlgorithm<float> hungarianAlgorithm = new HungarianAlgorithm<float>(ioUMatrix);
            int[] assignment = hungarianAlgorithm.Solve();

            var allItemIndexes = new List<int>();
            var matched = new List<int>();
            var matchedPairs = new List<(int, int)>();
            var unmatched = new List<int>();

            if (detections.Count > _trackers.Count)
            {
                for (int i = 0; i < detections.Count; i++)
                    allItemIndexes.Add(i);

                for (int i = 0; i < _trackers.Count; i++)
                    matched.Add(assignment[i]);

                unmatched = allItemIndexes.Except(matched).ToList();
            }

            for (int i = 0; i < assignment.Length; i++)
            {
                if (assignment[i] == -1)
                    continue;

                if (1 - ioUMatrix[i, assignment[i]] < IouThreshold)
                {
                    unmatched.Add(assignment[i]);
                    continue;
                }

                matchedPairs.Add((i, assignment[i]));
            }

            return (matchedPairs, unmatched);
        }
    }
}

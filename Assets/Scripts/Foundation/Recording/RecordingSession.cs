using Ankhora.Domain.Model;
using Ankhora.Domain.Recording;
using Ankhora.Foundation.Persistence;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// The shared, UI-agnostic core of a hands recording: it drives the <see cref="TimelineRecorder"/>
    /// from an <see cref="IHandPoseSource"/>, captures each hand's <see cref="HandSkeleton"/> once (so
    /// replay can rebuild a faithful rig — the step a recorder that forgets it produces an unreplayable
    /// file), then wraps the take in a <see cref="Masterclass"/> and persists it via a
    /// <see cref="MasterclassStore"/>. Different triggers (auto-countdown, controller button, hand pinch)
    /// are thin shells that call <see cref="Begin"/> / <see cref="Tick"/> / <see cref="SaveTo"/>.
    /// </summary>
    public class RecordingSession
    {
        private readonly IHandPoseSource _source;
        private readonly IHandSkeletonSource _skeletonSource;
        private readonly TimelineRecorder _recorder;

        private HandPose _left;   // reused buffers — the source reuses its bone arrays across frames
        private HandPose _right;
        private HandSkeleton _leftSkeleton;
        private HandSkeleton _rightSkeleton;

        public RecordingSession(IHandPoseSource source, float sampleRateHz)
        {
            _source = source;
            _skeletonSource = source as IHandSkeletonSource;
            _recorder = new TimelineRecorder(sampleRateHz);
        }

        public bool IsRecording => _recorder.IsRecording;

        public void Begin(float now)
        {
            _leftSkeleton = null;
            _rightSkeleton = null;
            _recorder.Begin(now);
        }

        /// <summary>Sample one frame: capture the skeleton if not yet captured, then push head + both hands.</summary>
        public void Tick(float now)
        {
            CaptureSkeleton();
            _source.TryGetHead(out Pose head);
            if (!_source.TryGetHand(false, ref _left)) _left.boneRotations = null;
            if (!_source.TryGetHand(true, ref _right)) _right.boneRotations = null;
            _recorder.Push(now, head, _left, _right);
        }

        /// <summary>
        /// Finish the take, stamp the captured per-hand skeletons onto the timeline, and persist it.
        /// Returns whether the write succeeded; <paramref name="frameCount"/> is the recorded length.
        /// </summary>
        public bool SaveTo(MasterclassStore store, float now, out int frameCount, out string error)
        {
            Timeline timeline = _recorder.Finish(now);
            timeline.leftSkeleton = _leftSkeleton;
            timeline.rightSkeleton = _rightSkeleton;
            frameCount = timeline.frames.Count;

            var masterclass = new Masterclass { id = "mc-local", title = "Local recording" };
            masterclass.chapters.Add(new Chapter { id = "ch-1", timeline = timeline });
            return store.Save(masterclass, out error);
        }

        public int LeftBoneCount => _leftSkeleton != null && _leftSkeleton.IsValid ? _leftSkeleton.boneParents.Length : 0;
        public int RightBoneCount => _rightSkeleton != null && _rightSkeleton.IsValid ? _rightSkeleton.boneParents.Length : 0;

        private void CaptureSkeleton()
        {
            if (_skeletonSource == null)
                return;
            // Left and right are mirrored skeletons — capture each independently.
            if ((_leftSkeleton == null || !_leftSkeleton.IsValid) &&
                _skeletonSource.TryGetSkeleton(false, out HandSkeleton left) && left.IsValid)
                _leftSkeleton = left;
            if ((_rightSkeleton == null || !_rightSkeleton.IsValid) &&
                _skeletonSource.TryGetSkeleton(true, out HandSkeleton right) && right.IsValid)
                _rightSkeleton = right;
        }
    }
}

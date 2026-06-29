using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>
    /// Builds a <see cref="Timeline"/> by sampling head + hand poses at a fixed rate on one
    /// monotonic clock, independent of frame rate. Pure and deterministic (no engine state, no
    /// wall-clock) so the capture cadence is EditMode-testable without a headset. The caller feeds
    /// it the current time + poses every frame via <see cref="Push"/>; it decides when to emit a
    /// <see cref="PoseFrame"/>.
    /// </summary>
    public class TimelineRecorder
    {
        private readonly float _sampleInterval;
        private Timeline _timeline;
        private float _startTime;
        private float _nextSampleTime;
        private bool _recording;

        /// <param name="sampleRateHz">Frames per second to capture (e.g. 30).</param>
        public TimelineRecorder(float sampleRateHz)
        {
            _sampleInterval = sampleRateHz > 0f ? 1f / sampleRateHz : 0f;
        }

        public bool IsRecording => _recording;

        /// <summary>Start a fresh recording; <paramref name="now"/> is the zero of the timeline clock.</summary>
        public void Begin(float now)
        {
            _timeline = new Timeline();
            _startTime = now;
            _nextSampleTime = now;   // emit the first frame immediately at t = 0
            _recording = true;
        }

        /// <summary>
        /// Call every frame while recording with the current clock + poses. Emits a frame each time
        /// the fixed interval has elapsed; otherwise does nothing.
        /// </summary>
        public void Push(float now, in Pose head, in HandPose left, in HandPose right)
        {
            if (!_recording || now < _nextSampleTime)
                return;

            _timeline.frames.Add(new PoseFrame
            {
                t = now - _startTime,
                head = head,
                leftHand = CloneHand(left),
                rightHand = CloneHand(right),
            });

            _nextSampleTime += _sampleInterval;
            // If a frame hitched and we fell behind, resync to avoid a burst of catch-up frames.
            if (_nextSampleTime < now)
                _nextSampleTime = now + _sampleInterval;
        }

        // Capture sources reuse one bone array per hand across frames, so the recorder must snapshot
        // the rotations AND positions per frame — storing the live reference would alias every frame to
        // the last pose. Capture runs at the sample rate (~30 Hz), not the replay hot loop, so a per-frame
        // clone is fine.
        private static HandPose CloneHand(in HandPose hand)
        {
            return new HandPose
            {
                root = hand.root,
                boneRotations = hand.boneRotations == null
                    ? null
                    : (Quaternion[])hand.boneRotations.Clone(),
                boneLocalPositions = hand.boneLocalPositions == null
                    ? null
                    : (Vector3[])hand.boneLocalPositions.Clone(),
            };
        }

        /// <summary>Stop recording and return the finished timeline with its duration set.</summary>
        public Timeline Finish(float now)
        {
            _recording = false;
            Timeline result = _timeline;
            if (result != null)
                result.durationSeconds = now - _startTime;
            _timeline = null;
            return result;
        }
    }
}

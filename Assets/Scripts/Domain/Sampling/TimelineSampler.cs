using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Domain.Sampling
{
    /// <summary>
    /// Reads a <see cref="Timeline"/> at an arbitrary time, interpolating between the recorded
    /// frames so ghost motion stays smooth at any display rate. Pure, deterministic, and
    /// allocation-free (value-type frames + structs) so it is safe in the replay hot loop.
    /// Kept off the <see cref="Timeline"/> DTO so the model layer stays strictly data.
    /// </summary>
    /// <remarks>
    /// Precondition: <see cref="Timeline.frames"/> are sorted by ascending <see cref="PoseFrame.t"/>.
    /// The recorder guarantees this (it writes from a single monotonic clock); the sampler does not
    /// re-sort on the hot path. When a per-bone hand sampler is added for replay, it must write into
    /// a caller-owned, pre-allocated <c>Quaternion[]</c> rather than returning a fresh array, to keep
    /// the 90 Hz playback loop allocation-free (see the replay-feature notes).
    /// </remarks>
    public static class TimelineSampler
    {
        /// <summary>
        /// Returns the head pose at time <paramref name="t"/> (seconds): the exact frame pose at a
        /// frame time, a linear/spherical interpolation between the two bracketing frames in
        /// between, and the first/last frame pose when <paramref name="t"/> is outside the range.
        /// Runs in O(log n) via a binary search over the sorted frames.
        /// </summary>
        public static Pose SampleHead(Timeline timeline, float t)
        {
            var frames = timeline.frames;
            if (frames == null || frames.Count == 0)
                return default;

            if (t <= frames[0].t)
                return frames[0].head;

            int lastIndex = frames.Count - 1;
            PoseFrame last = frames[lastIndex];
            if (t >= last.t)
                return last.head;

            // Binary search for the [lo, hi] frame pair bracketing t (frames are sorted ascending).
            int lo = 0;
            int hi = lastIndex;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (frames[mid].t <= t)
                    lo = mid;
                else
                    hi = mid;
            }

            PoseFrame a = frames[lo];
            PoseFrame b = frames[hi];
            float span = b.t - a.t;
            float u = span > 0f ? (t - a.t) / span : 0f;
            return new Pose(
                Vector3.LerpUnclamped(a.head.position, b.head.position, u),
                Quaternion.SlerpUnclamped(a.head.rotation, b.head.rotation, u));
        }
    }
}

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
    public static class TimelineSampler
    {
        /// <summary>
        /// Returns the head pose at time <paramref name="t"/> (seconds): the exact frame pose at a
        /// frame time, a linear/spherical interpolation between the two bracketing frames in
        /// between, and the first/last frame pose when <paramref name="t"/> is outside the range.
        /// </summary>
        public static Pose SampleHead(Timeline timeline, float t)
        {
            var frames = timeline.frames;
            if (frames == null || frames.Count == 0)
                return default;

            if (t <= frames[0].t)
                return frames[0].head;

            PoseFrame last = frames[frames.Count - 1];
            if (t >= last.t)
                return last.head;

            for (int i = 0; i < frames.Count - 1; i++)
            {
                PoseFrame a = frames[i];
                PoseFrame b = frames[i + 1];
                if (t >= a.t && t <= b.t)
                {
                    float span = b.t - a.t;
                    float u = span > 0f ? (t - a.t) / span : 0f;
                    return new Pose(
                        Vector3.LerpUnclamped(a.head.position, b.head.position, u),
                        Quaternion.SlerpUnclamped(a.head.rotation, b.head.rotation, u));
                }
            }

            return last.head; // unreachable: t is within [first, last] here.
        }
    }
}

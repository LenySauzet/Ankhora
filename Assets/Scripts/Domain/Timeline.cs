using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ankhora.Domain
{
    /// <summary>
    /// A Chapter's recorded timeline: head/hand pose frames sampled at a fixed rate on one
    /// monotonic clock. Replay reads it via <see cref="Sample"/>, interpolating between frames
    /// so ghost motion is smooth at any display rate.
    /// </summary>
    [Serializable]
    public class Timeline
    {
        public float durationSeconds;

        public List<PoseFrame> frames = new List<PoseFrame>();

        /// <summary>
        /// Returns the pose at time <paramref name="t"/> (seconds), interpolating between the
        /// two bracketing frames and clamping outside the recorded range. Allocation-free
        /// (value-type frames + structs) so it is safe in the replay hot loop.
        /// </summary>
        public Pose Sample(float t)
        {
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

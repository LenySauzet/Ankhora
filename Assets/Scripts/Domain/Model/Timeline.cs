using System;
using System.Collections.Generic;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// A Chapter's recorded timeline: head/hand pose frames sampled at a fixed rate on one
    /// monotonic clock, plus the spatial pins shown during playback. Pure serialisable data;
    /// reading it back (interpolating between frames) is the job of
    /// <c>Ankhora.Domain.Sampling.TimelineSampler</c>.
    /// </summary>
    [Serializable]
    public class Timeline
    {
        /// <summary>Authoritative recording length. Sampling clamps to frame times, not this.</summary>
        public float durationSeconds;

        public List<PoseFrame> frames = new List<PoseFrame>();

        public List<Pin> pins = new List<Pin>();

        /// <summary>
        /// Per-hand bone topology + rest pose, captured once for this recording so replay can rebuild a
        /// faithful articulated rig. Left and right are MIRRORED skeletons, so each must be stored and
        /// applied separately — using one hand's bind poses for the other scatters its joints. Null/
        /// invalid for legacy recordings or a hand that was never tracked.
        /// </summary>
        public HandSkeleton leftSkeleton;

        public HandSkeleton rightSkeleton;
    }
}

using System;
using UnityEngine;

namespace Ankhora.Domain
{
    /// <summary>
    /// One sampled instant of the recording, on the Chapter's single monotonic clock.
    /// A value type: a 2-minute recording is many frames, and replay samples them in a hot
    /// loop — keep it allocation-free. Hand poses are added in a later slice (after the
    /// OVRSkeleton bone set is confirmed against the live Meta API).
    /// </summary>
    [Serializable]
    public struct PoseFrame
    {
        /// <summary>Seconds from the start of the Chapter timeline.</summary>
        public float t;

        /// <summary>Head pose (position + rotation) at <see cref="t"/>.</summary>
        public Pose head;
    }
}

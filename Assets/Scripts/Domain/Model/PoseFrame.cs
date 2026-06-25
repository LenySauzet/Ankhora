using System;
using UnityEngine;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// One sampled instant of the recording, on the Chapter's single monotonic clock.
    /// A value type: a 2-minute recording is many frames, and replay samples them in a hot
    /// loop — keep it allocation-free (the hand bone arrays are the only heap part).
    /// </summary>
    [Serializable]
    public struct PoseFrame
    {
        /// <summary>Seconds from the start of the Chapter timeline.</summary>
        public float t;

        /// <summary>Head pose (position + rotation) at <see cref="t"/>.</summary>
        public Pose head;

        /// <summary>Left hand at <see cref="t"/>; empty <c>boneRotations</c> means "not tracked".</summary>
        public HandPose leftHand;

        /// <summary>Right hand at <see cref="t"/>; empty <c>boneRotations</c> means "not tracked".</summary>
        public HandPose rightHand;
    }
}

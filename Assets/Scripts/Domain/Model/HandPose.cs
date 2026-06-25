using System;
using UnityEngine;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// One hand in one frame: a wrist <see cref="root"/> pose plus per-bone local rotations.
    /// The array length and bone order are fixed by the capture source — Meta's
    /// <c>OVRSkeleton</c> exposes 19 skinnable bones (Hand_WristRoot..Hand_Pinky3); OpenXR
    /// (Unity XR Hands) uses 26 joints (XRHandJointID). The DTO stays count-agnostic so the
    /// recorder owns that choice and replay reads it back with the same ordering. An empty/null
    /// <see cref="boneRotations"/> means "no hand tracked this frame".
    /// </summary>
    [Serializable]
    public struct HandPose
    {
        /// <summary>Wrist root pose in tracking space (position + rotation).</summary>
        public Pose root;

        /// <summary>Local bone rotations, ordered by the capture source's bone enum.</summary>
        public Quaternion[] boneRotations;
    }
}

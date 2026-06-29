using System;
using UnityEngine;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// One hand in one frame: a wrist <see cref="root"/> pose plus per-bone local rotations AND local
    /// positions. The array length and bone order are fixed by the capture source — Meta's
    /// <c>OVRSkeleton</c> exposes 19 skinnable bones (Hand_WristRoot..Hand_Pinky3); OpenXR
    /// (Unity XR Hands) uses 26 joints (XRHandJointID). The DTO stays count-agnostic so the
    /// recorder owns that choice and replay reads it back with the same ordering. An empty/null
    /// <see cref="boneRotations"/> means "no hand tracked this frame".
    /// <para>
    /// Both rotations and positions are captured per frame because Meta's OpenXR hand path recomputes
    /// each bone's LOCAL position every frame from the tracked joint translations
    /// (<c>OVRSkeleton.cs</c>: <c>localPosition = parentRot⁻¹ · (bonePos − parentPos)</c>), fitted to the
    /// user's actual hand. Replaying rotations onto the generic rest bind offsets diverges from the live
    /// hand (a ~cm fingertip offset); replaying the captured per-frame positions reproduces it exactly.
    /// </para>
    /// </summary>
    [Serializable]
    public struct HandPose
    {
        /// <summary>Wrist root pose in tracking space (position + rotation).</summary>
        public Pose root;

        /// <summary>Local bone rotations, ordered by the capture source's bone enum.</summary>
        public Quaternion[] boneRotations;

        /// <summary>
        /// Per-frame local bone positions (each bone relative to its parent), same order/length as
        /// <see cref="boneRotations"/>. Null on recordings captured before positions were stored; replay
        /// then falls back to the rest bind offsets.
        /// </summary>
        public Vector3[] boneLocalPositions;
    }
}

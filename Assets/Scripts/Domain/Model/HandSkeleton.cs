using System;
using UnityEngine;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// The articulated structure of one captured hand: parent links + each bone's rest LOCAL pose.
    /// Captured once per hand at the start of a take (the structure is constant) and stored per hand
    /// on the <see cref="Timeline"/> — left and right are MIRRORED, so each needs its own descriptor.
    /// Replay rebuilds a faithful rest skeleton from it and applies the per-frame
    /// <see cref="HandPose.boneRotations"/> via forward kinematics; without it, finger motion would be
    /// distorted.
    /// </summary>
    [Serializable]
    public class HandSkeleton
    {
        /// <summary>Parent bone index per bone; a value &lt; 0 (or out of range) marks a root. Length == bone count.</summary>
        public int[] boneParents;

        /// <summary>Rest LOCAL pose per bone (position + rotation relative to its parent). Length == bone count.</summary>
        public Pose[] boneBindPoses;

        /// <summary>True when both arrays are present, non-empty, and the same length.</summary>
        public bool IsValid =>
            boneParents != null && boneBindPoses != null &&
            boneParents.Length == boneBindPoses.Length && boneParents.Length > 0;
    }
}

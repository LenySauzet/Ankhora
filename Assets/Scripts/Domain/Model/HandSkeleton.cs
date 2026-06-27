using System;
using UnityEngine;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// The articulated structure of a captured hand, recorded ONCE per timeline (both hands share the
    /// same bone topology, so a single descriptor suffices). It lets replay rebuild a faithful rest
    /// skeleton — parent links + each bone's rest LOCAL pose — onto which the per-frame
    /// <see cref="HandPose.boneRotations"/> are applied via forward kinematics. Without it, replay
    /// would have to guess the skeleton and finger motion would look distorted.
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

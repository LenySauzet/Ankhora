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

        /// <summary>
        /// Index of the topological root bone — the one with no valid parent (a parent index out of
        /// <c>[0, count)</c>; OVRPlugin uses a negative or out-of-range sentinel). The OpenXR 26-joint hand
        /// is rooted at the WRIST (index 1), not the palm (index 0); assuming index 0 mis-anchors the whole
        /// rig by the palm→wrist distance (~70 mm). Returns 0 when there is no clear single root (legacy/
        /// degenerate skeletons), preserving the old behaviour.
        /// </summary>
        public int RootBoneIndex => FindRootBoneIndex(boneParents);

        /// <summary>Pure root finder shared by capture and replay so both anchor the same bone.</summary>
        public static int FindRootBoneIndex(int[] parents)
        {
            if (parents == null)
                return 0;
            int n = parents.Length;
            for (int i = 0; i < n; i++)
                if (parents[i] < 0 || parents[i] >= n)
                    return i;
            return 0;
        }
    }
}

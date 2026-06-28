using Ankhora.Domain.Model;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The OpenXR 26-joint hand skeleton is rooted at the WRIST (index 1), not the palm (index 0):
    /// the wrist is the bone with no valid parent, the palm is a child of it. Anchoring capture/replay
    /// at index 0 mis-roots the whole rig (~70 mm offset). <see cref="HandSkeleton.RootBoneIndex"/> finds
    /// the real root from the parent links.
    /// </summary>
    public class HandSkeletonRootTests
    {
        private static HandSkeleton Skel(int[] parents)
        {
            var poses = new Pose[parents.Length];
            for (int i = 0; i < poses.Length; i++) poses[i] = Pose.identity;
            return new HandSkeleton { boneParents = parents, boneBindPoses = poses };
        }

        [Test]
        public void RootBoneIndex_OpenXRLayout_FindsWristNotPalm()
        {
            // palm(0) -> wrist(1); wrist(1) is the root (-1); thumb metacarpal(2) -> wrist(1).
            var s = Skel(new[] { 1, -1, 1, 2 });
            Assert.AreEqual(1, s.RootBoneIndex);
        }

        [Test]
        public void RootBoneIndex_LegacyLayout_RootIsZero()
        {
            // Legacy hand where index 0 is the root.
            var s = Skel(new[] { -1, 0, 1 });
            Assert.AreEqual(0, s.RootBoneIndex);
        }

        [Test]
        public void RootBoneIndex_OutOfRangeParentSentinel_TreatedAsRoot()
        {
            // OVRPlugin can report a large/out-of-range sentinel for the root rather than a negative value.
            var s = Skel(new[] { 1, 99, 1 });
            Assert.AreEqual(1, s.RootBoneIndex);
        }

        [Test]
        public void RootBoneIndex_NoBones_ReturnsZero()
        {
            var s = new HandSkeleton { boneParents = new int[0], boneBindPoses = new Pose[0] };
            Assert.AreEqual(0, s.RootBoneIndex);
        }
    }
}

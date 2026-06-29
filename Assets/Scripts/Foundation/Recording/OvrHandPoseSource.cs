using Ankhora.Domain.Model;
using Ankhora.Domain.Spatial;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Reads the live Meta hand skeletons + head each frame as <see cref="IHandPoseSource"/> for the
    /// recorder. Device-only: OVRSkeleton produces no data in macOS Editor Play Mode.
    /// <para>
    /// The wrist <see cref="HandPose.root"/> and the head are captured in the camera rig's
    /// <see cref="_trackingSpace"/> frame (not bone-local), so a recording preserves the hand's gross
    /// motion through the room and survives recentering. Per-bone rotations stay LOCAL (each bone
    /// relative to its parent), which is exactly what an articulated replay rig re-applies via forward
    /// kinematics — independent of where the hand is.
    /// </para>
    /// </summary>
    public class OvrHandPoseSource : MonoBehaviour, IHandPoseSource, IHandSkeletonSource
    {
        [SerializeField] private OVRSkeleton _leftSkeleton;
        [SerializeField] private OVRSkeleton _rightSkeleton;
        [SerializeField] private Transform _centerEye;
        [Tooltip("Camera rig tracking space — the stable frame poses are stored relative to. " +
                 "Usually OVRCameraRig/TrackingSpace. If unset, world space is used.")]
        [SerializeField] private Transform _trackingSpace;

        private Pose ReferencePose =>
            _trackingSpace != null ? new Pose(_trackingSpace.position, _trackingSpace.rotation) : Pose.identity;

        public bool TryGetHead(out Pose head)
        {
            if (_centerEye == null) { head = default; return false; }
            head = PoseSpace.RelativeTo(ReferencePose, new Pose(_centerEye.position, _centerEye.rotation));
            return true;
        }

        public bool TryGetHand(bool rightHand, ref HandPose pose)
        {
            OVRSkeleton skeleton = rightHand ? _rightSkeleton : _leftSkeleton;
            if (skeleton == null || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence)
                return false;

            var bones = skeleton.Bones;                 // IList<OVRBone>
            int count = bones.Count;
            if (count == 0)
                return false;

            // Anchor on the SKELETON-ROOT FRAME (the parent of the topological root bone — Meta's
            // "_bonesGO", which OVRSkeleton drives to the hand RootPose), NOT on a bone. Replay rebuilds
            // the full bone hierarchy under this frame exactly like the live skeleton, so the skinned mesh
            // matches. The OpenXR hand's root bone is the WRIST (index 1), not the palm (index 0); reading
            // bones[0] mis-anchors the whole hand by ~70 mm, and reading the wrist bone itself double-counts
            // the wrist's local pose against the rebuilt rig.
            int rootIdx = RootBoneIndex(bones);
            Transform rootBone = bones[rootIdx].Transform;
            Transform frame = rootBone.parent != null ? rootBone.parent : rootBone;
            // Read its WORLD transform (the frame carries the gross motion through the room) and reframe it
            // so the recording survives recentering.
            pose.root = PoseSpace.RelativeTo(ReferencePose, new Pose(frame.position, frame.rotation));

            if (pose.boneRotations == null || pose.boneRotations.Length != count)
                pose.boneRotations = new Quaternion[count];
            if (pose.boneLocalPositions == null || pose.boneLocalPositions.Length != count)
                pose.boneLocalPositions = new Vector3[count];
            // Capture BOTH local rotation and local position per frame: Meta's OpenXR hand path recomputes
            // each bone's local position every frame (fitted to the user's hand), so rotation-only replay
            // onto the generic rest offsets diverges from the live hand. See HandPose.boneLocalPositions.
            for (int i = 0; i < count; i++)
            {
                Transform t = bones[i].Transform;
                pose.boneRotations[i] = t.localRotation;
                pose.boneLocalPositions[i] = t.localPosition;
            }
            return true;
        }

        // The topological root = the bone whose ParentBoneIndex is invalid (out of [0, count)). Mirrors
        // HandSkeleton.FindRootBoneIndex so capture and replay anchor the same bone. Falls back to 0.
        private static int RootBoneIndex(System.Collections.Generic.IList<OVRBone> bones)
        {
            int count = bones.Count;
            for (int i = 0; i < count; i++)
            {
                int p = bones[i].ParentBoneIndex;
                if (p < 0 || p >= count)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Reads the rest skeleton (parent links + each bone's local bind pose) from the live
        /// OVRSkeleton's <c>BindPoses</c>. Available once the skeleton is initialised on device.
        /// </summary>
        public bool TryGetSkeleton(bool rightHand, out HandSkeleton skeleton)
        {
            skeleton = null;
            OVRSkeleton ovr = rightHand ? _rightSkeleton : _leftSkeleton;
            if (ovr == null || !ovr.IsInitialized)
                return false;

            var binds = ovr.BindPoses;          // IList<OVRBone>; parented at rest, index 0 == wrist
            if (binds == null || binds.Count == 0)
                return false;

            int count = binds.Count;
            var parents = new int[count];
            var poses = new Pose[count];
            for (int i = 0; i < count; i++)
            {
                OVRBone bone = binds[i];
                parents[i] = bone.ParentBoneIndex;  // OVRPlugin reports a sentinel (< 0) for the root
                Transform t = bone.Transform;
                poses[i] = t != null ? new Pose(t.localPosition, t.localRotation) : Pose.identity;
            }

            skeleton = new HandSkeleton { boneParents = parents, boneBindPoses = poses };
            return true;
        }
    }
}

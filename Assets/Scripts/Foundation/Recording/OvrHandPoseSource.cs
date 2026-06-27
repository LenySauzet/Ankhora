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

            var bones = skeleton.Bones;                 // IList<OVRBone>; index 0 == Hand_WristRoot
            int count = bones.Count;
            if (count == 0)
                return false;

            Transform wrist = bones[0].Transform;
            // Wrist in tracking space: the bone's own local pose is ~constant (the rig anchor carries
            // the gross motion), so we must read its WORLD transform and reframe it.
            pose.root = PoseSpace.RelativeTo(ReferencePose, new Pose(wrist.position, wrist.rotation));

            if (pose.boneRotations == null || pose.boneRotations.Length != count)
                pose.boneRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
                pose.boneRotations[i] = bones[i].Transform.localRotation;
            return true;
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

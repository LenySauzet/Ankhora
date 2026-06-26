using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Reads the live Meta hand skeletons + head each frame as <see cref="IHandPoseSource"/> for the
    /// recorder. Device-only: OVRSkeleton produces no data in macOS Editor Play Mode. Captures each
    /// bone's LOCAL rotation + the wrist root pose (compact, retargetable) — never world transforms.
    /// </summary>
    public class OvrHandPoseSource : MonoBehaviour, IHandPoseSource
    {
        [SerializeField] private OVRSkeleton _leftSkeleton;
        [SerializeField] private OVRSkeleton _rightSkeleton;
        [SerializeField] private Transform _centerEye;

        public bool TryGetHead(out Pose head)
        {
            if (_centerEye == null) { head = default; return false; }
            head = new Pose(_centerEye.localPosition, _centerEye.localRotation);
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
            pose.root = new Pose(wrist.localPosition, wrist.localRotation);

            if (pose.boneRotations == null || pose.boneRotations.Length != count)
                pose.boneRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
                pose.boneRotations[i] = bones[i].Transform.localRotation;
            return true;
        }
    }
}

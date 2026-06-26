using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Product <see cref="IHandView"/>: a duplicated Meta hand rig (skinned mesh) rendered with the
    /// translucent fresnel ghost material (see the urp-shadergraph skill). The bone transforms are
    /// assigned in the same order the capture wrote them (OVRSkeleton bone order, wrist at index 0)
    /// so sampled local rotations retarget directly. Device-verified.
    /// </summary>
    public class MetaGhostHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private GameObject _rigRoot;   // the skinned ghost hand, hidden until replay
        [SerializeField] private Transform _wrist;      // wrist bone (drives root pose)
        [SerializeField] private Transform[] _bones;    // bone transforms, capture order (index 0 == wrist)

        public void Show(bool visible)
        {
            if (_rigRoot != null)
                _rigRoot.SetActive(visible);
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_wrist != null)
            {
                _wrist.localPosition = root.position;
                _wrist.localRotation = root.rotation;
            }
            if (_bones == null)
                return;
            int n = Mathf.Min(_bones.Length, boneCount);
            for (int i = 0; i < n; i++)
                if (_bones[i] != null)
                    _bones[i].localRotation = boneRotations[i];
        }
    }
}

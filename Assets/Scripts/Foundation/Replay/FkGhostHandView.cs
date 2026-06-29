using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Articulated ghost hand built from a captured <see cref="HandSkeleton"/>: one GameObject per bone,
    /// parented per the captured topology at each bone's rest LOCAL pose, with a small sphere at every
    /// joint. Replay sets the wrist container to the tracking-space root and each finger bone's local
    /// rotation from the sampled data, so forward kinematics swing the joints through space — finger
    /// articulation is actually visible (unlike rotating a lone symmetric sphere).
    /// <para>
    /// Convention-safe: captured rotations are Unity-space <c>localRotation</c> values applied straight
    /// back onto an equivalently-parented rig (no flipped-Z round trip). The skinned Meta hand mesh is
    /// the later polish step. Bone 0 is the wrist root; its orientation comes from <c>root</c> (which
    /// carries the hand's gross motion), so its per-bone rotation is not re-applied.
    /// </para>
    /// </summary>
    public class FkGhostHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private Material _jointMaterial;
        [SerializeField, Min(0.001f)] private float _jointRadius = 0.008f;

        private Transform[] _bones;     // captured-order bone transforms, all children of this.transform (the anchor)
        private GameObject[] _joints;   // one joint sphere per bone, toggled by Show()
        private bool _built;

        public void Bind(HandSkeleton skeleton)
        {
            if (_built || skeleton == null || !skeleton.IsValid)
                return;
            BuildRig(skeleton);
            _built = true;
            Show(false);
        }

        private void BuildRig(HandSkeleton s)
        {
            int n = s.boneParents.Length;
            _bones = new Transform[n];
            _joints = new GameObject[n];

            // this.transform is the skeleton-root frame (the anchor Apply drives), NOT a bone. EVERY bone,
            // including the wrist root, is a real child carrying its bind pose; the root bone (invalid
            // parent) parents to this.transform, all others per the captured topology.
            for (int i = 0; i < n; i++)
                _bones[i] = new GameObject($"Bone_{i}").transform;

            for (int i = 0; i < n; i++)
            {
                int p = s.boneParents[i];
                Transform parent = (p >= 0 && p < n) ? _bones[p] : transform;
                _bones[i].SetParent(parent, false);
                _bones[i].localPosition = s.boneBindPoses[i].position;
                _bones[i].localRotation = s.boneBindPoses[i].rotation;
            }

            for (int i = 0; i < n; i++)
                _joints[i] = CreateJoint(_bones[i]);
        }

        private GameObject CreateJoint(Transform bone)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Joint";
            var collider = sphere.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            Transform t = sphere.transform;
            t.SetParent(bone, false);
            t.localPosition = Vector3.zero;
            t.localScale = Vector3.one * (_jointRadius * 2f);
            if (_jointMaterial != null)
                sphere.GetComponent<MeshRenderer>().sharedMaterial = _jointMaterial;
            return sphere;
        }

        public void Show(bool visible)
        {
            if (_joints == null)
                return;
            for (int i = 0; i < _joints.Length; i++)
                if (_joints[i] != null)
                    _joints[i].SetActive(visible);
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, Vector3[] boneLocalPositions, int boneCount)
        {
            if (_bones == null || boneRotations == null)
                return;

            // this.transform is the skeleton-root frame: placed from the captured root pose, which carries
            // the hand's gross motion through the room.
            transform.localPosition = root.position;
            transform.localRotation = root.rotation;

            bool hasPos = boneLocalPositions != null;
            int n = Mathf.Min(_bones.Length, boneCount);
            for (int i = 0; i < n; i++)
            {
                if (_bones[i] == null)
                    continue;
                _bones[i].localRotation = boneRotations[i];
                if (hasPos && i < boneLocalPositions.Length)
                    _bones[i].localPosition = boneLocalPositions[i];
            }
        }
    }
}

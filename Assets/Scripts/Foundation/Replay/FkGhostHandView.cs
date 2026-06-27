using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Articulated ghost hand built from a captured <see cref="HandSkeleton"/>: one GameObject per
    /// bone, parented per the captured topology at each bone's rest LOCAL pose, with a small sphere
    /// at every joint. Replay sets the wrist container to the tracking-space root and each finger
    /// bone's local rotation from the sampled data, so forward kinematics swing the joints through
    /// space — finger articulation is actually visible (unlike rotating a lone symmetric sphere).
    /// <para>
    /// This is the convention-safe first-light visual: captured rotations are Unity-space
    /// <c>localRotation</c> values applied straight back onto an equivalently-parented rig (no
    /// flipped-Z round trip). The skinned Meta hand mesh is the later polish step.
    /// </para>
    /// Bone 0 is the wrist root; its orientation comes from <c>root</c> (which carries the hand's
    /// gross motion through the room), so its per-bone rotation is not re-applied.
    /// </summary>
    public class FkGhostHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private Material _jointMaterial;
        [SerializeField, Min(0.001f)] private float _jointRadius = 0.008f;

        private Transform[] _bones;     // index-aligned with captured boneRotations; _bones[0] == this.transform
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
            _bones[0] = transform; // wrist container; positioned by Apply from the tracking-space root

            for (int i = 1; i < n; i++)
                _bones[i] = new GameObject($"Bone_{i}").transform;

            for (int i = 1; i < n; i++)
            {
                int p = s.boneParents[i];
                Transform parent = (p >= 0 && p < n) ? _bones[p] : transform;
                _bones[i].SetParent(parent, false);
                _bones[i].localPosition = s.boneBindPoses[i].position;
                _bones[i].localRotation = s.boneBindPoses[i].rotation;
            }

            for (int i = 0; i < n; i++)
                AttachJoint(_bones[i]);
        }

        private void AttachJoint(Transform bone)
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
        }

        public void Show(bool visible)
        {
            if (_bones == null)
                return;
            for (int i = 0; i < _bones.Length; i++)
                if (_bones[i] != null && _bones[i] != transform)
                    _bones[i].gameObject.SetActive(visible);
            // The wrist container stays active; its child joint follows _bones[0]'s own joint sphere.
            if (_bones[0] != null)
            {
                Transform wristJoint = _bones[0].Find("Joint");
                if (wristJoint != null)
                    wristJoint.gameObject.SetActive(visible);
            }
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_bones == null || boneRotations == null)
                return;

            // Wrist (bone 0): placed from the tracking-space root. This object is parented under the
            // rig's tracking-space anchor, so the root's local pose maps directly.
            transform.localPosition = root.position;
            transform.localRotation = root.rotation;

            int n = Mathf.Min(_bones.Length, boneCount);
            for (int i = 1; i < n; i++)
                if (_bones[i] != null)
                    _bones[i].localRotation = boneRotations[i];
        }
    }
}

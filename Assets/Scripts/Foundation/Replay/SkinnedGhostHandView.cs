using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Skinned translucent ghost hand: the captured <see cref="HandSkeleton"/> is rebuilt as a parented
    /// bone-transform hierarchy (same rig-build as <see cref="FkGhostHandView"/>), then a
    /// <see cref="SkinnedMeshRenderer"/> is wired to the Meta hand mesh and skinned to those bones, so
    /// the recorded finger articulation deforms a real hand mesh instead of floating joint spheres.
    /// Drives through the same <see cref="IHandView"/> seam, so <c>GhostHandPlayer</c> is unchanged.
    /// <para>
    /// Approach A (our rig drives the Meta mesh): capture and replay derive from the SAME OVR skeleton,
    /// so bone order and count match by construction (OpenXR 26-joint, count-agnostic). The mesh's own
    /// bind poses are baked into <c>sharedMesh.bindposes</c>; we only supply the bone transforms and a
    /// root. Device-verified — hand tracking does not render in the macOS Editor.
    /// </para>
    /// </summary>
    public class SkinnedGhostHandView : MonoBehaviour, IHandView
    {
        [Tooltip("Meta hand mesh for this hand: OVRHand_L for left, OVRHand_R for right.")]
        [SerializeField] private Mesh _handMesh;
        [SerializeField] private Material _ghostMaterial;

        private Transform[] _bones;     // index-aligned with captured boneRotations; _bones[0] == this.transform
        private SkinnedMeshRenderer _renderer;
        private bool _built;

        public void Bind(HandSkeleton skeleton)
        {
            if (_built || skeleton == null || !skeleton.IsValid)
                return;
            if (_handMesh == null)
            {
                Debug.LogError("[SkinnedGhostHandView] No hand mesh assigned.", this);
                return;
            }
            BuildRig(skeleton);
            BuildRenderer();
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
        }

        private void BuildRenderer()
        {
            var go = new GameObject("GhostMesh");
            go.transform.SetParent(transform, false);
            _renderer = go.AddComponent<SkinnedMeshRenderer>();
            _renderer.sharedMesh = _handMesh;       // bind poses are baked into the mesh
            _renderer.bones = _bones;               // built transforms in captured-skeleton order
            _renderer.rootBone = _bones[0];
            _renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 0.4f); // hand-sized; avoids culling pops
            _renderer.updateWhenOffscreen = true;
            if (_ghostMaterial != null)
                _renderer.sharedMaterial = _ghostMaterial;
        }

        public void Show(bool visible)
        {
            if (_renderer != null)
                _renderer.enabled = visible;
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_bones == null || boneRotations == null)
                return;

            // Wrist (bone 0): placed from the tracking-space root (carries the hand's gross motion).
            transform.localPosition = root.position;
            transform.localRotation = root.rotation;

            int n = Mathf.Min(_bones.Length, boneCount);
            for (int i = 1; i < n; i++)
                if (_bones[i] != null)
                    _bones[i].localRotation = boneRotations[i];
        }
    }
}

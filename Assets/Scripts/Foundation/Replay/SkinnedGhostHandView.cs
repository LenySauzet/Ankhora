using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Skinned translucent ghost hand: the captured <see cref="HandSkeleton"/> is rebuilt as a parented
    /// bone-transform hierarchy, then a <see cref="SkinnedMeshRenderer"/> is wired to the Meta hand mesh
    /// and skinned to those bones, so the recorded finger articulation deforms a real hand mesh instead
    /// of floating joint spheres. Drives through the same <see cref="IHandView"/> seam, so
    /// <c>GhostHandPlayer</c> is unchanged.
    /// <para>
    /// The Meta hand mesh is NOT the <c>OVRHand_*.fbx</c> asset — that mesh's vertex bone indices follow
    /// the FBX armature's own ordering and have no relation to <c>OVRPlugin.BoneId</c>. The real mesh is
    /// fetched from the headset at runtime by <see cref="OVRMesh"/>; we reuse the live hand's
    /// <see cref="OVRMesh"/> and replicate exactly what <c>OVRMeshRenderer.Initialize()</c> does:
    /// recompute <c>bindposes</c> from our rest rig (<c>bone.worldToLocal * meshRoot.localToWorld</c>,
    /// then the OpenXR 180°-Y fixup), with bones in <c>BoneId</c> order (our captured order already is).
    /// Binding the bare FBX mesh instead — the original bring-up mistake — tears the mesh apart. The mesh
    /// data only exists on device, so this is device-verified (the macOS Editor can't render hand tracking).
    /// </para>
    /// </summary>
    public class SkinnedGhostHandView : MonoBehaviour, IHandView
    {
        // Matches OVRMeshRenderer._openXRFixup exactly — corrects the OpenXR joint frame vs the mesh frame.
        private static readonly Matrix4x4 OpenXRFixup = Matrix4x4.Rotate(new Quaternion(0f, 1f, 0f, 0f));

        [Tooltip("The live hand's OVRMesh (same hand: left ghost -> left OVRHandPrefab's OVRMesh). " +
                 "Supplies the runtime headset mesh; bindposes are recomputed here from the captured rig.")]
        [SerializeField] private OVRMesh _ovrMesh;
        [SerializeField] private Material _ghostMaterial;

        private Transform[] _bones;     // index-aligned with captured boneRotations; _bones[0] == this.transform
        private SkinnedMeshRenderer _renderer;
        private bool _built;

        public void Bind(HandSkeleton skeleton)
        {
            if (_built || skeleton == null || !skeleton.IsValid)
                return;
            if (_ovrMesh == null)
            {
                Debug.LogError("[SkinnedGhostHandView] No OVRMesh assigned.", this);
                return;
            }
            // The runtime mesh is fetched from the headset; if the live hand hasn't initialised it yet,
            // build nothing and leave _built false so a later Bind retries (no duplicate rig).
            if (!_ovrMesh.IsInitialized)
                return;

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

        /// <summary>
        /// Builds the skinned renderer by replicating <c>OVRMeshRenderer.Initialize()</c>: take a private
        /// copy of the runtime mesh (the live hand shares the original), recompute every bind pose from the
        /// rig AT REST (this is called before the first <see cref="Apply"/>), and skin to our bone array.
        /// </summary>
        private void BuildRenderer()
        {
            // Private copy: OVRMesh.Mesh is a single runtime Mesh shared with the live hand's renderer, and
            // we overwrite its bindposes — mutating the shared instance would corrupt the live hand.
            Mesh mesh = Instantiate(_ovrMesh.Mesh);

            // Bake the wrist-fade gradient into this copy's vertex-colour alpha so the ghost fades at the
            // wrist (GhostHands_URP reads COLOR.a). Geometry-based, independent of the mesh UVs/glow mask.
            WristFadeBake.Apply(mesh);

            var go = new GameObject("GhostMesh");
            go.transform.SetParent(transform, false);   // identity under the wrist root = the mesh frame
            _renderer = go.AddComponent<SkinnedMeshRenderer>();

            int n = _bones.Length;
            var bindPoses = new Matrix4x4[n];
            Matrix4x4 localToWorld = go.transform.localToWorldMatrix;
            for (int i = 0; i < n; i++)
            {
                if (_bones[i] == null)
                    continue;
                // OVRMeshRenderer: bindPoses[i] = BindPoses[i].worldToLocal * meshRoot.localToWorld * fixup.
                // Our rig is at its bind pose right now, so _bones[i].worldToLocalMatrix IS that bind pose.
                bindPoses[i] = _bones[i].worldToLocalMatrix * localToWorld * OpenXRFixup;
            }
            mesh.bindposes = bindPoses;

            _renderer.sharedMesh = mesh;
            _renderer.bones = _bones;               // captured BoneId order == the mesh's blend-index order
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

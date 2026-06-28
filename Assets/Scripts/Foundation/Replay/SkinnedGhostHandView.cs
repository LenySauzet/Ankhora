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

        private Transform[] _bones;     // index-aligned with captured boneRotations; _bones[_rootIndex] == this.transform
        private int _rootIndex;         // topological root bone (the wrist in OpenXR hands), NOT necessarily 0
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
            _rootIndex = s.RootBoneIndex;   // the wrist in OpenXR hands (index 1), not the palm (index 0)
            _bones = new Transform[n];

            // this.transform is the SKELETON-ROOT FRAME (the anchor Apply drives), NOT a bone — it mirrors
            // Meta's _bonesGO. EVERY bone, including the wrist root, is a real child carrying its bind pose,
            // so the rig's rest equals the live skeleton's rest and the mesh skins correctly. The root bone
            // (invalid parent) parents to this.transform; all others per the captured topology.
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
            go.transform.SetParent(transform, false);   // identity under the skeleton-root frame = mesh frame (Meta's meshRoot)
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
            _renderer.rootBone = _bones[_rootIndex];   // the wrist anchor, not the palm (index 0)
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

        public void Apply(in Pose root, Quaternion[] boneRotations, Vector3[] boneLocalPositions, int boneCount)
        {
            if (_bones == null || boneRotations == null)
                return;

            // this.transform is the skeleton-root frame: placed from the captured root pose, which carries
            // the hand's gross motion through the room.
            transform.localPosition = root.position;
            transform.localRotation = root.rotation;

            // Drive BOTH local rotation and local position for EVERY bone (the wrist root included — it is a
            // real child of the anchor here). Position is a small refinement (the offsets are near-rigid);
            // when absent (legacy recording) bones keep the rest bind offsets set in BuildRig.
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

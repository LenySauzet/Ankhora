using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Bakes the wrist-fade gradient into the LIVE hand mesh's vertex colours once <see cref="OVRMesh"/>
    /// has fetched the runtime mesh from the headset, so <c>GhostHands_URP</c> fades the live hand's wrist
    /// like the replay ghost. The runtime mesh only exists on device, so this runs at play time on the
    /// Quest (the Mac editor can't render hand tracking); it bakes once, then disables. The replay ghost
    /// bakes its own instantiated copy in <see cref="SkinnedGhostHandView"/>.
    /// </summary>
    [RequireComponent(typeof(OVRMesh))]
    public class HandMeshWristFadeBaker : MonoBehaviour
    {
        [SerializeField] private OVRMesh _ovrMesh;
        [Tooltip("The live hand's SkinnedMeshRenderer; its sharedMesh gets the baked vertex colours.")]
        [SerializeField] private SkinnedMeshRenderer _renderer;

        private bool _done;

        private void Awake()
        {
            if (_ovrMesh == null)
                _ovrMesh = GetComponent<OVRMesh>();
            if (_renderer == null)
                _renderer = GetComponent<SkinnedMeshRenderer>();
        }

        private void Update()
        {
            if (_done || _ovrMesh == null || !_ovrMesh.IsInitialized)
                return;

            UnityEngine.Mesh mesh = _renderer != null ? _renderer.sharedMesh : _ovrMesh.Mesh;
            if (mesh == null || mesh.vertexCount == 0)
                return;

            WristFadeBake.Apply(mesh);
            _done = true;
            enabled = false;
        }
    }
}

using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Real <see cref="IPassthroughSurface"/>: drives the Meta <c>OVRPassthroughLayer</c> and the
    /// center-eye camera so enabling shows the real world behind virtual content and disabling
    /// restores the opaque VR background.
    ///
    /// Enabling the layer component alone is NOT enough: <c>OVRPassthroughLayer</c> only submits a
    /// compositor layer while <c>OVRManager.isInsightPassthroughEnabled</c> is true and the system
    /// has finished initialising it (see OVRPassthroughLayer's overlay gate). So this adapter also
    /// drives <c>OVRManager.instance.isInsightPassthroughEnabled</c>. Init is asynchronous — a brief
    /// black frame on the first enable is expected while the passthrough cameras spin up. Verified
    /// on device — Mac Editor Play Mode cannot render passthrough.
    /// </summary>
    public class OvrPassthroughSurface : MonoBehaviour, IPassthroughSurface
    {
        [Tooltip("The underlay passthrough layer (virtual content renders on top).")]
        [SerializeField] private OVRPassthroughLayer _passthroughLayer;

        [Tooltip("The center-eye camera whose background is cleared to show passthrough through.")]
        [SerializeField] private Camera _centerEyeCamera;

        [Tooltip("Opaque background restored when passthrough is off (VR mode).")]
        [SerializeField] private Color _vrBackground = Color.black;

        public void SetEnabled(bool enabled)
        {
            // Gate the OVRManager-level insight passthrough: without it the layer never submits a
            // compositor layer (logcat shows "numLayers: 0") and the user sees black, not the room.
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = enabled;

            if (_passthroughLayer != null)
                _passthroughLayer.enabled = enabled;

            if (_centerEyeCamera != null)
            {
                _centerEyeCamera.clearFlags = enabled ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
                _centerEyeCamera.backgroundColor = enabled ? Color.clear : _vrBackground;
            }
        }
    }
}

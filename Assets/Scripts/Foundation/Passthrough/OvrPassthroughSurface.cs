using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Real <see cref="IPassthroughSurface"/>: drives the Meta <c>OVRPassthroughLayer</c> and the
    /// center-eye camera so enabling shows the real world behind virtual content and disabling
    /// restores the opaque VR background. Enabling the layer kicks off system passthrough init
    /// (asynchronous — a brief black flicker on the very first enable is expected; see Meta's
    /// passthroughLayerResumed event if that ever needs hiding). Verified on device / Meta XR
    /// Simulator — Mac Editor Play Mode cannot render passthrough.
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

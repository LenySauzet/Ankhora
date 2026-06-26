using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Real <see cref="IPassthroughSurface"/>: smoothly crossfades between the opaque VR background
    /// and the real-world passthrough feed by animating the center-eye camera's background alpha
    /// (1 = opaque VR, 0 = transparent, revealing the underlay passthrough). The eased progress is
    /// computed by <see cref="PassthroughFade"/>.
    ///
    /// Two Meta-SDK facts shape this:
    /// 1. <c>OVRPassthroughLayer</c> only submits a compositor layer while
    ///    <c>OVRManager.isInsightPassthroughEnabled</c> is true (enabling the component alone shows
    ///    black — logcat "numLayers: 0"). The system is brought up once in <see cref="Awake"/> and
    ///    kept ready so every toggle crossfades instantly instead of re-initialising (which is async
    ///    and would flash black at the start of a fade-in). Cost: the passthrough cameras stay on in
    ///    VR too; acceptable for an MVP comfort toggle.
    /// 2. An underlay is only visible through a transparent eye buffer, so an opaque Skybox cannot
    ///    crossfade with passthrough — VR therefore uses a solid <see cref="_vrBackground"/> colour
    ///    (themeable) rather than the Skybox.
    ///
    /// Verified on device — Mac Editor Play Mode cannot render passthrough.
    /// </summary>
    public class OvrPassthroughSurface : MonoBehaviour, IPassthroughSurface
    {
        [Tooltip("The underlay passthrough layer (virtual content renders on top).")]
        [SerializeField] private OVRPassthroughLayer _passthroughLayer;

        [Tooltip("The center-eye camera whose background alpha is crossfaded to reveal passthrough.")]
        [SerializeField] private Camera _centerEyeCamera;

        [Tooltip("Solid VR background shown when passthrough is off (themeable).")]
        [SerializeField] private Color _vrBackground = Color.black;

        [Tooltip("Seconds for the VR↔MR crossfade. 0 = instant.")]
        [SerializeField, Min(0f)] private float _transitionSeconds = 0.4f;

        // Global shader property the VR environment (floor grid, gradient sky) reads to fade out
        // in MR: 0 = full VR, 1 = full passthrough.
        private static readonly int MrAmountId = Shader.PropertyToID("_AnkhoraMrAmount");

        private PassthroughFade _fade;
        private float _target;

        private void Awake()
        {
            // Bring passthrough up once and keep it ready (see class summary, point 1).
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = true;

            if (_passthroughLayer != null)
            {
                _passthroughLayer.enabled = true;
                _passthroughLayer.textureOpacity = 1f; // opacity is carried by the camera-alpha crossfade
            }

            if (_centerEyeCamera != null)
                _centerEyeCamera.clearFlags = CameraClearFlags.SolidColor;

            Apply(); // snap to the initial target (VR at startup)
        }

        public void SetEnabled(bool enabled) => _target = enabled ? 1f : 0f;

        private void Update()
        {
            if (_fade.HasReached(_target))
                return;

            _fade.Step(_target, Time.deltaTime, _transitionSeconds);
            Apply();
        }

        private void Apply()
        {
            float mr = _fade.Opacity;

            // Fade the VR environment (floor grid, gradient sky) out as passthrough comes in.
            Shader.SetGlobalFloat(MrAmountId, mr);

            if (_centerEyeCamera == null)
                return;

            // MR-ness fades the eye-buffer background from opaque VR colour to transparent, so the
            // underlay passthrough is progressively revealed through it.
            _centerEyeCamera.backgroundColor =
                new Color(_vrBackground.r, _vrBackground.g, _vrBackground.b, 1f - mr);
        }

        private void OnDisable()
        {
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = false;
        }
    }
}

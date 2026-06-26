using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Real <see cref="IPassthroughSurface"/>: drives the VR↔MR transition by publishing a global
    /// shader value <c>_AnkhoraMrAmount</c> (0 = VR, 1 = MR, eased by <see cref="PassthroughFade"/>).
    /// The VR environment shaders read it: the gradient sky uses it for a centre-of-vision radial
    /// reveal of the passthrough underlay (and provides the VR backdrop via the skybox), and the
    /// floor grid fades out. The model and grid stay visible in MR because the sky only paints
    /// background pixels.
    ///
    /// Two Meta-SDK facts still apply:
    /// 1. <c>OVRPassthroughLayer</c> only submits a compositor layer while
    ///    <c>OVRManager.isInsightPassthroughEnabled</c> is true. The system is brought up once in
    ///    <see cref="Awake"/> and kept ready so every toggle is instant (re-init is async and would
    ///    flash). Cost: passthrough cameras stay on in VR too — fine for an MVP comfort toggle.
    /// 2. The reveal works by lowering the eye-buffer alpha; the camera therefore clears to the
    ///    skybox, whose shader writes that alpha. Verified on device — Mac Editor Play Mode cannot
    ///    render passthrough.
    /// </summary>
    public class OvrPassthroughSurface : MonoBehaviour, IPassthroughSurface
    {
        [Tooltip("The underlay passthrough layer (virtual content renders on top).")]
        [SerializeField] private OVRPassthroughLayer _passthroughLayer;

        [Tooltip("The center-eye camera, cleared to the gradient skybox.")]
        [SerializeField] private Camera _centerEyeCamera;

        [Tooltip("Seconds for the VR↔MR transition. 0 = instant.")]
        [SerializeField, Min(0f)] private float _transitionSeconds = 1.5f;

        // Global shader property the VR environment (gradient sky reveal, floor grid) reads:
        // 0 = full VR, 1 = full passthrough.
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
                _passthroughLayer.textureOpacity = 1f;
            }

            // The gradient skybox provides the VR backdrop and writes the reveal alpha.
            if (_centerEyeCamera != null)
                _centerEyeCamera.clearFlags = CameraClearFlags.Skybox;

            Apply();
        }

        public void SetEnabled(bool enabled) => _target = enabled ? 1f : 0f;

        private void Update()
        {
            if (_fade.HasReached(_target))
                return;

            _fade.Step(_target, Time.deltaTime, _transitionSeconds);
            Apply();
        }

        // Publish the eased MR-ness; the sky reveal and grid fade read it from the global.
        private void Apply() => Shader.SetGlobalFloat(MrAmountId, _fade.Opacity);

        private void OnDisable()
        {
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = false;
        }
    }
}

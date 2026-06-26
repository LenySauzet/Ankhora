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
    ///    <c>OVRManager.isInsightPassthroughEnabled</c> is true. The system is brought up in
    ///    <see cref="OnEnable"/> (torn down symmetrically in <see cref="OnDisable"/>) and kept ready
    ///    so every toggle is instant (re-init is async and would flash). Cost: passthrough cameras
    ///    stay on in VR too — fine for an MVP comfort toggle.
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

        private PassthroughFade _fade;
        private float _target;

        private void Awake()
        {
            // The gradient skybox provides the VR backdrop and writes the reveal alpha (one-time).
            if (_centerEyeCamera != null)
                _centerEyeCamera.clearFlags = CameraClearFlags.Skybox;
        }

        private void OnEnable()
        {
            // Bring passthrough up and keep it ready (see class summary, point 1). Symmetric with
            // OnDisable so a disable/re-enable cycle (scene reload, rig reset) restores it instead
            // of leaving the compositor layer dead for the rest of the session.
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = true;

            if (_passthroughLayer != null)
            {
                _passthroughLayer.enabled = true;
                _passthroughLayer.textureOpacity = 1f;
            }

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
        private void Apply() => Shader.SetGlobalFloat(PassthroughShaderProperties.MrAmount, _fade.Opacity);

        private void OnDisable()
        {
            if (OVRManager.instance != null)
                OVRManager.instance.isInsightPassthroughEnabled = false;

            // Reset the VR/MR global so the environment shaders don't keep a stale mid-fade value
            // while the passthrough layer is gone (a re-enable republishes it from OnEnable).
            Shader.SetGlobalFloat(PassthroughShaderProperties.MrAmount, 0f);
        }
    }
}

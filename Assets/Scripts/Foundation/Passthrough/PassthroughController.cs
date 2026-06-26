using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Toggles Passthrough (MR) on/off at runtime: pressing the bound controller button flips
    /// between the opaque VR background and the real-world feed. This is the MVP's Passthrough
    /// toggle (roadmap S1) — a comfort/utility control for both the instructor and the learner.
    /// The on/off decision and surface driving are plain logic; only the per-frame button read
    /// touches the engine, so the behaviour is EditMode-testable via a fake <see cref="IPassthroughSurface"/>.
    /// </summary>
    public class PassthroughController : MonoBehaviour
    {
        [Tooltip("Drives the real OVRPassthroughLayer + camera. Leave unset only in tests.")]
        [SerializeField] private OvrPassthroughSurface _surface;

        [Tooltip("If true the app starts in Passthrough (MR); otherwise in the opaque VR background.")]
        [SerializeField] private bool _startInPassthrough;

        [Tooltip("Controller button that toggles passthrough (Two = B / Y).")]
        [SerializeField] private OVRInput.Button _toggleButton = OVRInput.Button.Two;

        private IPassthroughSurface _boundSurface;
        private bool _isOn;

        /// <summary>True when passthrough (the real-world feed) is currently shown.</summary>
        public bool IsPassthroughOn => _isOn;

        private void Awake() => Initialize(_surface, _startInPassthrough);

        /// <summary>
        /// Bind the surface this controller drives and apply the initial state. Called from
        /// <see cref="Awake"/> with the serialized OVR surface; tests call it with a fake.
        /// </summary>
        public void Initialize(IPassthroughSurface surface, bool startInPassthrough)
        {
            _boundSurface = surface;
            SetPassthrough(startInPassthrough);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_toggleButton))
                Toggle();
        }

        /// <summary>Flip between passthrough (MR) and the opaque VR background.</summary>
        public void Toggle() => SetPassthrough(!_isOn);

        /// <summary>Show passthrough when <paramref name="on"/> is true, else the VR background.</summary>
        public void SetPassthrough(bool on)
        {
            _isOn = on;
            _boundSurface?.SetEnabled(on);
        }
    }
}

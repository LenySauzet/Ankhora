using Ankhora.Foundation.Recording;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ankhora.Foundation.UI
{
    /// <summary>
    /// In-headset recording cue, driven by <see cref="PinchRecordingTrigger"/>. Anchored to the top-right
    /// of the user's view and rendered on top of the scene (its graphics use ZTest-Always overlay materials)
    /// so it never clips into world geometry — the issue reported on device.
    /// <para>
    /// Three states: the 3-2-1 countdown shows as digits on <see cref="_label"/>; while recording, a small
    /// red <see cref="_dot"/> pulses; on save the dot turns steady green for <see cref="_savedHideDelay"/>
    /// seconds, then everything hides. Label and dot are mutually exclusive per state.
    /// </para>
    /// </summary>
    public class RecordingHud : MonoBehaviour
    {
        [SerializeField] private PinchRecordingTrigger _trigger;
        [Tooltip("Countdown digits (3, 2, 1). Use an Overlay TMP material so it renders on top.")]
        [SerializeField] private TMP_Text _label;
        [Tooltip("The REC/Saved dot (a filled-circle Image). Use a ZTest-Always material so it renders on top.")]
        [SerializeField] private Graphic _dot;
        [SerializeField, Min(0f)] private float _savedHideDelay = 1.5f;
        [Tooltip("Pulses per second of the red REC dot.")]
        [SerializeField, Min(0.1f)] private float _pulseHz = 1.6f;

        private static readonly Color CountingColor = new Color(0.85f, 0.95f, 1f);
        private static readonly Color RecordingColor = new Color(1f, 0.25f, 0.25f);
        private static readonly Color SavedColor = new Color(0.35f, 1f, 0.45f);

        private enum Mode { Hidden, Counting, Recording, Saved }

        private Mode _mode = Mode.Hidden;
        private float _hideAt = -1f;

        private void Awake()
        {
            if (_trigger == null || _label == null || _dot == null)
            {
                Debug.LogError("[RecordingHud] Assign the trigger, the label and the dot.", this);
                enabled = false;
                return;
            }
            Hide();
        }

        private void OnEnable()
        {
            if (_trigger == null)
                return;
            _trigger.OnCountdownTick.AddListener(ShowCountdown);
            _trigger.OnRecordingStarted.AddListener(ShowRecording);
            _trigger.OnRecordingSaved.AddListener(ShowSaved);
        }

        private void OnDisable()
        {
            if (_trigger == null)
                return;
            _trigger.OnCountdownTick.RemoveListener(ShowCountdown);
            _trigger.OnRecordingStarted.RemoveListener(ShowRecording);
            _trigger.OnRecordingSaved.RemoveListener(ShowSaved);
        }

        private void Update()
        {
            if (_mode == Mode.Recording && _dot != null)
            {
                // Pulse the dot's opacity, but only between a high floor and full (0.6 -> 1.0). On Quest a
                // translucent overlay over passthrough opens a "porthole" to the real world; keeping a high
                // floor makes the pulse read as a gentle breathing glow rather than a window. Unscaled time
                // so slow-motion replay never affects the cue.
                float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * _pulseHz * 2f * Mathf.PI);
                Color c = RecordingColor;
                c.a = Mathf.Lerp(0.6f, 1f, t);
                _dot.color = c;
            }

            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
                Hide();
        }

        private void ShowCountdown(int seconds)
        {
            _mode = Mode.Counting;
            _hideAt = -1f;
            SetDot(false, RecordingColor);
            SetLabel(seconds > 0 ? seconds.ToString() : string.Empty, CountingColor);
        }

        private void ShowRecording()
        {
            _mode = Mode.Recording;
            _hideAt = -1f;
            SetLabel(string.Empty, CountingColor);
            SetDot(true, RecordingColor);
        }

        private void ShowSaved()
        {
            _mode = Mode.Saved;
            SetLabel(string.Empty, CountingColor);
            SetDot(true, SavedColor);
            _hideAt = Time.unscaledTime + _savedHideDelay;
        }

        private void SetLabel(string text, Color color)
        {
            if (_label == null)
                return;
            _label.text = text;
            _label.color = color;
            _label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private void SetDot(bool visible, Color color)
        {
            if (_dot == null)
                return;
            _dot.color = color;                 // alpha as given (recording pulses it; saved stays opaque)
            _dot.gameObject.SetActive(visible);
        }

        private void Hide()
        {
            _mode = Mode.Hidden;
            _hideAt = -1f;
            SetLabel(string.Empty, CountingColor);
            SetDot(false, RecordingColor);
        }
    }
}

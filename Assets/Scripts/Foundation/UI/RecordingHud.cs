using Ankhora.Foundation.Recording;
using TMPro;
using UnityEngine;

namespace Ankhora.Foundation.UI
{
    /// <summary>
    /// In-headset recording feedback driven by <see cref="PinchRecordingTrigger"/>: shows the 3-2-1
    /// countdown, a "REC" indicator while the take records, and a brief "Saved" confirmation. Without it
    /// the pinch trigger gives no visible cue that recording has started — the gap reported on device.
    /// Put the <see cref="_label"/> on a world-space canvas anchored in front of the user.
    /// </summary>
    public class RecordingHud : MonoBehaviour
    {
        [SerializeField] private PinchRecordingTrigger _trigger;
        [SerializeField] private TMP_Text _label;
        [SerializeField, Min(0f)] private float _savedHideDelay = 1.5f;

        private static readonly Color CountingColor = new Color(0.85f, 0.95f, 1f);
        private static readonly Color RecordingColor = new Color(1f, 0.3f, 0.3f);
        private static readonly Color SavedColor = new Color(0.4f, 1f, 0.5f);

        private float _hideAt = -1f;

        private void Awake()
        {
            if (_trigger == null || _label == null)
            {
                Debug.LogError("[RecordingHud] Assign the trigger and the label.", this);
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
            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
                Hide();
        }

        private void ShowCountdown(int seconds)
        {
            _hideAt = -1f;
            Set(seconds > 0 ? seconds.ToString() : string.Empty, CountingColor);
        }

        private void ShowRecording()
        {
            _hideAt = -1f;
            Set("● REC", RecordingColor);   // ● REC
        }

        private void ShowSaved()
        {
            Set("✓ Saved", SavedColor);     // ✓ Saved
            _hideAt = Time.unscaledTime + _savedHideDelay;
        }

        private void Set(string text, Color color)
        {
            _label.text = text;
            _label.color = color;
            _label.gameObject.SetActive(true);
        }

        private void Hide()
        {
            _hideAt = -1f;
            if (_label != null)
            {
                _label.text = string.Empty;
                _label.gameObject.SetActive(false);
            }
        }
    }
}

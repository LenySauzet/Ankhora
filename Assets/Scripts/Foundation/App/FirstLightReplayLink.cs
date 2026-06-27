using Ankhora.Foundation.Recording;
using Ankhora.Foundation.Replay;
using UnityEngine;

namespace Ankhora.Foundation.App
{
    /// <summary>
    /// Scene composition root: links the recorder's "saved" event to the ghost player's playback so the
    /// Recording code stays ignorant of the Replay code (ADR-0004 — features don't reference each other;
    /// the wiring lives here). Reused by the real trigger when it replaces the first-light harness.
    /// </summary>
    public class FirstLightReplayLink : MonoBehaviour
    {
        [SerializeField] private FirstLightAutoCapture _recorder;
        [SerializeField] private GhostHandPlayer _player;

        private void Awake()
        {
            if (_recorder == null || _player == null)
            {
                Debug.LogError("[FirstLightReplayLink] Assign both the recorder and the player.", this);
                return;
            }
            _recorder.OnRecordingSaved.AddListener(_player.LoadAndPlay);
        }

        private void OnDestroy()
        {
            if (_recorder != null && _player != null)
                _recorder.OnRecordingSaved.RemoveListener(_player.LoadAndPlay);
        }
    }
}

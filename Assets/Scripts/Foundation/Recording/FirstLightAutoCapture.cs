using Ankhora.Domain.Recording;
using Ankhora.Foundation.Persistence;
using UnityEngine;
using UnityEngine.Events;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Buttonless first-light bring-up harness for the hands capture → replay loop. On a fixed schedule
    /// — countdown, record, done — it records from a <see cref="RecordingSession"/> and raises
    /// <see cref="OnRecordingSaved"/> when the take is written, with no controller buttons (a held
    /// controller would disable that hand's tracking). The replay is wired to that event in the scene,
    /// so this Recording component does not depend on the Replay code.
    /// <para>
    /// Scaffolding: this is the de-risking harness, superseded by a real trigger (hand pinch) in the
    /// hands-consolidation slice. Hand tracking can't run in the macOS Editor, so the timing is pinned
    /// in EditMode via <see cref="AutoCaptureClock"/> and this shell stays thin.
    /// </para>
    /// </summary>
    public class FirstLightAutoCapture : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _poseSourceBehaviour;  // implements IHandPoseSource
        [SerializeField, Min(0f)] private float _countdownSeconds = 3f;
        [SerializeField, Min(1f)] private float _recordSeconds = 8f;
        [SerializeField, Min(1f)] private float _sampleRateHz = 30f;
        [SerializeField] private string _fileName = "masterclass.json";

        [Tooltip("Raised after the take is saved — wire it to the ghost player's LoadAndPlay in the scene.")]
        [SerializeField] private UnityEvent _onRecordingSaved = new UnityEvent();

        public UnityEvent OnRecordingSaved => _onRecordingSaved;

        private RecordingSession _session;
        private MasterclassStore _store;
        private AutoCaptureClock _clock;
        private float _startTime;
        private AutoCapturePhase _phase = AutoCapturePhase.Countdown;
        private bool _recordingBegun;
        private bool _saved;

        private void Awake()
        {
            var source = _poseSourceBehaviour as IHandPoseSource;
            if (source == null)
                Debug.LogError("[FirstLightAutoCapture] _poseSourceBehaviour must implement IHandPoseSource.", this);
            else
                _session = new RecordingSession(source, _sampleRateHz);
            _store = new MasterclassStore(_fileName);
            _clock = new AutoCaptureClock(_countdownSeconds, _recordSeconds);
        }

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;
            _phase = AutoCapturePhase.Countdown;
            _recordingBegun = false;
            _saved = false;
            Debug.Log($"[FirstLightAutoCapture] Countdown {_countdownSeconds:0}s, then recording {_recordSeconds:0}s.");
        }

        private void Update()
        {
            if (_session == null)
                return;

            float now = Time.unscaledTime;
            AutoCapturePhase phase = _clock.PhaseAt(now - _startTime);
            if (phase != _phase)
            {
                _phase = phase;
                Debug.Log($"[FirstLightAutoCapture] Phase -> {phase}");
            }

            switch (phase)
            {
                case AutoCapturePhase.Recording:
                    if (!_recordingBegun)
                    {
                        _session.Begin(now);
                        _recordingBegun = true;
                    }
                    _session.Tick(now);
                    break;

                case AutoCapturePhase.Done:
                    if (_recordingBegun && !_saved)
                        StopAndPublish(now);
                    break;
            }
        }

        private void StopAndPublish(float now)
        {
            _saved = true;
            bool ok = _session.SaveTo(_store, now, out int frames, out string error);
            if (!ok)
            {
                Debug.LogError($"[FirstLightAutoCapture] Save failed: {error}", this);
                return;
            }

            Debug.Log($"[FirstLightAutoCapture] Saved {frames} frames " +
                      $"(L:{_session.LeftBoneCount} R:{_session.RightBoneCount} bones) to {_store.Path}. Replaying.");
            _onRecordingSaved.Invoke();
        }
    }
}

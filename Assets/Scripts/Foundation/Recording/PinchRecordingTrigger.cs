using Ankhora.Domain.Recording;
using Ankhora.Foundation.Persistence;
using UnityEngine;
using UnityEngine.Events;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Pinch-triggered recording: a non-dominant index pinch arms a take, a fixed 3-2-1 countdown keeps
    /// the arming gesture out of the recorded window, recording then runs until a second pinch stops and
    /// saves it. Replaces the buttonless <c>FirstLightAutoCapture</c> bring-up harness. The pinch toggle
    /// and the countdown are pure (<see cref="PinchEdgeDetector"/> / <see cref="RecordingCountdown"/>,
    /// EditMode-tested); this shell only owns the OVR reads and the state machine, verified on device.
    /// <para>
    /// Interim trigger by design — the real record control will come from the product UI later. We pinch
    /// the NON-dominant hand so the dominant hand (the one demonstrating) is never occluded by the
    /// gesture, and so a held controller never disables that hand's tracking.
    /// </para>
    /// </summary>
    public class PinchRecordingTrigger : MonoBehaviour
    {
        [Tooltip("The non-dominant hand whose index pinch arms/stops the take.")]
        [SerializeField] private OVRHand _triggerHand;
        [SerializeField] private MonoBehaviour _poseSourceBehaviour;   // implements IHandPoseSource
        [SerializeField, Min(1f)] private float _countdownSeconds = 3f;
        [SerializeField, Min(1f)] private float _sampleRateHz = 30f;
        [SerializeField, Min(0f)] private float _pinchDebounceSeconds = 0.05f;
        [SerializeField] private string _fileName = "masterclass.json";

        [Tooltip("Raised after the take is saved — wire it to the ghost player's LoadAndPlay in the scene.")]
        [SerializeField] private UnityEvent _onRecordingSaved = new UnityEvent();
        [Tooltip("Raised each second of the countdown with the value to show (3, 2, 1). Hook for future UI.")]
        [SerializeField] private UnityEvent<int> _onCountdownTick = new UnityEvent<int>();

        public UnityEvent OnRecordingSaved => _onRecordingSaved;
        public UnityEvent<int> OnCountdownTick => _onCountdownTick;

        private enum State { Idle, CountingDown, Recording }

        private RecordingSession _session;
        private MasterclassStore _store;
        private RecordingCountdown _countdown;
        private PinchEdgeDetector _pinch;
        private State _state = State.Idle;
        private float _armTime;
        private int _lastSecondShown = -1;

        private void Awake()
        {
            var source = _poseSourceBehaviour as IHandPoseSource;
            if (source == null)
                Debug.LogError("[PinchRecordingTrigger] _poseSourceBehaviour must implement IHandPoseSource.", this);
            else
                _session = new RecordingSession(source, _sampleRateHz);

            if (_triggerHand == null)
                Debug.LogError("[PinchRecordingTrigger] Assign the non-dominant trigger OVRHand.", this);

            _store = new MasterclassStore(_fileName);
            _countdown = new RecordingCountdown(_countdownSeconds);
            _pinch = new PinchEdgeDetector(_pinchDebounceSeconds);
        }

        private void Update()
        {
            if (_session == null || _triggerHand == null)
                return;

            float now = Time.unscaledTime;
            bool isPinching = _triggerHand.IsTracked &&
                              _triggerHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            bool freshPinch = _pinch.Tick(isPinching, Time.unscaledDeltaTime);

            switch (_state)
            {
                case State.Idle:
                    if (freshPinch)
                        Arm(now);
                    break;

                case State.CountingDown:
                    TickCountdown(now);
                    break;

                case State.Recording:
                    _session.Tick(now);
                    if (freshPinch)
                        StopAndPublish(now);
                    break;
            }
        }

        private void Arm(float now)
        {
            _state = State.CountingDown;
            _armTime = now;
            _lastSecondShown = -1;
            Debug.Log($"[PinchRecordingTrigger] Armed — {_countdownSeconds:0}s countdown.");
            TickCountdown(now);
        }

        private void TickCountdown(float now)
        {
            float elapsed = now - _armTime;
            if (_countdown.PhaseAt(elapsed) == CountdownPhase.Live)
            {
                _state = State.Recording;
                _session.Begin(now);
                Debug.Log("[PinchRecordingTrigger] Recording — pinch again to stop.");
                return;
            }

            int second = _countdown.SecondsRemaining(elapsed);
            if (second != _lastSecondShown)
            {
                _lastSecondShown = second;
                _onCountdownTick.Invoke(second);
                Debug.Log($"[PinchRecordingTrigger] {second}...");
            }
        }

        private void StopAndPublish(float now)
        {
            _state = State.Idle;
            bool ok = _session.SaveTo(_store, now, out int frames, out string error);
            if (!ok)
            {
                Debug.LogError($"[PinchRecordingTrigger] Save failed: {error}", this);
                return;
            }

            Debug.Log($"[PinchRecordingTrigger] Saved {frames} frames " +
                      $"(L:{_session.LeftBoneCount} R:{_session.RightBoneCount} bones) to {_store.Path}. Replaying.");
            _onRecordingSaved.Invoke();
        }
    }
}

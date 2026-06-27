using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Recording;
using Ankhora.Domain.Serialization;
using Ankhora.Foundation.Replay;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Buttonless first-light harness for the hands capture → replay loop. On a fixed schedule —
    /// countdown, record, done — it records both hands + head from an <see cref="IHandPoseSource"/>,
    /// captures the bone skeleton once, writes the take to device storage, then tells a
    /// <see cref="GhostHandPlayer"/> to load and loop it. No controller buttons, so it works while the
    /// user's bare hands are tracked (a controller in hand would disable that hand's tracking).
    /// <para>
    /// Verified on device: the macOS Editor cannot produce hand-tracking data, so the timing is pinned
    /// in EditMode via <see cref="AutoCaptureClock"/> and this shell stays thin.
    /// </para>
    /// </summary>
    public class FirstLightAutoCapture : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _poseSourceBehaviour;  // implements IHandPoseSource
        [SerializeField] private GhostHandPlayer _player;
        [SerializeField, Min(0f)] private float _countdownSeconds = 3f;
        [SerializeField, Min(1f)] private float _recordSeconds = 8f;
        [SerializeField, Min(1f)] private float _sampleRateHz = 30f;
        [SerializeField] private string _fileName = "masterclass.json";

        private IHandPoseSource _source;
        private IHandSkeletonSource _skeletonSource;
        private TimelineRecorder _recorder;
        private AutoCaptureClock _clock;
        private readonly IMasterclassSerializer _serializer = new JsonMasterclassSerializer();
        private HandPose _left;
        private HandPose _right;
        private HandSkeleton _leftSkeleton;
        private HandSkeleton _rightSkeleton;
        private float _startTime;
        private AutoCapturePhase _phase = AutoCapturePhase.Countdown;
        private bool _recordingBegun;
        private bool _saved;

        private void Awake()
        {
            _source = _poseSourceBehaviour as IHandPoseSource;
            _skeletonSource = _poseSourceBehaviour as IHandSkeletonSource;
            if (_source == null)
                Debug.LogError("[FirstLightAutoCapture] _poseSourceBehaviour must implement IHandPoseSource.", this);
            _recorder = new TimelineRecorder(_sampleRateHz);
            _clock = new AutoCaptureClock(_countdownSeconds, _recordSeconds);
        }

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;
            _phase = AutoCapturePhase.Countdown;
            _recordingBegun = false;
            _saved = false;
            _leftSkeleton = null;
            _rightSkeleton = null;
            Debug.Log($"[FirstLightAutoCapture] Countdown {_countdownSeconds:0}s, then recording {_recordSeconds:0}s.");
        }

        private void Update()
        {
            if (_source == null)
                return;

            float now = Time.unscaledTime;
            float elapsed = now - _startTime;
            AutoCapturePhase phase = _clock.PhaseAt(elapsed);

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
                        _recorder.Begin(now);
                        _recordingBegun = true;
                    }
                    TryCaptureSkeleton();
                    PushFrame(now);
                    break;

                case AutoCapturePhase.Done:
                    if (_recordingBegun && !_saved)
                        StopAndReplay(now);
                    break;
            }
        }

        private void TryCaptureSkeleton()
        {
            if (_skeletonSource == null)
                return;
            // Capture each hand's mirrored skeleton independently — one can't substitute for the other.
            if ((_leftSkeleton == null || !_leftSkeleton.IsValid) &&
                _skeletonSource.TryGetSkeleton(false, out HandSkeleton left) && left.IsValid)
                _leftSkeleton = left;
            if ((_rightSkeleton == null || !_rightSkeleton.IsValid) &&
                _skeletonSource.TryGetSkeleton(true, out HandSkeleton right) && right.IsValid)
                _rightSkeleton = right;
        }

        private void PushFrame(float now)
        {
            _source.TryGetHead(out Pose head);
            if (!_source.TryGetHand(false, ref _left)) _left.boneRotations = null;
            if (!_source.TryGetHand(true, ref _right)) _right.boneRotations = null;
            _recorder.Push(now, head, _left, _right);
        }

        private void StopAndReplay(float now)
        {
            Timeline timeline = _recorder.Finish(now);
            timeline.leftSkeleton = _leftSkeleton;
            timeline.rightSkeleton = _rightSkeleton;

            var masterclass = new Masterclass { id = "mc-local", title = "First-light capture" };
            masterclass.chapters.Add(new Chapter { id = "ch-1", timeline = timeline });

            string path = Path.Combine(Application.persistentDataPath, _fileName);
            File.WriteAllText(path, _serializer.Serialize(masterclass));
            _saved = true;

            int lBones = _leftSkeleton != null && _leftSkeleton.IsValid ? _leftSkeleton.boneParents.Length : 0;
            int rBones = _rightSkeleton != null && _rightSkeleton.IsValid ? _rightSkeleton.boneParents.Length : 0;
            Debug.Log($"[FirstLightAutoCapture] Saved {timeline.frames.Count} frames (L:{lBones} R:{rBones} bones) to {path}. Replaying.");

            if (_player != null)
                _player.LoadAndPlay();
            else
                Debug.LogWarning("[FirstLightAutoCapture] No GhostHandPlayer assigned — recorded but not replaying.");
        }
    }
}

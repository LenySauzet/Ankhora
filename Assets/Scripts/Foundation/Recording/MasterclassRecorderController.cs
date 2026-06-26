using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Recording;
using Ankhora.Domain.Serialization;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Drives a single hands-only recording: a controller button toggles record; while recording it
    /// pushes head + both hands from an <see cref="IHandPoseSource"/> into a <see cref="TimelineRecorder"/>
    /// at a fixed rate; on stop it wraps the timeline in a Masterclass and writes it to device storage
    /// as JSON. The pure cadence/serialisation logic is tested in EditMode; this MonoBehaviour is the
    /// thin device-side wiring (verified on headset).
    /// </summary>
    public class MasterclassRecorderController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _poseSourceBehaviour; // must implement IHandPoseSource
        [SerializeField, Min(1f)] private float _sampleRateHz = 30f;
        [SerializeField] private OVRInput.Button _recordButton = OVRInput.Button.One; // A / X
        [SerializeField] private string _fileName = "masterclass.json";

        private IHandPoseSource _source;
        private TimelineRecorder _recorder;
        private readonly IMasterclassSerializer _serializer = new JsonMasterclassSerializer();
        private HandPose _left;   // reused buffers (boneRotations arrays are reused by the source)
        private HandPose _right;

        public bool IsRecording => _recorder != null && _recorder.IsRecording;
        public string SavedFilePath { get; private set; }

        private void Awake()
        {
            _source = _poseSourceBehaviour as IHandPoseSource;
            if (_source == null)
                Debug.LogError(
                    "[MasterclassRecorderController] _poseSourceBehaviour is unset or does not implement " +
                    "IHandPoseSource — recording is disabled until it is wired.", this);
            _recorder = new TimelineRecorder(_sampleRateHz);
            SavedFilePath = Path.Combine(Application.persistentDataPath, _fileName);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_recordButton))
                Toggle();

            if (!IsRecording || _source == null)
                return;

            float now = Time.unscaledTime;
            _source.TryGetHead(out Pose head);
            if (!_source.TryGetHand(false, ref _left)) _left.boneRotations = null;
            if (!_source.TryGetHand(true, ref _right)) _right.boneRotations = null;
            _recorder.Push(now, head, _left, _right);
        }

        public void Toggle()
        {
            if (IsRecording)
            {
                StopAndSave();
                return;
            }

            // Don't begin a doomed recording (it would write a zero-frame file on stop) when the
            // scene is misconfigured — the Awake error already explains why.
            if (_source == null)
            {
                Debug.LogWarning("[MasterclassRecorderController] Cannot start recording: no IHandPoseSource bound.");
                return;
            }

            _recorder.Begin(Time.unscaledTime);
        }

        private void StopAndSave()
        {
            Timeline timeline = _recorder.Finish(Time.unscaledTime);
            var masterclass = new Masterclass { id = "mc-local", title = "Local recording" };
            masterclass.chapters.Add(new Chapter { id = "ch-1", timeline = timeline });

            File.WriteAllText(SavedFilePath, _serializer.Serialize(masterclass));
            Debug.Log($"[MasterclassRecorderController] Saved {timeline.frames.Count} frames to {SavedFilePath}");
        }
    }
}

using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using Ankhora.Domain.Serialization;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Loads a recorded masterclass from device storage and replays its first chapter as ghost hands:
    /// advances a playback clock, samples both hands from the <see cref="Timeline"/> each frame
    /// (into reused arrays, no hot-loop allocation), and drives an <see cref="IHandView"/> per hand.
    /// A controller button starts playback; replay loops if enabled.
    /// </summary>
    public class GhostHandPlayer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _leftViewBehaviour;   // implements IHandView
        [SerializeField] private MonoBehaviour _rightViewBehaviour;  // implements IHandView
        [SerializeField] private OVRInput.Button _playButton = OVRInput.Button.Two; // shared B/Y? choose a free button
        [SerializeField] private string _fileName = "masterclass.json";
        [SerializeField] private bool _loop = true;
        [SerializeField, Min(1)] private int _boneCapacity = 19;

        private readonly IMasterclassSerializer _serializer = new JsonMasterclassSerializer();
        private IHandView _leftView;
        private IHandView _rightView;
        private Timeline _timeline;
        private Quaternion[] _leftBones;
        private Quaternion[] _rightBones;
        private float _clock;
        private bool _playing;

        private void Awake()
        {
            _leftView = _leftViewBehaviour as IHandView;
            _rightView = _rightViewBehaviour as IHandView;
            _leftBones = new Quaternion[_boneCapacity];
            _rightBones = new Quaternion[_boneCapacity];
            _leftView?.Show(false);
            _rightView?.Show(false);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_playButton))
                LoadAndPlay();

            if (!_playing || _timeline == null)
                return;

            _clock += Time.deltaTime;
            if (_clock >= _timeline.durationSeconds)
            {
                if (_loop) _clock = 0f;
                else { Stop(); return; }
            }

            DriveHand(_leftView, rightHand: false, _leftBones);
            DriveHand(_rightView, rightHand: true, _rightBones);
        }

        private void DriveHand(IHandView view, bool rightHand, Quaternion[] buffer)
        {
            if (view == null)
                return;
            bool tracked = TimelineSampler.SampleHand(_timeline, _clock, rightHand, buffer, out Pose root);
            view.Show(tracked);
            if (tracked)
                view.Apply(root, buffer, buffer.Length);
        }

        public void LoadAndPlay()
        {
            string path = Path.Combine(Application.persistentDataPath, _fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[GhostHandPlayer] No recording at {path}");
                return;
            }

            Masterclass mc;
            try { mc = _serializer.Deserialize(File.ReadAllText(path)); }
            catch (System.Exception e) { Debug.LogError($"[GhostHandPlayer] Load failed: {e.Message}"); return; }

            if (mc.chapters.Count == 0 || mc.chapters[0].timeline.frames.Count == 0)
            {
                Debug.LogWarning("[GhostHandPlayer] Recording has no frames.");
                return;
            }

            _timeline = mc.chapters[0].timeline;
            _leftView?.Bind(_timeline.leftSkeleton);
            _rightView?.Bind(_timeline.rightSkeleton);
            _clock = 0f;
            _playing = true;
        }

        public void Stop()
        {
            _playing = false;
            _leftView?.Show(false);
            _rightView?.Show(false);
        }
    }
}

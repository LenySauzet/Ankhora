using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using Ankhora.Foundation.Persistence;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Loads a recorded masterclass from device storage and replays its first chapter as ghost hands:
    /// advances a playback clock, samples both hands from the <see cref="Timeline"/> each frame
    /// (into reused arrays, no hot-loop allocation), and drives an <see cref="IHandView"/> per hand.
    /// Replay starts via <see cref="LoadAndPlay"/> — wired to the recorder's "saved" event in the scene,
    /// or to an optional controller button for re-watching. Replay loops if enabled.
    /// </summary>
    public class GhostHandPlayer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _leftViewBehaviour;   // implements IHandView
        [SerializeField] private MonoBehaviour _rightViewBehaviour;  // implements IHandView
        [Tooltip("Optional manual re-watch trigger. PrimaryIndexTrigger by default (free of the passthrough B/Y toggle).")]
        [SerializeField] private OVRInput.Button _playButton = OVRInput.Button.PrimaryIndexTrigger;
        [SerializeField] private string _storageDir = "mc-local";
        [SerializeField] private bool _loop = true;

        private MasterclassStore _store;
        private IHandView _leftView;
        private IHandView _rightView;
        private Timeline _timeline;
        private Quaternion[] _leftBones;
        private Quaternion[] _rightBones;
        private Vector3[] _leftBonePositions;
        private Vector3[] _rightBonePositions;
        private float _clock;
        private bool _playing;
        private bool _leftTracked;
        private bool _rightTracked;

        private void Awake()
        {
            _store = new MasterclassStore(_storageDir);
            _leftView = _leftViewBehaviour as IHandView;
            _rightView = _rightViewBehaviour as IHandView;
            _leftView?.Show(false);
            _rightView?.Show(false);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_playButton))
                LoadAndPlay();

            if (!_playing || _timeline == null)
                return;

            // Unscaled so replay speed is owned here, not coupled to Time.timeScale (until slow-mo lands).
            _clock += Time.unscaledDeltaTime;
            if (_clock >= _timeline.durationSeconds)
            {
                if (_loop) _clock = 0f;
                else { Stop(); return; }
            }

            _leftTracked = DriveHand(_leftView, rightHand: false, _leftBones, _leftBonePositions, _leftTracked);
            _rightTracked = DriveHand(_rightView, rightHand: true, _rightBones, _rightBonePositions, _rightTracked);
        }

        private bool DriveHand(IHandView view, bool rightHand, Quaternion[] buffer, Vector3[] positions, bool wasTracked)
        {
            if (view == null || buffer == null)
                return false;
            bool tracked = TimelineSampler.SampleHand(_timeline, _clock, rightHand, buffer, positions, out Pose root);
            if (tracked != wasTracked)        // toggle visibility only on a track/untrack transition
                view.Show(tracked);
            if (tracked)
                view.Apply(root, buffer, positions, buffer.Length);
            return tracked;
        }

        public void LoadAndPlay()
        {
            if (!_store.TryLoad(out Masterclass mc, out string error))
            {
                Debug.LogWarning($"[GhostHandPlayer] {error}");
                return;
            }

            if (mc.chapters.Count == 0 || mc.chapters[0].timeline.frames.Count == 0)
            {
                Debug.LogWarning("[GhostHandPlayer] Recording has no frames.");
                return;
            }

            _timeline = mc.chapters[0].timeline;
            EnsureBoneBuffers(_timeline);
            _leftView?.Bind(_timeline.leftSkeleton);
            _rightView?.Bind(_timeline.rightSkeleton);

            _clock = 0f;
            _playing = true;
            _leftTracked = _rightTracked = false;   // force a Show() on the first tracked frame
            _leftView?.Show(false);
            _rightView?.Show(false);
        }

        public void Stop()
        {
            _playing = false;
            _leftView?.Show(false);
            _rightView?.Show(false);
            _leftTracked = _rightTracked = false;
        }

        /// <summary>Size the reused sample buffers from the loaded recording's actual bone count.</summary>
        private void EnsureBoneBuffers(Timeline timeline)
        {
            int needed = Mathf.Max(BoneCount(timeline.leftSkeleton), BoneCount(timeline.rightSkeleton));
            if (needed == 0 && timeline.frames.Count > 0)
            {
                PoseFrame f = timeline.frames[0];
                needed = Mathf.Max(f.leftHand.boneRotations?.Length ?? 0, f.rightHand.boneRotations?.Length ?? 0);
            }
            needed = Mathf.Max(needed, 1);

            if (_leftBones == null || _leftBones.Length < needed)
            {
                _leftBones = new Quaternion[needed];
                _rightBones = new Quaternion[needed];
            }

            // Allocate each hand's position buffer independently, only if THAT hand carries per-frame bone
            // positions. A hand without them keeps a null buffer so replay falls back to its rest bind
            // offsets, rather than being driven with stale zeros from a buffer the sampler never fills.
            _leftBonePositions = EnsurePositionBuffer(
                _leftBonePositions, TimelineSampler.HasBoneLocalPositions(timeline, rightHand: false), needed);
            _rightBonePositions = EnsurePositionBuffer(
                _rightBonePositions, TimelineSampler.HasBoneLocalPositions(timeline, rightHand: true), needed);
        }

        private static Vector3[] EnsurePositionBuffer(Vector3[] buffer, bool hasPositions, int needed)
        {
            if (!hasPositions)
                return null;
            return (buffer == null || buffer.Length < needed) ? new Vector3[needed] : buffer;
        }

        private static int BoneCount(HandSkeleton s) => s != null && s.IsValid ? s.boneParents.Length : 0;
    }
}

using Ankhora.Domain.Audio;
using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Plays a recorded <see cref="VoiceTrack"/> spatialised from the ghost's head, locked to the replay
    /// clock the <see cref="GhostHandPlayer"/> owns (never its own clock). The AudioSource uses the Meta XR
    /// Audio spatialiser (set the project Spatializer Plugin to "Meta XR Audio"). Device-verified.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VoicePlayer : MonoBehaviour
    {
        [Tooltip("Re-seek the clip when |AudioSource.time - target| exceeds this (loop wrap, future scrub).")]
        [SerializeField, Min(0.02f)] private float _resyncThreshold = 0.08f;

        private AudioSource _source;
        private float _offset;
        private bool _loaded;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 1f;     // full 3D distance/pan
            _source.spatialize = true;     // route through the installed Meta XR Audio spatialiser (HRTF) — spatialBlend alone is only Unity's built-in pan
            // Keep the narration at full level across the working space: the HRTF still gives direction,
            // but distance shouldn't bury it. Linear falloff, full volume within a few metres of the ghost.
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = 4f;
            _source.maxDistance = 12f;
        }

        /// <summary>Decode the WAV blob into a clip and arm playback. No-op if the track has no clip.</summary>
        public void Load(byte[] wavBytes, VoiceTrack track)
        {
            Unload();   // release any previously-decoded clip before creating a new one
            if (track == null || !track.HasClip) return;
            if (!WavCodec.TryDecode(wavBytes, out float[] samples, out int sampleRate, out int channels))
            {
                Debug.LogWarning("[VoicePlayer] Could not decode voice clip; replay is hands-only.", this);
                return;
            }
            var clip = AudioClip.Create(track.clipRef, samples.Length / Mathf.Max(1, channels), channels, sampleRate, false);
            clip.SetData(samples, 0);
            _source.clip = clip;
            _offset = track.timelineOffsetSeconds;
            _loaded = true;
        }

        public void Stop()
        {
            if (_source != null && _source.isPlaying) _source.Stop();
        }

        /// <summary>Stop playback and release the runtime-decoded clip. <see cref="AudioClip.Create"/> clips are
        /// not collected with the hierarchy or owned by the <see cref="AudioSource"/>, so they must be freed
        /// explicitly (same ownership rule as the ghost mesh) — on every re-load, on disarm, and on destroy.
        /// Also clears <c>_loaded</c> so a stale clip can't resume on the next <see cref="Tick"/>.</summary>
        public void Unload()
        {
            if (_source != null)
            {
                if (_source.isPlaying) _source.Stop();
                if (_source.clip != null) { Destroy(_source.clip); _source.clip = null; }
            }
            _loaded = false;
        }

        private void OnDestroy() => Unload();

        /// <summary>Drive one frame from the owning player: position the source at the ghost head, keep the
        /// clip aligned to the clock, and play/pause with the replay. Called every frame, never self-clocked.</summary>
        public void Tick(float clock, bool playing, Vector3 headPosition)
        {
            if (!_loaded) return;
            transform.position = headPosition;   // voice emanates from the ghost's head as it moves

            float target = VoiceSync.AudioPlayhead(clock, _offset);

            if (!playing || target < 0f || target >= _source.clip.length)
            {
                if (_source.isPlaying) _source.Stop();
                return;
            }

            if (!_source.isPlaying)
            {
                _source.time = target;
                _source.Play();
            }
            else if (Mathf.Abs(_source.time - target) > _resyncThreshold)
            {
                _source.time = target;   // loop wrap / large jump / future scrub
            }
        }
    }
}

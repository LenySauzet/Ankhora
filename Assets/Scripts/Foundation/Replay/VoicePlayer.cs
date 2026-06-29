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
            _source.spatialBlend = 1f;   // full 3D; the Meta XR Audio spatialiser does the rest
        }

        /// <summary>Decode the WAV blob into a clip and arm playback. No-op if the track has no clip.</summary>
        public void Load(byte[] wavBytes, VoiceTrack track)
        {
            _loaded = false;
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

using Ankhora.Domain.Audio;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Captures the Instructor's narration via <see cref="Microphone"/> alongside a hands take and emits it
    /// as a 16-bit PCM WAV (<see cref="WavCodec"/>). Implements <see cref="IVoiceCaptureSource"/> so
    /// <see cref="RecordingSession"/> drives it. The real start-of-audio offset is measured from the first
    /// frame the mic reports samples, so warm-up latency does not desync replay. Device/Play-Mode verified.
    /// </summary>
    public class VoiceRecorder : MonoBehaviour, IVoiceCaptureSource
    {
        [SerializeField, Min(8000)] private int _requestedSampleRate = 16000;
        [SerializeField, Min(1)] private int _maxSeconds = 600;   // ring-buffer ceiling for one take

        private string _device;
        private AudioClip _clip;
        private bool _capturing;
        private float _beginNow;            // timeline zero (the recorder's clock)
        private float _firstSampleOffset;   // seconds from _beginNow to the first delivered sample
        private bool _firstSampleSeen;

        public bool IsAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone)) return false;
#endif
                return Microphone.devices != null && Microphone.devices.Length > 0;
            }
        }

        private void OnEnable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                Permission.RequestUserPermission(Permission.Microphone);
#endif
        }

        public void BeginCapture(float now)
        {
            if (!IsAvailable) return;
            _device = Microphone.devices[0];
            _beginNow = now;
            _firstSampleOffset = 0f;
            _firstSampleSeen = false;
            _clip = Microphone.Start(_device, loop: false, _maxSeconds, _requestedSampleRate);
            _capturing = true;
        }

        private void Update()
        {
            if (!_capturing || _firstSampleSeen) return;
            if (Microphone.GetPosition(_device) > 0)
            {
                _firstSampleOffset = Mathf.Max(0f, Time.unscaledTime - _beginNow);
                _firstSampleSeen = true;
            }
        }

        public bool TryEndCapture(float now, out VoiceCaptureResult result)
        {
            result = default;
            if (!_capturing || _clip == null) return false;

            int sampleCount = Microphone.GetPosition(_device);   // samples written so far (per channel)
            Microphone.End(_device);
            _capturing = false;
            if (sampleCount <= 0) { ReleaseClip(); return false; }

            int channels = _clip.channels;
            int sampleRate = _clip.frequency;
            if (channels != 1)
                Debug.LogWarning($"[VoiceRecorder] Mic reports {channels} channels; the voice path expects mono.", this);
            var data = new float[sampleCount * channels];
            _clip.GetData(data, 0);
            AudioLevels.NormalizeLoudness(data);   // the Quest mic captures at a low gain — lift perceived loudness (RMS) before encoding

            result = new VoiceCaptureResult
            {
                wavBytes = WavCodec.Encode(data, sampleRate, channels),
                sampleRate = sampleRate,
                channels = channels,
                timelineOffsetSeconds = _firstSampleOffset,
                durationSeconds = (float)sampleCount / sampleRate
            };
            ReleaseClip();
            return true;
        }

        private void OnDisable()
        {
            if (_capturing) { Microphone.End(_device); _capturing = false; }
            ReleaseClip();
        }

        /// <summary>Microphone.Start allocates a runtime AudioClip that is not collected with the hierarchy,
        /// so it must be destroyed explicitly on every capture-end path — otherwise each take leaks its
        /// native PCM buffer until scene unload.</summary>
        private void ReleaseClip()
        {
            if (_clip != null) { Destroy(_clip); _clip = null; }
        }
    }
}

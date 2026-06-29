using System;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// Metadata for one Chapter's recorded narration: a relative blob path + the timing needed to play it
    /// back in sync with the Hands Track. The audio bytes themselves live in a sibling blob, not here.
    /// </summary>
    [Serializable]
    public class VoiceTrack
    {
        public string clipRef;               // relative blob path, e.g. "voice-ch-1.wav"
        public int sampleRate;               // e.g. 16000
        public int channels;                 // 1 (mono)
        public float timelineOffsetSeconds;  // timeline t=0 -> first real audio sample (mic warm-up)
        public float durationSeconds;

        /// <summary>True when this track points at a real clip. The discriminator for "has voice", because
        /// JsonUtility cannot round-trip a null nested object (mirrors <see cref="HandSkeleton.IsValid"/>).</summary>
        public bool HasClip => !string.IsNullOrEmpty(clipRef);
    }
}

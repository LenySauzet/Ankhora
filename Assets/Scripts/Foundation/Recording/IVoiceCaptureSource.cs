namespace Ankhora.Foundation.Recording
{
    /// <summary>The voice equivalent of <see cref="IHandPoseSource"/>: a capture lifecycle the recorder
    /// drives alongside the hands. A null/unavailable source degrades the take to hands-only.</summary>
    public interface IVoiceCaptureSource
    {
        /// <summary>True when capture is possible right now (mic present + permission granted).</summary>
        bool IsAvailable { get; }

        /// <summary>Start capturing; <paramref name="now"/> is the same timeline zero the recorder uses.</summary>
        void BeginCapture(float now);

        /// <summary>Stop capturing and emit the encoded take. Returns false if nothing usable was captured.</summary>
        bool TryEndCapture(float now, out VoiceCaptureResult result);
    }

    /// <summary>An encoded voice take: WAV bytes + the metadata the manifest stores.</summary>
    public struct VoiceCaptureResult
    {
        public byte[] wavBytes;
        public int sampleRate;
        public int channels;
        public float timelineOffsetSeconds;
        public float durationSeconds;
    }
}

namespace Ankhora.Domain.Sampling
{
    /// <summary>
    /// Pure replay-clock → audio-playhead mapping. Kept off any MonoBehaviour so the sync contract
    /// is EditMode-testable. A value &lt; 0 means the clock is before the audio's first sample (silence);
    /// the player keeps the source silent until it reaches 0.
    /// </summary>
    public static class VoiceSync
    {
        public static float AudioPlayhead(float clock, float timelineOffsetSeconds)
            => clock - timelineOffsetSeconds;
    }
}

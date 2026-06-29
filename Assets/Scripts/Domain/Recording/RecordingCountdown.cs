using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>The phase of a pinch-armed take's lead-in.</summary>
    public enum CountdownPhase
    {
        Counting,
        Live,
    }

    /// <summary>
    /// Pure lead-in gate for a pinch-armed take: a fixed countdown, then live (recording). All queries
    /// are functions of seconds elapsed since the take was armed, so the driving MonoBehaviour holds no
    /// timing state of its own. The stop is event-driven (a second pinch), so unlike the retired
    /// <c>AutoCaptureClock</c> this owns no record duration.
    /// </summary>
    public class RecordingCountdown
    {
        private readonly float _countdown;

        public RecordingCountdown(float countdownSeconds)
        {
            _countdown = Mathf.Max(0f, countdownSeconds);
        }

        public CountdownPhase PhaseAt(float elapsed) =>
            elapsed < _countdown ? CountdownPhase.Counting : CountdownPhase.Live;

        /// <summary>Integer seconds still to show (3 → 2 → 1 → 0); 0 once live.</summary>
        public int SecondsRemaining(float elapsed)
        {
            float remaining = _countdown - elapsed;
            if (remaining <= 0f)
                return 0;
            return Mathf.Clamp(Mathf.CeilToInt(remaining), 0, Mathf.CeilToInt(_countdown));
        }
    }
}

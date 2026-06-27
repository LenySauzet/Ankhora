using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>The phase of the buttonless first-light capture schedule.</summary>
    public enum AutoCapturePhase
    {
        Countdown,
        Recording,
        Done,
    }

    /// <summary>
    /// Pure schedule for the first-light auto-capture: a fixed countdown, then a fixed recording
    /// window, then done. All queries are functions of elapsed seconds since the harness started, so
    /// the driving MonoBehaviour holds no timing state of its own.
    /// </summary>
    public class AutoCaptureClock
    {
        private readonly float _countdown;
        private readonly float _record;

        public AutoCaptureClock(float countdownSeconds, float recordSeconds)
        {
            _countdown = Mathf.Max(0f, countdownSeconds);
            _record = Mathf.Max(0f, recordSeconds);
        }

        public AutoCapturePhase PhaseAt(float elapsed)
        {
            if (elapsed < _countdown)
                return AutoCapturePhase.Countdown;
            if (elapsed < _countdown + _record)
                return AutoCapturePhase.Recording;
            return AutoCapturePhase.Done;
        }

        /// <summary>Seconds elapsed inside the recording window (0 during countdown, clamped to its length).</summary>
        public float RecordElapsed(float elapsed) => Mathf.Clamp(elapsed - _countdown, 0f, _record);

        /// <summary>Seconds left on the countdown (0 once recording starts).</summary>
        public float CountdownRemaining(float elapsed) => Mathf.Max(0f, _countdown - elapsed);
    }
}

using Ankhora.Domain.Recording;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The buttonless first-light harness runs a fixed schedule: a countdown, then a recording
    /// window, then done. This pins the pure phase logic so the MonoBehaviour stays a thin shell
    /// (hand tracking can't be exercised in the macOS Editor).
    /// </summary>
    public class AutoCaptureClockTests
    {
        private static AutoCaptureClock Clock() => new AutoCaptureClock(countdownSeconds: 3f, recordSeconds: 8f);

        [Test]
        public void PhaseAt_BeforeCountdownEnds_IsCountdown()
        {
            Assert.AreEqual(AutoCapturePhase.Countdown, Clock().PhaseAt(0f));
            Assert.AreEqual(AutoCapturePhase.Countdown, Clock().PhaseAt(2.99f));
        }

        [Test]
        public void PhaseAt_WithinRecordWindow_IsRecording()
        {
            Assert.AreEqual(AutoCapturePhase.Recording, Clock().PhaseAt(3f));
            Assert.AreEqual(AutoCapturePhase.Recording, Clock().PhaseAt(10.99f));
        }

        [Test]
        public void PhaseAt_AfterRecordWindow_IsDone()
        {
            Assert.AreEqual(AutoCapturePhase.Done, Clock().PhaseAt(11f));
            Assert.AreEqual(AutoCapturePhase.Done, Clock().PhaseAt(100f));
        }

        [Test]
        public void RecordElapsed_IsZeroDuringCountdown_AndClampedToWindow()
        {
            AutoCaptureClock c = Clock();
            Assert.That(c.RecordElapsed(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(c.RecordElapsed(5f), Is.EqualTo(2f).Within(1e-4f));   // 5 - 3 countdown
            Assert.That(c.RecordElapsed(50f), Is.EqualTo(8f).Within(1e-4f));  // clamped to record window
        }

        [Test]
        public void CountdownRemaining_TicksDownToZero()
        {
            AutoCaptureClock c = Clock();
            Assert.That(c.CountdownRemaining(0f), Is.EqualTo(3f).Within(1e-4f));
            Assert.That(c.CountdownRemaining(1.2f), Is.EqualTo(1.8f).Within(1e-4f));
            Assert.That(c.CountdownRemaining(5f), Is.EqualTo(0f).Within(1e-4f));
        }
    }
}

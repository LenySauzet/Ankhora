using Ankhora.Domain.Recording;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The pinch-armed take runs a fixed 3-2-1 lead-in before recording, kept out of the recorded
    /// window. This pins the pure phase + remaining-seconds logic so the device trigger stays a thin
    /// shell (hand tracking can't be exercised in the macOS Editor).
    /// </summary>
    public class RecordingCountdownTests
    {
        private static RecordingCountdown Countdown() => new RecordingCountdown(countdownSeconds: 3f);

        [Test]
        public void PhaseAt_WhileInsideCountdown_IsCounting()
        {
            Assert.AreEqual(CountdownPhase.Counting, Countdown().PhaseAt(0f));
            Assert.AreEqual(CountdownPhase.Counting, Countdown().PhaseAt(2.99f));
        }

        [Test]
        public void PhaseAt_AtAndAfterCountdownEnd_IsLive()
        {
            Assert.AreEqual(CountdownPhase.Live, Countdown().PhaseAt(3f));
            Assert.AreEqual(CountdownPhase.Live, Countdown().PhaseAt(100f));
        }

        [Test]
        public void SecondsRemaining_CountsDownThreeTwoOne()
        {
            RecordingCountdown c = Countdown();
            Assert.AreEqual(3, c.SecondsRemaining(0f));     // just armed: "3"
            Assert.AreEqual(3, c.SecondsRemaining(0.5f));   // still showing "3"
            Assert.AreEqual(2, c.SecondsRemaining(1f));     // "2"
            Assert.AreEqual(1, c.SecondsRemaining(2f));     // "1"
            Assert.AreEqual(0, c.SecondsRemaining(3f));     // recording starts
        }

        [Test]
        public void SecondsRemaining_IsZeroOnceLive_AndNeverNegative()
        {
            Assert.AreEqual(0, Countdown().SecondsRemaining(50f));
        }

        [Test]
        public void NegativeCountdown_IsClampedToImmediatelyLive()
        {
            var c = new RecordingCountdown(countdownSeconds: -2f);
            Assert.AreEqual(CountdownPhase.Live, c.PhaseAt(0f));
            Assert.AreEqual(0, c.SecondsRemaining(0f));
        }
    }
}

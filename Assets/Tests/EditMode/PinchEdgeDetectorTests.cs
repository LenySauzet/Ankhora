using Ankhora.Domain.Recording;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The pinch toggle must fire once per deliberate pinch, never on jitter and never twice for one
    /// hold. This pins the pure rising-edge + debounce logic so the device trigger stays a thin shell.
    /// </summary>
    public class PinchEdgeDetectorTests
    {
        private const float Dt = 1f / 30f;   // ~33 ms per frame

        [Test]
        public void Tick_FiresOnceAfterDebounce_WhenHeld()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            Assert.IsFalse(d.Tick(true, Dt));   // ~33 ms held — below 50 ms debounce
            Assert.IsTrue(d.Tick(true, Dt));    // ~66 ms held — fires
            Assert.IsFalse(d.Tick(true, Dt));   // still held — no second fire
            Assert.IsFalse(d.Tick(true, Dt));
        }

        [Test]
        public void Tick_NeverFiresWhileReleased()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            Assert.IsFalse(d.Tick(false, Dt));
            Assert.IsFalse(d.Tick(false, Dt));
        }

        [Test]
        public void Tick_RejectsJitter_ShorterThanDebounce()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            Assert.IsFalse(d.Tick(true, Dt));   // ~33 ms then released — too short
            Assert.IsFalse(d.Tick(false, Dt));
            Assert.IsFalse(d.Tick(false, Dt));
        }

        [Test]
        public void Tick_RearmsAfterRelease_ForNextPinch()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            d.Tick(true, Dt);
            Assert.IsTrue(d.Tick(true, Dt));    // first pinch fires
            Assert.IsFalse(d.Tick(false, Dt));  // released
            Assert.IsFalse(d.Tick(true, Dt));   // second pinch: debounce again
            Assert.IsTrue(d.Tick(true, Dt));    // second pinch fires
        }

        [Test]
        public void ZeroDebounce_FiresOnFirstHeldFrame()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0f);
            Assert.IsTrue(d.Tick(true, Dt));
            Assert.IsFalse(d.Tick(true, Dt));
        }
    }
}

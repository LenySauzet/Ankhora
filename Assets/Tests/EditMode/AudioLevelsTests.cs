using Ankhora.Domain.Audio;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class AudioLevelsTests
    {
        [Test]
        public void PeakNormalize_QuietSignal_BoostsToTargetPeak()
        {
            var s = new[] { 0.1f, -0.05f, 0.08f };
            AudioLevels.PeakNormalize(s, 0.95f);
            Assert.That(MaxAbs(s), Is.EqualTo(0.95f).Within(1e-4f));
        }

        [Test]
        public void PeakNormalize_HotSignal_ScalesDownToTargetPeak()
        {
            var s = new[] { 1.0f, -0.9f, 0.5f };
            AudioLevels.PeakNormalize(s, 0.95f);
            Assert.That(MaxAbs(s), Is.EqualTo(0.95f).Within(1e-4f));
        }

        [Test]
        public void PeakNormalize_PreservesRelativeShape()
        {
            var s = new[] { 0.1f, 0.2f };
            AudioLevels.PeakNormalize(s, 0.9f);
            Assert.That(s[1] / s[0], Is.EqualTo(2f).Within(1e-4f));   // ratio preserved
        }

        [Test]
        public void PeakNormalize_Silence_IsUnchanged()
        {
            var s = new[] { 0f, 0f, 0f };
            AudioLevels.PeakNormalize(s);
            Assert.That(s, Is.EqualTo(new[] { 0f, 0f, 0f }));
        }

        [Test]
        public void PeakNormalize_NullOrEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => AudioLevels.PeakNormalize(null));
            Assert.DoesNotThrow(() => AudioLevels.PeakNormalize(new float[0]));
        }

        private static float MaxAbs(float[] s)
        {
            float m = 0f;
            foreach (var v in s) { float a = v < 0f ? -v : v; if (a > m) m = a; }
            return m;
        }
    }
}

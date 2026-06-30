using Ankhora.Domain.Audio;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class AudioLevelsTests
    {
        [Test]
        public void NormalizeLoudness_QuietSignal_RaisesRmsToTarget()
        {
            // A constant-magnitude signal has RMS == its magnitude, so the target is reached exactly
            // (gain 0.22/0.05 = 4.4, below the cap; no limiting needed).
            var s = Filled(64, 0.05f);
            AudioLevels.NormalizeLoudness(s, targetRms: 0.22f);
            Assert.That(Rms(s), Is.EqualTo(0.22f).Within(1e-3f));
        }

        [Test]
        public void NormalizeLoudness_VeryQuietSignal_GainIsCapped()
        {
            // RMS 0.001 would need 220x to hit target; the cap (14x) bounds it to 0.014, not 0.22.
            var s = Filled(64, 0.001f);
            AudioLevels.NormalizeLoudness(s, targetRms: 0.22f, maxGain: 14f);
            Assert.That(Rms(s), Is.EqualTo(0.014f).Within(1e-4f));
        }

        [Test]
        public void NormalizeLoudness_TransientPeak_IsLimited()
        {
            // Quiet body + one loud transient: the makeup gain pushes the transient past the limit,
            // which must be clamped rather than clipped to an overflow.
            var s = Filled(64, 0.02f);
            s[10] = 0.95f;
            AudioLevels.NormalizeLoudness(s, targetRms: 0.22f, maxGain: 14f, limit: 0.98f);
            Assert.That(MaxAbs(s), Is.EqualTo(0.98f).Within(1e-4f));
        }

        [Test]
        public void NormalizeLoudness_Silence_IsUnchanged()
        {
            var s = new[] { 0f, 0f, 0f };
            AudioLevels.NormalizeLoudness(s);
            Assert.That(s, Is.EqualTo(new[] { 0f, 0f, 0f }));
        }

        [Test]
        public void NormalizeLoudness_NullOrEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => AudioLevels.NormalizeLoudness(null));
            Assert.DoesNotThrow(() => AudioLevels.NormalizeLoudness(new float[0]));
        }

        private static float[] Filled(int n, float v)
        {
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = v;
            return a;
        }

        private static float Rms(float[] s)
        {
            double sum = 0.0;
            foreach (var v in s) sum += (double)v * v;
            return (float)System.Math.Sqrt(sum / s.Length);
        }

        private static float MaxAbs(float[] s)
        {
            float m = 0f;
            foreach (var v in s) { float a = v < 0f ? -v : v; if (a > m) m = a; }
            return m;
        }
    }
}

using System;

namespace Ankhora.Domain.Audio
{
    /// <summary>
    /// Pure level utilities for captured PCM. Kept in Domain (no Unity audio types) so the DSP is
    /// EditMode-testable; the Foundation recorder applies it before <see cref="WavCodec"/> encoding.
    /// </summary>
    public static class AudioLevels
    {
        /// <summary>
        /// Loudness-normalises <paramref name="samples"/> in place: applies a makeup gain so the signal's
        /// RMS (perceived loudness, not just peak) reaches <paramref name="targetRms"/>, capped at
        /// <paramref name="maxGain"/> so a near-silent take doesn't blow up its noise floor, then hard-limits
        /// to ±<paramref name="limit"/> so the occasional transient can't clip. Peak normalisation alone
        /// leaves quiet, dynamic speech sounding faint — the Quest mic captures at a low gain — so targeting
        /// RMS is what makes the narration actually audible. A silent/null/empty buffer is left untouched.
        /// Operates in place to avoid a second buffer on device.
        /// </summary>
        public static void NormalizeLoudness(float[] samples, float targetRms = 0.22f, float maxGain = 14f, float limit = 0.98f)
        {
            if (samples == null || samples.Length == 0 || targetRms <= 0f) return;

            double sumSq = 0.0;
            for (int i = 0; i < samples.Length; i++)
                sumSq += (double)samples[i] * samples[i];
            float rms = (float)Math.Sqrt(sumSq / samples.Length);
            if (rms <= 1e-6f) return;   // silence — nothing to lift, and avoids div-by-zero

            float gain = targetRms / rms;
            if (gain > maxGain) gain = maxGain;

            for (int i = 0; i < samples.Length; i++)
            {
                float v = samples[i] * gain;
                if (v > limit) v = limit;
                else if (v < -limit) v = -limit;
                samples[i] = v;
            }
        }
    }
}

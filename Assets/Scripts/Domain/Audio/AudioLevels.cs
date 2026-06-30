namespace Ankhora.Domain.Audio
{
    /// <summary>
    /// Pure level utilities for captured PCM. Kept in Domain (no Unity audio types) so the DSP is
    /// EditMode-testable; the Foundation recorder applies it before <see cref="WavCodec"/> encoding.
    /// </summary>
    public static class AudioLevels
    {
        /// <summary>
        /// Peak-normalises <paramref name="samples"/> in place so the loudest sample reaches
        /// <paramref name="targetPeak"/> (default 0.95, leaving headroom). Quiet captures (the Quest mic
        /// runs at a low gain) are boosted; hot captures are scaled down to avoid clipping. A silent,
        /// null, or empty buffer is left untouched. Operates in place to avoid a second buffer on device.
        /// </summary>
        public static void PeakNormalize(float[] samples, float targetPeak = 0.95f)
        {
            if (samples == null || samples.Length == 0 || targetPeak <= 0f) return;

            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float a = samples[i] < 0f ? -samples[i] : samples[i];
                if (a > peak) peak = a;
            }
            if (peak <= 1e-6f) return;   // silence — nothing to scale, and avoids div-by-zero

            float gain = targetPeak / peak;
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= gain;
        }
    }
}

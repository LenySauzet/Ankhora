using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ankhora.Domain.Audio
{
    /// <summary>
    /// Pure encode/decode between PCM float samples ([-1, 1]) and a canonical 16-bit little-endian WAV byte
    /// stream. Kept in Domain (no Unity runtime audio types) so the wire format is EditMode-testable; the
    /// Foundation layer bridges it to <c>AudioClip</c>/<c>Microphone</c>. No external encoder dependency.
    /// </summary>
    public static class WavCodec
    {
        private const int HeaderBytes = 44;
        private const int BitsPerSample = 16;

        public static byte[] Encode(float[] samples, int sampleRate, int channels)
        {
            samples ??= Array.Empty<float>();
            int dataBytes = samples.Length * 2;
            using var ms = new MemoryStream(HeaderBytes + dataBytes);
            using var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
            int byteRate = sampleRate * channels * (BitsPerSample / 8);
            short blockAlign = (short)(channels * (BitsPerSample / 8));

            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataBytes);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);                       // PCM fmt chunk size
            w.Write((short)1);                 // PCM
            w.Write((short)channels);
            w.Write(sampleRate);
            w.Write(byteRate);
            w.Write(blockAlign);
            w.Write((short)BitsPerSample);
            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(dataBytes);
            for (int i = 0; i < samples.Length; i++)
                w.Write((short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f));

            w.Flush();
            return ms.ToArray();
        }

        public static bool TryDecode(byte[] wav, out float[] samples, out int sampleRate, out int channels)
        {
            samples = Array.Empty<float>();
            sampleRate = 0;
            channels = 0;
            if (wav == null || wav.Length < HeaderBytes) return false;
            if (wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F') return false;
            if (wav[8] != 'W' || wav[9] != 'A' || wav[10] != 'V' || wav[11] != 'E') return false;

            channels = wav[22] | (wav[23] << 8);
            sampleRate = wav[24] | (wav[25] << 8) | (wav[26] << 16) | (wav[27] << 24);
            int dataBytes = wav[40] | (wav[41] << 8) | (wav[42] << 16) | (wav[43] << 24);
            dataBytes = Mathf.Clamp(dataBytes, 0, wav.Length - HeaderBytes);

            int count = dataBytes / 2;
            samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                short s = (short)(wav[HeaderBytes + i * 2] | (wav[HeaderBytes + i * 2 + 1] << 8));
                samples[i] = s / 32767f;
            }
            return channels > 0 && sampleRate > 0;
        }
    }
}

// Assets/Tests/EditMode/WavCodecTests.cs
using Ankhora.Domain.Audio;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class WavCodecTests
    {
        [Test]
        public void Encode_WritesRiffWaveHeaderAndPcmDataSize()
        {
            byte[] wav = WavCodec.Encode(new[] { 0f, 0f, 0f }, 16000, 1);
            Assert.AreEqual('R', (char)wav[0]); Assert.AreEqual('I', (char)wav[1]);
            Assert.AreEqual('F', (char)wav[2]); Assert.AreEqual('F', (char)wav[3]);
            Assert.AreEqual('W', (char)wav[8]); Assert.AreEqual('A', (char)wav[9]);
            Assert.AreEqual('V', (char)wav[10]); Assert.AreEqual('E', (char)wav[11]);
            Assert.AreEqual(44 + 3 * 2, wav.Length, "44-byte header + 3 mono 16-bit samples");
        }

        [Test]
        public void Encode_MapsFullScaleSamplesToInt16Extremes()
        {
            byte[] wav = WavCodec.Encode(new[] { 1f, -1f, 0f, 2f }, 16000, 1);  // 2f must clamp to +full-scale
            short S(int i) => (short)(wav[44 + i * 2] | (wav[44 + i * 2 + 1] << 8));
            Assert.AreEqual(32767, S(0));
            Assert.AreEqual(-32767, S(1));
            Assert.AreEqual(0, S(2));
            Assert.AreEqual(32767, S(3));  // clamped
        }

        [Test]
        public void EncodeThenDecode_RoundTripsSamplesAndFormat()
        {
            var src = new[] { 0f, 0.5f, -0.5f, 0.999f, -0.999f };
            Assert.IsTrue(WavCodec.TryDecode(WavCodec.Encode(src, 16000, 1), out float[] outS, out int sr, out int ch));
            Assert.AreEqual(16000, sr);
            Assert.AreEqual(1, ch);
            Assert.AreEqual(src.Length, outS.Length);
            for (int i = 0; i < src.Length; i++)
                Assert.That(outS[i], Is.EqualTo(src[i]).Within(1f / 32767f + 1e-5f));
        }

        [Test]
        public void TryDecode_NonRiffBytes_ReturnsFalse()
            => Assert.IsFalse(WavCodec.TryDecode(new byte[] { 1, 2, 3, 4 }, out _, out _, out _));

        [Test]
        public void TryDecode_NonPcmFormat_ReturnsFalse()
        {
            byte[] wav = WavCodec.Encode(new[] { 0.1f, -0.1f }, 16000, 1);
            wav[20] = 2;   // audioFormat PCM(1) -> non-PCM
            Assert.IsFalse(WavCodec.TryDecode(wav, out _, out _, out _));
        }

        [Test]
        public void TryDecode_ClampsInt16MinToMinusOne()
        {
            byte[] wav = WavCodec.Encode(new[] { 0f }, 16000, 1);   // one mono sample
            wav[44] = 0x00; wav[45] = 0x80;                          // overwrite to int16 min (-32768)
            Assert.IsTrue(WavCodec.TryDecode(wav, out float[] s, out _, out _));
            Assert.That(s[0], Is.EqualTo(-1f).Within(1e-6f));        // clamped, not -1.00003
        }
    }
}

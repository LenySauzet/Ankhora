using Ankhora.Domain.Sampling;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class VoiceSyncTests
    {
        [Test]
        public void AudioPlayhead_AtOffset_IsZero()
            => Assert.That(VoiceSync.AudioPlayhead(0.2f, 0.2f), Is.EqualTo(0f).Within(1e-6f));

        [Test]
        public void AudioPlayhead_MidClip_IsClockMinusOffset()
            => Assert.That(VoiceSync.AudioPlayhead(1.5f, 0.2f), Is.EqualTo(1.3f).Within(1e-6f));

        [Test]
        public void AudioPlayhead_BeforeAudioStarts_IsNegative()
            => Assert.That(VoiceSync.AudioPlayhead(0.1f, 0.2f), Is.LessThan(0f));
    }
}

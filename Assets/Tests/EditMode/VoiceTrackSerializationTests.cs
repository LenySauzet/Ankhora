using Ankhora.Domain.Model;
using Ankhora.Domain.Serialization;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class VoiceTrackSerializationTests
    {
        [Test]
        public void Timeline_WithVoiceTrack_RoundTripsAllFields()
        {
            var mc = new Masterclass { id = "mc-local", title = "t" };
            var tl = new Timeline { durationSeconds = 2f };
            tl.voiceTrack = new VoiceTrack
            {
                clipRef = "voice-ch-1.wav", sampleRate = 16000, channels = 1,
                timelineOffsetSeconds = 0.12f, durationSeconds = 1.9f
            };
            mc.chapters.Add(new Chapter { id = "ch-1", timeline = tl });

            var ser = new JsonMasterclassSerializer();
            VoiceTrack vt = ser.Deserialize(ser.Serialize(mc)).chapters[0].timeline.voiceTrack;

            Assert.IsTrue(vt.HasClip);
            Assert.AreEqual("voice-ch-1.wav", vt.clipRef);
            Assert.AreEqual(16000, vt.sampleRate);
            Assert.AreEqual(1, vt.channels);
            Assert.That(vt.timelineOffsetSeconds, Is.EqualTo(0.12f).Within(1e-4f));
            Assert.That(vt.durationSeconds, Is.EqualTo(1.9f).Within(1e-4f));
        }

        [Test]
        public void Timeline_NullVoiceTrack_RoundTripsToNotHasClip()
        {
            // JsonUtility cannot preserve null for nested [Serializable] fields: a null voiceTrack comes back
            // as a non-null default object. "No voice" is therefore discriminated by HasClip, not by null.
            var mc = new Masterclass { id = "mc-local", title = "t" };
            mc.chapters.Add(new Chapter { id = "ch-1", timeline = new Timeline { voiceTrack = null } });

            var ser = new JsonMasterclassSerializer();
            VoiceTrack vt = ser.Deserialize(ser.Serialize(mc)).chapters[0].timeline.voiceTrack;

            Assert.IsFalse(vt != null && vt.HasClip, "a hands-only take must not read as having a clip");
        }
    }
}

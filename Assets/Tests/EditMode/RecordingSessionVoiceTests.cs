using Ankhora.Domain.Model;
using Ankhora.Foundation.Persistence;
using Ankhora.Foundation.Recording;
using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class RecordingSessionVoiceTests
    {
        // Minimal stubs (no headset): a pose source that yields one tracked frame, and a voice source that
        // returns canned WAV bytes.
        private class StubPose : IHandPoseSource
        {
            public bool TryGetHead(out Pose head) { head = Pose.identity; return true; }
            public bool TryGetHand(bool rightHand, ref HandPose pose)
            { pose.boneRotations = new[] { Quaternion.identity }; pose.boneLocalPositions = new[] { Vector3.zero }; return true; }
        }
        private class StubVoice : IVoiceCaptureSource
        {
            public bool Began, Ended;
            public bool IsAvailable => true;
            public void BeginCapture(float now) => Began = true;
            public bool TryEndCapture(float now, out VoiceCaptureResult r)
            {
                Ended = true;
                r = new VoiceCaptureResult { wavBytes = new byte[] { 9, 9, 9 }, sampleRate = 16000, channels = 1,
                    timelineOffsetSeconds = 0.1f, durationSeconds = 0.5f };
                return true;
            }
        }

        private string _dir;
        [SetUp] public void SetUp() => _dir = "test-mc-" + System.Guid.NewGuid().ToString("N");
        [TearDown] public void TearDown()
        {
            string p = Path.Combine(Application.persistentDataPath, _dir);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }

        [Test]
        public void SaveTo_WithVoiceSource_WritesBlobAndVoiceTrack()
        {
            var voice = new StubVoice();
            var session = new RecordingSession(new StubPose(), 30f, voice);
            var store = new MasterclassStore(_dir);

            session.Begin(0f);
            session.Tick(0f);
            Assert.IsTrue(session.SaveTo(store, 1f, out _, out string error), error);

            Assert.IsTrue(voice.Began && voice.Ended);
            Assert.IsTrue(store.TryLoad(out Masterclass mc, out _));
            VoiceTrack vt = mc.chapters[0].timeline.voiceTrack;
            Assert.IsTrue(vt.HasClip);
            Assert.AreEqual("voice-ch-1.wav", vt.clipRef);
            Assert.That(vt.timelineOffsetSeconds, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.IsTrue(store.ReadBlob(vt.clipRef, out byte[] blob, out _));
            Assert.AreEqual(new byte[] { 9, 9, 9 }, blob);
        }

        [Test]
        public void SaveTo_NoVoiceSource_LeavesHandsOnlyTake()
        {
            var session = new RecordingSession(new StubPose(), 30f);   // no voice
            var store = new MasterclassStore(_dir);
            session.Begin(0f); session.Tick(0f);
            Assert.IsTrue(session.SaveTo(store, 1f, out _, out _));
            Assert.IsTrue(store.TryLoad(out Masterclass mc, out string loadError), loadError);
            Assert.IsFalse(mc.chapters[0].timeline.voiceTrack != null && mc.chapters[0].timeline.voiceTrack.HasClip);
        }
    }
}

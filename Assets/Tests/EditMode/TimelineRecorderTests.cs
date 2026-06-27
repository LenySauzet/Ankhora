using Ankhora.Domain.Model;
using Ankhora.Domain.Recording;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class TimelineRecorderTests
    {
        // 10 Hz -> 0.1 s interval, for round numbers.
        private static TimelineRecorder TenHz() => new TimelineRecorder(10f);
        private static HandPose Tracked() => new HandPose { boneRotations = new[] { Quaternion.identity } };

        [Test]
        public void Begin_ThenFirstPush_EmitsFrameAtZero()
        {
            var rec = TenHz();
            rec.Begin(100f);                                  // non-zero start: t must be relative
            rec.Push(100f, default, Tracked(), Tracked());
            Timeline tl = rec.Finish(100f);

            Assert.AreEqual(1, tl.frames.Count);
            Assert.That(tl.frames[0].t, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Push_WithinInterval_DoesNotEmitSecondFrame()
        {
            var rec = TenHz();
            rec.Begin(0f);
            rec.Push(0f, default, Tracked(), Tracked());      // frame at t=0
            rec.Push(0.05f, default, Tracked(), Tracked());   // 0.05 < 0.1 -> ignored
            Timeline tl = rec.Finish(0.05f);

            Assert.AreEqual(1, tl.frames.Count);
        }

        [Test]
        public void Push_AcrossIntervals_EmitsAtFixedCadence()
        {
            var rec = TenHz();
            rec.Begin(0f);
            rec.Push(0f, default, Tracked(), Tracked());      // t=0
            rec.Push(0.05f, default, Tracked(), Tracked());   // ignored
            rec.Push(0.1f, default, Tracked(), Tracked());    // t=0.1
            rec.Push(0.2f, default, Tracked(), Tracked());    // t=0.2
            Timeline tl = rec.Finish(0.2f);

            Assert.AreEqual(3, tl.frames.Count);
            Assert.That(tl.frames[1].t, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(tl.frames[2].t, Is.EqualTo(0.2f).Within(1e-4f));
        }

        [Test]
        public void Finish_SetsDurationRelativeToStart()
        {
            var rec = TenHz();
            rec.Begin(5f);
            rec.Push(5f, default, Tracked(), Tracked());
            Timeline tl = rec.Finish(7.5f);

            Assert.That(tl.durationSeconds, Is.EqualTo(2.5f).Within(1e-4f));
        }

        [Test]
        public void Push_BeforeBegin_IsIgnored()
        {
            var rec = TenHz();
            rec.Push(0f, default, Tracked(), Tracked());      // no Begin yet
            rec.Begin(0f);
            rec.Push(0f, default, Tracked(), Tracked());
            Timeline tl = rec.Finish(0f);

            Assert.AreEqual(1, tl.frames.Count);
        }

        [Test]
        public void StoresHeadAndBothHands()
        {
            var rec = TenHz();
            rec.Begin(0f);
            var head = new Pose(new Vector3(1f, 2f, 3f), Quaternion.identity);
            rec.Push(0f, head, Tracked(), Tracked());
            Timeline tl = rec.Finish(0f);

            Assert.That(tl.frames[0].head.position.z, Is.EqualTo(3f).Within(1e-4f));
            Assert.AreEqual(1, tl.frames[0].leftHand.boneRotations.Length);
            Assert.AreEqual(1, tl.frames[0].rightHand.boneRotations.Length);
        }

        [Test]
        public void Push_WithReusedBoneArray_SnapshotsEachFrameIndependently()
        {
            // The real capture sources reuse ONE bone array per hand and mutate it in place each
            // frame. The recorder must snapshot the bone data per frame, not store the live reference.
            var rec = new TimelineRecorder(10f);
            rec.Begin(0f);

            var bones = new Quaternion[1];
            var hand = new HandPose { boneRotations = bones };

            bones[0] = Quaternion.identity;
            rec.Push(0f, default, hand, hand);            // frame at t=0

            bones[0] = Quaternion.Euler(0f, 0f, 90f);     // mutate the SAME array
            rec.Push(0.1f, default, hand, hand);          // frame at t=0.1

            Timeline tl = rec.Finish(0.1f);

            Assert.AreEqual(2, tl.frames.Count);
            Assert.AreNotSame(tl.frames[0].leftHand.boneRotations, tl.frames[1].leftHand.boneRotations,
                "Each frame must own a snapshot of the bone array, not alias the reused buffer.");
            Assert.That(Quaternion.Angle(tl.frames[0].leftHand.boneRotations[0], Quaternion.identity), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(tl.frames[1].leftHand.boneRotations[0], Quaternion.Euler(0f, 0f, 90f)), Is.LessThan(0.1f));
        }
    }
}

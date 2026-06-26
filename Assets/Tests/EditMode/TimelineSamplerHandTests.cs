using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class TimelineSamplerHandTests
    {
        private static HandPose Hand(Vector3 rootPos, params Quaternion[] bones) =>
            new HandPose { root = new Pose(rootPos, Quaternion.identity), boneRotations = bones };

        // Two frames 1s apart; right hand root x goes 0 -> 10, one bone 0deg -> 90deg about Z.
        private static Timeline TwoHandFrames()
        {
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f, rightHand = Hand(new Vector3(0f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 1f, rightHand = Hand(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f)) });
            return tl;
        }

        [Test]
        public void SampleHand_AtFrameTime_ReturnsThatFrameRootAndBones()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 0f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Quaternion.Angle(into[0], Quaternion.identity), Is.LessThan(0.1f));
        }

        [Test]
        public void SampleHand_Midpoint_InterpolatesRootAndBones()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 0.5f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(5f).Within(1e-4f));          // lerp
            Assert.That(Quaternion.Angle(into[0], Quaternion.Euler(0f, 0f, 45f)), Is.LessThan(0.5f)); // slerp
        }

        [Test]
        public void SampleHand_BeforeFirst_ClampsToFirst()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), -1f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_AfterLast_ClampsToLast()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 99f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_NotTracked_ReturnsFalse()
        {
            // Both frames have an empty right hand (boneRotations null) -> not tracked.
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f });
            tl.frames.Add(new PoseFrame { t = 1f });
            var into = new Quaternion[1];

            bool tracked = TimelineSampler.SampleHand(tl, 0.5f, rightHand: true, into, out _);

            Assert.IsFalse(tracked);
        }

        [Test]
        public void SampleHand_MixedTracking_UsesTheTrackedFrame()
        {
            // Frame A tracked, frame B not: a sample between them should use A and report tracked.
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f, rightHand = Hand(new Vector3(2f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 1f });
            var into = new Quaternion[1];

            bool tracked = TimelineSampler.SampleHand(tl, 0.5f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_WritesIntoCallerArray_NoNewAllocation()
        {
            var into = new Quaternion[1];
            Quaternion[] reference = into;

            TimelineSampler.SampleHand(TwoHandFrames(), 0f, rightHand: true, into, out _);

            Assert.AreSame(reference, into, "Sampler must fill the caller-owned array, not replace it.");
        }
    }
}

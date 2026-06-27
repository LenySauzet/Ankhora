using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class TimelineSamplerTests
    {
        // Two frames 1s apart, head moving from x=0 to x=10 along a straight line.
        private static Timeline TwoFrames()
        {
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f, head = new Pose(new Vector3(0f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 1f, head = new Pose(new Vector3(10f, 0f, 0f), Quaternion.identity) });
            return tl;
        }

        [Test]
        public void SampleHead_AtFrameTime_ReturnsThatFramePose()
        {
            Pose pose = TimelineSampler.SampleHead(TwoFrames(), 1f);
            Assert.That(pose.position.x, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void SampleHead_BetweenFrames_InterpolatesPositionLinearly()
        {
            Pose pose = TimelineSampler.SampleHead(TwoFrames(), 0.5f);
            Assert.That(pose.position.x, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void SampleHead_BeforeFirstFrame_ClampsToFirst()
        {
            Pose pose = TimelineSampler.SampleHead(TwoFrames(), -1f);
            Assert.That(pose.position.x, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SampleHead_AfterLastFrame_ClampsToLast()
        {
            Pose pose = TimelineSampler.SampleHead(TwoFrames(), 99f);
            Assert.That(pose.position.x, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void SampleHead_NullTimeline_ReturnsDefaultPose()
        {
            // Defensive: a null timeline must not throw (symmetry with SampleHand's null guard).
            Pose pose = TimelineSampler.SampleHead(null, 0.5f);

            Assert.That(pose.position, Is.EqualTo(Vector3.zero));
            Assert.That(pose.rotation, Is.EqualTo(default(Quaternion)));
        }

        [Test]
        public void SampleHead_EmptyTimeline_ReturnsDefaultPose()
        {
            // No frames recorded yet: the sampler must not throw, it returns the default pose.
            var empty = new Timeline { durationSeconds = 0f };

            Pose pose = TimelineSampler.SampleHead(empty, 0.5f);

            Assert.That(pose.position, Is.EqualTo(Vector3.zero));
            Assert.That(pose.rotation, Is.EqualTo(default(Quaternion)));
        }

        [Test]
        public void SampleHead_SingleFrame_ReturnsThatFramePose()
        {
            var tl = new Timeline { durationSeconds = 0f };
            tl.frames.Add(new PoseFrame { t = 0.5f, head = new Pose(new Vector3(7f, 0f, 0f), Quaternion.identity) });

            Pose pose = TimelineSampler.SampleHead(tl, 0.5f);
            Assert.That(pose.position.x, Is.EqualTo(7f).Within(1e-4f));
        }

        [Test]
        public void SampleHead_ThreeFrames_InterpolatesWithinCorrectBracket()
        {
            // Exercises the binary search: t=1.5 must interpolate the [1s, 2s] pair, not [0s, 1s].
            var tl = new Timeline { durationSeconds = 2f };
            tl.frames.Add(new PoseFrame { t = 0f, head = new Pose(new Vector3(0f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 1f, head = new Pose(new Vector3(10f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 2f, head = new Pose(new Vector3(20f, 0f, 0f), Quaternion.identity) });

            Pose pose = TimelineSampler.SampleHead(tl, 1.5f);
            Assert.That(pose.position.x, Is.EqualTo(15f).Within(1e-4f));
        }
    }
}

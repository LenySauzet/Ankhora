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
    }
}

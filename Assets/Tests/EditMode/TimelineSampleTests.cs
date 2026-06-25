using Ankhora.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class TimelineSampleTests
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
        public void Sample_AtFrameTime_ReturnsThatFramePose()
        {
            Pose pose = TwoFrames().Sample(1f);
            Assert.That(pose.position.x, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void Sample_BetweenFrames_InterpolatesPositionLinearly()
        {
            Pose pose = TwoFrames().Sample(0.5f);
            Assert.That(pose.position.x, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void Sample_BeforeFirstFrame_ClampsToFirst()
        {
            Pose pose = TwoFrames().Sample(-1f);
            Assert.That(pose.position.x, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Sample_AfterLastFrame_ClampsToLast()
        {
            Pose pose = TwoFrames().Sample(99f);
            Assert.That(pose.position.x, Is.EqualTo(10f).Within(1e-4f));
        }
    }
}

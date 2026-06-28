using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// Covers the per-frame bone LOCAL position channel added to fix the replay offset: the ghost must
    /// reproduce the live hand's tracked joint offsets, not the generic rest bind offsets. See
    /// <see cref="HandPose.boneLocalPositions"/>.
    /// </summary>
    public class TimelineSamplerHandPositionTests
    {
        private static HandPose Hand(Vector3 rootPos, Quaternion[] rot, Vector3[] pos) =>
            new HandPose { root = new Pose(rootPos, Quaternion.identity), boneRotations = rot, boneLocalPositions = pos };

        // Two frames 1s apart; right hand bone 0 local position goes (1,0,0) -> (3,0,0).
        private static Timeline TwoHandFrames()
        {
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame
            {
                t = 0f,
                rightHand = Hand(Vector3.zero, new[] { Quaternion.identity }, new[] { new Vector3(1f, 0f, 0f) })
            });
            tl.frames.Add(new PoseFrame
            {
                t = 1f,
                rightHand = Hand(Vector3.zero, new[] { Quaternion.identity }, new[] { new Vector3(3f, 0f, 0f) })
            });
            return tl;
        }

        [Test]
        public void SampleHand_AtFrameTime_ReturnsThatFrameBonePositions()
        {
            var rot = new Quaternion[1];
            var pos = new Vector3[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 1f, rightHand: true, rot, pos, out _);

            Assert.IsTrue(tracked);
            Assert.That(pos[0].x, Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_Midpoint_InterpolatesBonePositions()
        {
            var rot = new Quaternion[1];
            var pos = new Vector3[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 0.5f, rightHand: true, rot, pos, out _);

            Assert.IsTrue(tracked);
            Assert.That(pos[0].x, Is.EqualTo(2f).Within(1e-4f));   // lerp between 1 and 3
        }

        [Test]
        public void SampleHand_WritesIntoCallerPositionArray_DoesNotReplaceIt()
        {
            var rot = new Quaternion[1];
            var pos = new Vector3[1];
            pos[0] = new Vector3(123f, 45f, 67f);   // sentinel the sampler must overwrite
            Vector3[] reference = pos;

            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 1f, rightHand: true, rot, pos, out _);

            Assert.IsTrue(tracked);
            Assert.AreSame(reference, pos, "Sampler must fill the caller-owned positions array, not replace it.");
            Assert.That(pos[0].x, Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_NullPositionBuffer_StillReturnsRotations()
        {
            // Callers that don't want positions pass null; the rotation channel must be unaffected.
            var rot = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 0.5f, rightHand: true, rot, null, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_SourceWithoutPositions_LeavesBufferUntouched()
        {
            // A legacy frame with rotations but no positions: the position buffer keeps its prior values
            // (replay falls back to the rest bind offsets) rather than collapsing to zero.
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame
            {
                t = 0f,
                rightHand = new HandPose { root = Pose.identity, boneRotations = new[] { Quaternion.identity } }
            });
            tl.frames.Add(new PoseFrame
            {
                t = 1f,
                rightHand = new HandPose { root = Pose.identity, boneRotations = new[] { Quaternion.identity } }
            });
            var rot = new Quaternion[1];
            var pos = new Vector3[1];
            pos[0] = new Vector3(9f, 9f, 9f);   // sentinel that must survive

            bool tracked = TimelineSampler.SampleHand(tl, 0.5f, rightHand: true, rot, pos, out _);

            Assert.IsTrue(tracked);
            Assert.That(pos[0], Is.EqualTo(new Vector3(9f, 9f, 9f)));
        }
    }
}

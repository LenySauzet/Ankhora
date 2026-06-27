using Ankhora.Domain.Model;
using Ankhora.Domain.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// Replay rebuilds the articulated ghost rig from the once-captured <see cref="HandSkeleton"/>
    /// stored on the timeline. These tests pin that the descriptor (parent links + rest bind poses)
    /// survives the JSON round-trip — JsonUtility silently drops unsupported nesting, so this guards
    /// the wire format the device-side first light depends on.
    /// </summary>
    public class HandSkeletonSerializationTests
    {
        [Test]
        public void RoundTrip_TimelineSkeleton_PreservesParentsAndBindPoses()
        {
            var mc = new Masterclass { id = "mc", title = "T" };
            var ch = new Chapter { id = "c" };
            ch.timeline.durationSeconds = 1f;
            ch.timeline.skeleton = new HandSkeleton
            {
                boneParents = new[] { -1, 0, 0, 2 },
                boneBindPoses = new[]
                {
                    new Pose(Vector3.zero, Quaternion.identity),
                    new Pose(new Vector3(0.02f, 0f, 0.06f), Quaternion.Euler(0f, 5f, 0f)),
                    new Pose(new Vector3(0f, 0f, 0.03f), Quaternion.Euler(10f, 0f, 0f)),
                    new Pose(new Vector3(0.025f, 0f, 0f), Quaternion.Euler(0f, 0f, 45f)),
                },
            };
            mc.chapters.Add(ch);

            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            Masterclass restored = serializer.Deserialize(serializer.Serialize(mc));

            HandSkeleton s = restored.chapters[0].timeline.skeleton;
            Assert.IsNotNull(s, "Skeleton descriptor must survive the round-trip.");
            Assert.IsTrue(s.IsValid);
            Assert.AreEqual(new[] { -1, 0, 0, 2 }, s.boneParents);
            Assert.That(s.boneBindPoses[1].position.z, Is.EqualTo(0.06f).Within(1e-4f));
            Assert.That(Quaternion.Angle(s.boneBindPoses[3].rotation, Quaternion.Euler(0f, 0f, 45f)),
                Is.LessThan(0.1f));
        }

        [Test]
        public void RoundTrip_TimelineWithoutSkeleton_DeserializesNullDescriptor()
        {
            // Legacy/hands-less recordings have no skeleton; replay must tolerate that, not crash.
            var mc = new Masterclass { id = "mc", title = "T" };
            mc.chapters.Add(new Chapter { id = "c" });

            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            Masterclass restored = serializer.Deserialize(serializer.Serialize(mc));

            HandSkeleton s = restored.chapters[0].timeline.skeleton;
            Assert.IsTrue(s == null || !s.IsValid, "Absent skeleton must read back as null/invalid.");
        }
    }
}

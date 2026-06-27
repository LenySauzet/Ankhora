using Ankhora.Domain.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The recorder must store hand/head poses relative to a stable reference frame (the camera
    /// rig's tracking space), not in the bone-local frame — otherwise the gross motion of the hand
    /// through the room is lost (see the recorder-hands-capture spec, reference-frame fix). These
    /// tests pin the pure transform math that does the world↔reference conversion.
    /// </summary>
    public class PoseSpaceTests
    {
        private const float Eps = 1e-4f;

        private static void AssertPoseEqual(Pose expected, Pose actual)
        {
            Assert.That(Vector3.Distance(expected.position, actual.position), Is.LessThan(Eps),
                $"position {actual.position} != {expected.position}");
            Assert.That(Quaternion.Angle(expected.rotation, actual.rotation), Is.LessThan(0.05f),
                $"rotation {actual.rotation.eulerAngles} != {expected.rotation.eulerAngles}");
        }

        [Test]
        public void RelativeTo_IdentityReference_ReturnsWorldUnchanged()
        {
            var reference = new Pose(Vector3.zero, Quaternion.identity);
            var world = new Pose(new Vector3(1f, 2f, 3f), Quaternion.Euler(10f, 20f, 30f));

            AssertPoseEqual(world, PoseSpace.RelativeTo(reference, world));
        }

        [Test]
        public void RelativeTo_TranslatedReference_SubtractsOrigin()
        {
            var reference = new Pose(new Vector3(5f, 0f, -2f), Quaternion.identity);
            var world = new Pose(new Vector3(6f, 1f, -2f), Quaternion.identity);

            Pose local = PoseSpace.RelativeTo(reference, world);

            AssertPoseEqual(new Pose(new Vector3(1f, 1f, 0f), Quaternion.identity), local);
        }

        [Test]
        public void RelativeTo_RotatedReference_RotatesIntoLocalFrame()
        {
            // Reference yawed 90° about Y. A world point 1 m in front of the origin along +Z maps,
            // in the reference's local frame, to local −X (Inverse(90° yaw) sends world +Z to local −X).
            var reference = new Pose(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            var world = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);

            Pose local = PoseSpace.RelativeTo(reference, world);

            // Inverse(90° yaw) maps world +Z to local -X.
            AssertPoseEqual(new Pose(new Vector3(-1f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f)), local);
        }

        [Test]
        public void ToWorld_IsInverseOfRelativeTo()
        {
            var reference = new Pose(new Vector3(-3f, 1.5f, 4f), Quaternion.Euler(15f, -40f, 75f));
            var world = new Pose(new Vector3(2f, 0.2f, 1f), Quaternion.Euler(80f, 10f, -25f));

            Pose roundTrip = PoseSpace.ToWorld(reference, PoseSpace.RelativeTo(reference, world));

            AssertPoseEqual(world, roundTrip);
        }
    }
}

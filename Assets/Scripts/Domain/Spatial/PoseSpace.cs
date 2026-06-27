using UnityEngine;

namespace Ankhora.Domain.Spatial
{
    /// <summary>
    /// Pure conversions between world space and a reference frame's local space, for poses
    /// (position + rotation). The recorder uses this to store hand/head poses relative to the
    /// camera rig's tracking space so a recording survives recentering and preserves the hand's
    /// gross motion through the room; replay reverses it. Assumes a uniform unit-scale reference
    /// (true of the OVR tracking space).
    /// </summary>
    public static class PoseSpace
    {
        /// <summary>Expresses <paramref name="world"/> in <paramref name="reference"/>'s local frame.</summary>
        public static Pose RelativeTo(in Pose reference, in Pose world)
        {
            Quaternion invRot = Quaternion.Inverse(reference.rotation);
            return new Pose(
                invRot * (world.position - reference.position),
                invRot * world.rotation);
        }

        /// <summary>Inverse of <see cref="RelativeTo"/>: maps a <paramref name="local"/> pose back to world.</summary>
        public static Pose ToWorld(in Pose reference, in Pose local)
        {
            return new Pose(
                reference.position + reference.rotation * local.position,
                reference.rotation * local.rotation);
        }
    }
}

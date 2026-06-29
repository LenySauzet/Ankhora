using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// A renderable ghost hand the player drives from sampled poses. Behind an interface so the
    /// product visual (skinned Meta mesh) and a debug visual (joint spheres) are interchangeable
    /// behind the same call site.
    /// </summary>
    public interface IHandView
    {
        /// <summary>
        /// Provide the captured bone topology + rest poses so a rig that needs them (e.g. the FK
        /// skeleton view) can build itself once. Views that don't need it may ignore the call.
        /// </summary>
        void Bind(HandSkeleton skeleton);

        /// <summary>Show or hide this hand (hidden when the recorded hand is untracked).</summary>
        void Show(bool visible);

        /// <summary>
        /// Apply a sampled pose: <paramref name="root"/> wrist pose + the first
        /// <paramref name="boneCount"/> local bone rotations from <paramref name="boneRotations"/> and,
        /// when non-null, local positions from <paramref name="boneLocalPositions"/>. A null positions
        /// buffer means "keep the rest bind offsets" (legacy recordings without per-frame positions).
        /// </summary>
        void Apply(in Pose root, Quaternion[] boneRotations, Vector3[] boneLocalPositions, int boneCount);
    }
}

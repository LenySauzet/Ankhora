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
        /// <summary>Show or hide this hand (hidden when the recorded hand is untracked).</summary>
        void Show(bool visible);

        /// <summary>
        /// Apply a sampled pose: <paramref name="root"/> wrist pose + the first
        /// <paramref name="boneCount"/> local bone rotations from <paramref name="boneRotations"/>.
        /// </summary>
        void Apply(in Pose root, Quaternion[] boneRotations, int boneCount);
    }
}

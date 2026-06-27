using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Per-frame source of head + hand poses for the recorder. Behind an interface so the recording
    /// loop can be driven by the real OVR skeleton on device or a simulated source headless — the
    /// engine/OVR dependency stays on the concrete implementations.
    /// </summary>
    public interface IHandPoseSource
    {
        /// <summary>Current head pose in tracking space; false if unavailable.</summary>
        bool TryGetHead(out Pose head);

        /// <summary>
        /// Fills <paramref name="pose"/> (root + bone rotations) for one hand and returns whether it
        /// is tracked. Implementations reuse <paramref name="pose"/>'s bone array when its length
        /// already matches to avoid per-frame allocation.
        /// </summary>
        bool TryGetHand(bool rightHand, ref HandPose pose);
    }
}

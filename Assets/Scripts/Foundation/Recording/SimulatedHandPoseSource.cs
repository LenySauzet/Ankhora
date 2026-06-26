using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Deterministic synthetic <see cref="IHandPoseSource"/> for smoke-testing the record -> replay
    /// loop without a headset (Editor Play Mode on macOS cannot produce hand tracking). Drives both
    /// hands through a slow looping wave so a recorded take has visible motion to replay.
    /// </summary>
    public class SimulatedHandPoseSource : MonoBehaviour, IHandPoseSource
    {
        [SerializeField, Min(1)] private int _boneCount = 19;

        public bool TryGetHead(out Pose head)
        {
            head = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            return true;
        }

        public bool TryGetHand(bool rightHand, ref HandPose pose)
        {
            float phase = Time.time * 2f + (rightHand ? Mathf.PI : 0f);
            float side = rightHand ? 0.2f : -0.2f;
            pose.root = new Pose(
                new Vector3(side, 1.2f + 0.1f * Mathf.Sin(phase), 0.4f),
                Quaternion.Euler(0f, 0f, 20f * Mathf.Sin(phase)));

            if (pose.boneRotations == null || pose.boneRotations.Length != _boneCount)
                pose.boneRotations = new Quaternion[_boneCount];
            for (int i = 0; i < _boneCount; i++)
                pose.boneRotations[i] = Quaternion.Euler(15f * Mathf.Sin(phase + i * 0.3f), 0f, 0f);
            return true;
        }
    }
}

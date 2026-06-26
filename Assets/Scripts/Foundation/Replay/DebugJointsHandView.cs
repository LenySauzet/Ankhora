using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Diagnostic <see cref="IHandView"/>: drives a pre-assigned chain of joint transforms (small
    /// spheres) by setting each one's local rotation from the sampled bone rotations and the wrist
    /// from the root. Proves the data pipeline cheaply when the skinned mesh looks wrong on device.
    /// </summary>
    public class DebugJointsHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private Transform _wrist;
        [SerializeField] private Transform[] _joints; // ordered to match the captured bone order

        public void Show(bool visible)
        {
            if (_wrist != null)
                _wrist.gameObject.SetActive(visible);
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_wrist != null)
            {
                _wrist.localPosition = root.position;
                _wrist.localRotation = root.rotation;
            }
            if (_joints == null)
                return;
            int n = Mathf.Min(_joints.Length, boneCount);
            for (int i = 0; i < n; i++)
                if (_joints[i] != null)
                    _joints[i].localRotation = boneRotations[i];
        }
    }
}

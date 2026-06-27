using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Diagnostic <see cref="IHandView"/>: drives a pre-assigned chain of joint transforms (small
    /// spheres) by setting each one's local rotation from the sampled bone rotations and the wrist
    /// from the root. Superseded by <see cref="FkGhostHandView"/> for showing finger motion (rotating
    /// a lone sphere is invisible); kept as a minimal seam example.
    /// </summary>
    public class DebugJointsHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private Transform _wrist;
        [SerializeField] private Transform[] _joints; // ordered to match the captured bone order

        /// <summary>Pre-wired in the scene; this view does not build itself from the descriptor.</summary>
        public void Bind(HandSkeleton skeleton) { }

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

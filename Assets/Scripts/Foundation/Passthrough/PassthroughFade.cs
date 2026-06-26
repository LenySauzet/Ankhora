using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Pure fade state for the VR↔MR transition: a normalised "MR-ness" in [0,1] that moves toward
    /// a target at a fixed rate (1 / transitionSeconds) and is eased for display. Holds no engine
    /// state, so the timing and easing are EditMode-testable without a headset; the adapter applies
    /// <see cref="Opacity"/> to the camera / passthrough layer each frame.
    /// </summary>
    public struct PassthroughFade
    {
        /// <summary>Raw progress toward MR: 0 = full VR background, 1 = full passthrough.</summary>
        public float Current;

        /// <summary>Eased value applied to the visuals (SmoothStep for a soft start and stop).</summary>
        public float Opacity => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Current));

        /// <summary>True once <see cref="Current"/> has reached <paramref name="target"/>.</summary>
        public bool HasReached(float target) => Mathf.Approximately(Current, Mathf.Clamp01(target));

        /// <summary>
        /// Advance <see cref="Current"/> toward <paramref name="target"/> (0 or 1) by one frame.
        /// A <paramref name="transitionSeconds"/> of 0 (or less) snaps to the target instantly.
        /// </summary>
        public void Step(float target, float deltaTime, float transitionSeconds)
        {
            float rate = transitionSeconds <= 0f ? float.MaxValue : 1f / transitionSeconds;
            Current = Mathf.MoveTowards(Current, Mathf.Clamp01(target), rate * deltaTime);
        }
    }
}

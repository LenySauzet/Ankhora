using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Domain.Sampling
{
    /// <summary>
    /// Reads a <see cref="Timeline"/> at an arbitrary time, interpolating between the recorded
    /// frames so ghost motion stays smooth at any display rate. Pure, deterministic, and
    /// allocation-free (value-type frames + structs) so it is safe in the replay hot loop.
    /// Kept off the <see cref="Timeline"/> DTO so the model layer stays strictly data.
    /// </summary>
    /// <remarks>
    /// Precondition: <see cref="Timeline.frames"/> are sorted by ascending <see cref="PoseFrame.t"/>.
    /// The recorder guarantees this (it writes from a single monotonic clock); the sampler does not
    /// re-sort on the hot path. When a per-bone hand sampler is added for replay, it must write into
    /// a caller-owned, pre-allocated <c>Quaternion[]</c> rather than returning a fresh array, to keep
    /// the 90 Hz playback loop allocation-free (see the replay-feature notes).
    /// </remarks>
    public static class TimelineSampler
    {
        /// <summary>
        /// Returns the head pose at time <paramref name="t"/> (seconds): the exact frame pose at a
        /// frame time, a linear/spherical interpolation between the two bracketing frames in
        /// between, and the first/last frame pose when <paramref name="t"/> is outside the range.
        /// Runs in O(log n) via a binary search over the sorted frames.
        /// </summary>
        public static Pose SampleHead(Timeline timeline, float t)
        {
            var frames = timeline.frames;
            if (frames == null || frames.Count == 0)
                return default;

            if (t <= frames[0].t)
                return frames[0].head;

            int lastIndex = frames.Count - 1;
            PoseFrame last = frames[lastIndex];
            if (t >= last.t)
                return last.head;

            // Binary search for the [lo, hi] frame pair bracketing t (frames are sorted ascending).
            int lo = 0;
            int hi = lastIndex;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (frames[mid].t <= t)
                    lo = mid;
                else
                    hi = mid;
            }

            PoseFrame a = frames[lo];
            PoseFrame b = frames[hi];
            float span = b.t - a.t;
            float u = span > 0f ? (t - a.t) / span : 0f;
            return new Pose(
                Vector3.LerpUnclamped(a.head.position, b.head.position, u),
                Quaternion.SlerpUnclamped(a.head.rotation, b.head.rotation, u));
        }

        /// <summary>
        /// Samples one hand at time <paramref name="t"/> into the caller-owned <paramref name="into"/>
        /// array (no allocation), returning whether the hand is tracked there. Clamps to the first/last
        /// frame outside the range; interpolates root (lerp/slerp) and each bone rotation (slerp) between
        /// the two bracketing frames. When only one bracketing frame has the hand tracked, uses that one;
        /// when neither does, returns false and leaves <paramref name="into"/> untouched.
        /// </summary>
        public static bool SampleHand(Timeline timeline, float t, bool rightHand, Quaternion[] into, out Pose root)
        {
            root = default;
            var frames = timeline?.frames;
            if (frames == null || frames.Count == 0)
                return false;

            int lastIndex = frames.Count - 1;
            if (t <= frames[0].t)
                return EmitHand(HandOf(frames[0], rightHand), into, out root);
            if (t >= frames[lastIndex].t)
                return EmitHand(HandOf(frames[lastIndex], rightHand), into, out root);

            int lo = 0;
            int hi = lastIndex;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (frames[mid].t <= t)
                    lo = mid;
                else
                    hi = mid;
            }

            PoseFrame fa = frames[lo];
            PoseFrame fb = frames[hi];
            HandPose a = HandOf(fa, rightHand);
            HandPose b = HandOf(fb, rightHand);
            bool ta = IsTracked(a);
            bool tb = IsTracked(b);

            if (!ta && !tb)
                return false;
            if (ta && !tb)
                return EmitHand(a, into, out root);
            if (!ta)
                return EmitHand(b, into, out root);

            float span = fb.t - fa.t;
            float u = span > 0f ? (t - fa.t) / span : 0f;
            root = new Pose(
                Vector3.LerpUnclamped(a.root.position, b.root.position, u),
                Quaternion.SlerpUnclamped(a.root.rotation, b.root.rotation, u));

            int n = Mathf.Min(into.Length, Mathf.Min(a.boneRotations.Length, b.boneRotations.Length));
            for (int i = 0; i < n; i++)
                into[i] = Quaternion.SlerpUnclamped(a.boneRotations[i], b.boneRotations[i], u);
            return true;
        }

        private static HandPose HandOf(in PoseFrame f, bool rightHand) => rightHand ? f.rightHand : f.leftHand;

        private static bool IsTracked(in HandPose h) => h.boneRotations != null && h.boneRotations.Length > 0;

        private static bool EmitHand(HandPose h, Quaternion[] into, out Pose root)
        {
            root = default;
            if (!IsTracked(h))
                return false;

            root = h.root;
            int n = Mathf.Min(into.Length, h.boneRotations.Length);
            for (int i = 0; i < n; i++)
                into[i] = h.boneRotations[i];
            return true;
        }
    }
}

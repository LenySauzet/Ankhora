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
            var frames = timeline?.frames;
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
        /// Rotation-only overload (no per-bone positions). Equivalent to passing a null position buffer.
        /// </summary>
        public static bool SampleHand(Timeline timeline, float t, bool rightHand, Quaternion[] into, out Pose root) =>
            SampleHand(timeline, t, rightHand, into, null, out root);

        /// <summary>
        /// Samples one hand at time <paramref name="t"/> into the caller-owned <paramref name="intoRot"/>
        /// (and, when non-null, <paramref name="intoPos"/>) arrays — no allocation — returning whether the
        /// hand is tracked there. Clamps to the first/last frame outside the range; interpolates root
        /// (lerp/slerp), each bone rotation (slerp) and each bone local position (lerp) between the two
        /// bracketing frames. When only one bracketing frame has the hand tracked, uses that one; when
        /// neither does, returns false and leaves the buffers untouched.
        /// <para>
        /// <paramref name="intoPos"/> is filled only when the source frames carry
        /// <see cref="HandPose.boneLocalPositions"/>; on legacy position-less frames it is left untouched so
        /// the view keeps its rest bind offsets (replay still works, just without the per-frame correction).
        /// </para>
        /// </summary>
        public static bool SampleHand(Timeline timeline, float t, bool rightHand,
            Quaternion[] intoRot, Vector3[] intoPos, out Pose root)
        {
            root = default;
            var frames = timeline?.frames;
            if (frames == null || frames.Count == 0)
                return false;

            int lastIndex = frames.Count - 1;
            if (t <= frames[0].t)
                return EmitHand(HandOf(frames[0], rightHand), intoRot, intoPos, out root);
            if (t >= frames[lastIndex].t)
                return EmitHand(HandOf(frames[lastIndex], rightHand), intoRot, intoPos, out root);

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
                return EmitHand(a, intoRot, intoPos, out root);
            if (!ta)
                return EmitHand(b, intoRot, intoPos, out root);

            float span = fb.t - fa.t;
            float u = span > 0f ? (t - fa.t) / span : 0f;
            root = new Pose(
                Vector3.LerpUnclamped(a.root.position, b.root.position, u),
                Quaternion.SlerpUnclamped(a.root.rotation, b.root.rotation, u));

            int n = Mathf.Min(intoRot.Length, Mathf.Min(a.boneRotations.Length, b.boneRotations.Length));
            for (int i = 0; i < n; i++)
                intoRot[i] = Quaternion.SlerpUnclamped(a.boneRotations[i], b.boneRotations[i], u);

            if (intoPos != null && HasPositions(a) && HasPositions(b))
            {
                int m = Mathf.Min(intoPos.Length, Mathf.Min(a.boneLocalPositions.Length, b.boneLocalPositions.Length));
                for (int i = 0; i < m; i++)
                    intoPos[i] = Vector3.LerpUnclamped(a.boneLocalPositions[i], b.boneLocalPositions[i], u);
            }
            return true;
        }

        /// <summary>
        /// True when ANY frame carries per-bone local positions on either hand. Used by the replay player
        /// to decide whether to allocate the position buffers. Scans every frame, not just the first: a take
        /// whose opening frame has both hands untracked (positions null) still gains the position channel
        /// as soon as a hand comes into view, and replaying it without that channel re-opens the fingertip
        /// offset the channel was added to fix.
        /// </summary>
        public static bool HasBoneLocalPositions(Timeline timeline)
            => HasBoneLocalPositions(timeline, rightHand: false) || HasBoneLocalPositions(timeline, rightHand: true);

        /// <summary>
        /// Per-hand variant: true when ANY frame carries per-bone local positions for the given hand. The
        /// player allocates each hand's position buffer independently from this, so a take where only one
        /// hand ever carries positions leaves the other hand's buffer null (replay keeps its rest bind
        /// offsets) rather than driving it with stale zeros.
        /// </summary>
        public static bool HasBoneLocalPositions(Timeline timeline, bool rightHand)
        {
            var frames = timeline?.frames;
            if (frames == null)
                return false;
            for (int i = 0; i < frames.Count; i++)
            {
                if (HasPositions(HandOf(frames[i], rightHand)))
                    return true;
            }
            return false;
        }

        private static HandPose HandOf(in PoseFrame f, bool rightHand) => rightHand ? f.rightHand : f.leftHand;

        private static bool IsTracked(in HandPose h) => h.boneRotations != null && h.boneRotations.Length > 0;

        private static bool HasPositions(in HandPose h) => h.boneLocalPositions != null && h.boneLocalPositions.Length > 0;

        private static bool EmitHand(HandPose h, Quaternion[] intoRot, Vector3[] intoPos, out Pose root)
        {
            root = default;
            if (!IsTracked(h))
                return false;

            root = h.root;
            int n = Mathf.Min(intoRot.Length, h.boneRotations.Length);
            for (int i = 0; i < n; i++)
                intoRot[i] = h.boneRotations[i];

            if (intoPos != null && HasPositions(h))
            {
                int m = Mathf.Min(intoPos.Length, h.boneLocalPositions.Length);
                for (int i = 0; i < m; i++)
                    intoPos[i] = h.boneLocalPositions[i];
            }
            return true;
        }
    }
}

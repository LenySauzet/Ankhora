using Ankhora.Domain.Model;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Provides the once-per-recording hand bone topology + rest pose. Separate from the per-frame
    /// <see cref="IHandPoseSource"/> because it is read a single time at the start of a take (the
    /// skeleton structure is constant) and not every source can supply it.
    /// </summary>
    public interface IHandSkeletonSource
    {
        /// <summary>
        /// Fills <paramref name="skeleton"/> (parent links + rest local bind poses) for one hand and
        /// returns whether a valid skeleton is currently available.
        /// </summary>
        bool TryGetSkeleton(bool rightHand, out HandSkeleton skeleton);
    }
}

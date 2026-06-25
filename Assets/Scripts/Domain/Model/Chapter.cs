using System;

namespace Ankhora.Domain.Model
{
    /// <summary>One ordered chapter of a masterclass: a single recorded take plus its pins.</summary>
    [Serializable]
    public class Chapter
    {
        public string id;
        public string title;
        public int order;

        /// <summary>Reference to a bundled model shown in front of the learner; optional.</summary>
        public string modelRef;

        /// <summary>
        /// Learner-side progress. The on-device manifest is the learner's mutable working copy;
        /// the authored recording is the read-only source (see the spec).
        /// </summary>
        public bool completed;

        public Timeline timeline = new Timeline();
    }
}

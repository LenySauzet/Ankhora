using System;
using System.Collections.Generic;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// Root persisted unit: a time-ordered training recording the learner replays.
    /// Plain serialisable C# (no MonoBehaviour) — the spine of the product. See
    /// docs/02-architecture/domain-record-replay-model.md.
    /// </summary>
    [Serializable]
    public class Masterclass
    {
        /// <summary>The schema version this build writes and can read. Bump when the shape changes.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Format version of this persisted unit; drives migration. See the serializer.</summary>
        public int schemaVersion;

        public string id;
        public string title;

        /// <summary>Ordered chapters (use <see cref="Chapter.order"/> for the intended sequence).</summary>
        public List<Chapter> chapters = new List<Chapter>();
    }
}

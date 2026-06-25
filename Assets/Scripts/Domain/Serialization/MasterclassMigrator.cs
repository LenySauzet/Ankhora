using System;
using Ankhora.Domain.Model;

namespace Ankhora.Domain.Serialization
{
    /// <summary>
    /// Brings a deserialized <see cref="Masterclass"/> up to
    /// <see cref="Masterclass.CurrentSchemaVersion"/>. Separated from the serializer (parsing JSON
    /// and upgrading a schema are different responsibilities) and from the wire format, so a
    /// binary serializer added later reuses the exact same migration path.
    /// </summary>
    public static class MasterclassMigrator
    {
        /// <summary>
        /// Returns <paramref name="masterclass"/> upgraded to the current schema. v1 is the only
        /// known schema today (pass-through); older versions get an explicit upgrade branch here
        /// as the format evolves, and an unknown/future version is rejected with a clear error.
        /// </summary>
        public static Masterclass Migrate(Masterclass masterclass)
        {
            if (masterclass.schemaVersion == Masterclass.CurrentSchemaVersion)
                return masterclass;

            // Future: `case 1: return UpgradeV1ToV2(masterclass);` etc.
            throw new ArgumentException(
                $"Unsupported schemaVersion {masterclass.schemaVersion} " +
                $"(this build reads schemaVersion {Masterclass.CurrentSchemaVersion}).",
                nameof(masterclass));
        }
    }
}

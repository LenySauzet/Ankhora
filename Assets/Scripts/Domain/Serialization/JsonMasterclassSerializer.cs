using System;
using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Domain.Serialization
{
    /// <summary>
    /// JSON implementation of <see cref="IMasterclassSerializer"/>, backed by Unity's
    /// <see cref="JsonUtility"/> (works on the [Serializable] DTOs, no extra dependency).
    /// Parses the payload defensively, then hands off versioning to <see cref="MasterclassMigrator"/>.
    /// </summary>
    public class JsonMasterclassSerializer : IMasterclassSerializer
    {
        public string Serialize(Masterclass masterclass)
        {
            if (masterclass == null)
                throw new ArgumentNullException(
                    nameof(masterclass),
                    $"[{nameof(JsonMasterclassSerializer)}] Cannot serialize a null masterclass.");

            return JsonUtility.ToJson(masterclass);
        }

        public Masterclass Deserialize(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentException(
                    $"[{nameof(JsonMasterclassSerializer)}] Cannot deserialize a null/empty payload.",
                    nameof(payload));

            Masterclass result;
            try
            {
                result = JsonUtility.FromJson<Masterclass>(payload);
            }
            catch (Exception e)
            {
                throw new ArgumentException(
                    $"[{nameof(JsonMasterclassSerializer)}] Malformed JSON payload: {e.Message}",
                    nameof(payload), e);
            }

            // JsonUtility.FromJson returns null for the literal "null" — reject it loudly.
            if (result == null)
                throw new ArgumentException(
                    $"[{nameof(JsonMasterclassSerializer)}] Payload deserialized to null (e.g. literal \"null\").",
                    nameof(payload));

            return MasterclassMigrator.Migrate(result);
        }
    }
}

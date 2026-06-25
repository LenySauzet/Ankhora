using System;
using UnityEngine;

namespace Ankhora.Domain
{
    /// <summary>
    /// JSON implementation of <see cref="IMasterclassSerializer"/>, backed by Unity's
    /// <see cref="JsonUtility"/> (works on the [Serializable] DTOs, no extra dependency).
    /// </summary>
    public class JsonMasterclassSerializer : IMasterclassSerializer
    {
        public string Serialize(Masterclass masterclass) => JsonUtility.ToJson(masterclass);

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

            return result;
        }
    }
}

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

        public Masterclass Deserialize(string payload) => JsonUtility.FromJson<Masterclass>(payload);
    }
}

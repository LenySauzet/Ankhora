using Ankhora.Domain.Model;

namespace Ankhora.Domain.Serialization
{
    /// <summary>
    /// Serialises a <see cref="Masterclass"/> to/from a string payload. Behind an interface so
    /// the on-disk format (JSON now, a compact/binary form later) can change without touching
    /// capture or replay code.
    /// </summary>
    public interface IMasterclassSerializer
    {
        string Serialize(Masterclass masterclass);

        Masterclass Deserialize(string payload);
    }
}

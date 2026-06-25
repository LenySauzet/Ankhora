using Ankhora.Domain;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class MasterclassSerializationTests
    {
        [Test]
        public void RoundTrip_PreservesIdentityFields()
        {
            var original = new Masterclass
            {
                schemaVersion = 1,
                id = "mc-001",
                title = "Knife sharpening",
            };
            IMasterclassSerializer serializer = new JsonMasterclassSerializer();

            string payload = serializer.Serialize(original);
            Masterclass restored = serializer.Deserialize(payload);

            Assert.AreEqual(original.schemaVersion, restored.schemaVersion);
            Assert.AreEqual(original.id, restored.id);
            Assert.AreEqual(original.title, restored.title);
        }
    }
}

using System;
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

        [Test]
        public void Deserialize_NullJsonLiteral_ThrowsWithContext()
        {
            IMasterclassSerializer serializer = new JsonMasterclassSerializer();

            // JsonUtility.FromJson("null") returns null silently — we must reject it loudly.
            var ex = Assert.Throws<ArgumentException>(() => serializer.Deserialize("null"));
            Assert.That(ex.Message, Does.Contain(nameof(JsonMasterclassSerializer)));
        }

        [Test]
        public void Deserialize_EmptyPayload_ThrowsWithContext()
        {
            IMasterclassSerializer serializer = new JsonMasterclassSerializer();

            var ex = Assert.Throws<ArgumentException>(() => serializer.Deserialize(""));
            Assert.That(ex.Message, Does.Contain(nameof(JsonMasterclassSerializer)));
        }
    }
}

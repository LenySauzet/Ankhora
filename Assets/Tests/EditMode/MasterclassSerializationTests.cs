using System;
using Ankhora.Domain;
using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void Deserialize_UnknownSchemaVersion_Throws()
        {
            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            string fromTheFuture = "{\"schemaVersion\":999,\"id\":\"x\",\"title\":\"y\"}";

            var ex = Assert.Throws<ArgumentException>(() => serializer.Deserialize(fromTheFuture));
            Assert.That(ex.Message, Does.Contain("schemaVersion"));
        }

        [Test]
        public void Deserialize_CurrentSchemaVersion_Succeeds()
        {
            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            string current = $"{{\"schemaVersion\":{Masterclass.CurrentSchemaVersion},\"id\":\"x\",\"title\":\"y\"}}";

            Masterclass restored = serializer.Deserialize(current);
            Assert.AreEqual(Masterclass.CurrentSchemaVersion, restored.schemaVersion);
        }

        [Test]
        public void RoundTrip_PreservesNestedChapterTimelineAndPin()
        {
            var original = new Masterclass
            {
                schemaVersion = Masterclass.CurrentSchemaVersion,
                id = "mc",
                title = "T",
            };
            var chapter = new Chapter
            {
                id = "ch1",
                title = "Chapter 1",
                order = 2,
                modelRef = "knife",
                completed = true,
            };
            chapter.timeline.durationSeconds = 2f;
            chapter.timeline.frames.Add(new PoseFrame
            {
                t = 0.5f,
                head = new Pose(new Vector3(1f, 2f, 3f), Quaternion.identity),
            });
            chapter.timeline.pins.Add(new Pin
            {
                id = "p1",
                type = PinType.Image,
                payload = "img/step1.png",
                pose = new Pose(new Vector3(4f, 5f, 6f), Quaternion.identity),
                timeRange = new TimeRange { start = 0.1f, end = 1.9f },
            });
            original.chapters.Add(chapter);

            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            Masterclass restored = serializer.Deserialize(serializer.Serialize(original));

            Assert.AreEqual(1, restored.chapters.Count);
            Chapter rc = restored.chapters[0];
            Assert.AreEqual("ch1", rc.id);
            Assert.AreEqual(2, rc.order);
            Assert.IsTrue(rc.completed);
            Assert.AreEqual("knife", rc.modelRef);

            Assert.AreEqual(1, rc.timeline.frames.Count);
            Assert.That(rc.timeline.frames[0].head.position.z, Is.EqualTo(3f).Within(1e-4f));

            Assert.AreEqual(1, rc.timeline.pins.Count);
            Pin rp = rc.timeline.pins[0];
            Assert.AreEqual(PinType.Image, rp.type);
            Assert.AreEqual("img/step1.png", rp.payload);
            Assert.That(rp.pose.position.x, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(rp.timeRange.end, Is.EqualTo(1.9f).Within(1e-4f));
        }
    }
}

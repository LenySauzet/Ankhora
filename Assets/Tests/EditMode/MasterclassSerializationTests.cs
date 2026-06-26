using System;
using Ankhora.Domain.Model;
using Ankhora.Domain.Serialization;
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
        public void Serialize_NullMasterclass_ThrowsWithContext()
        {
            IMasterclassSerializer serializer = new JsonMasterclassSerializer();

            // Fail fast on a null input rather than emitting the literal "null" payload.
            var ex = Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null));
            Assert.That(ex.Message, Does.Contain(nameof(JsonMasterclassSerializer)));
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
        public void RoundTrip_MasterclassCreatedInCode_DefaultsToCurrentSchemaVersion()
        {
            // The recorder builds a Masterclass in code without setting schemaVersion explicitly.
            // It must already carry the current version (not an uninitialised 0 the migrator rejects).
            var original = new Masterclass { id = "mc", title = "T" };
            Assert.AreEqual(Masterclass.CurrentSchemaVersion, original.schemaVersion,
                "A freshly constructed Masterclass should already carry the current schema version.");

            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            Masterclass restored = serializer.Deserialize(serializer.Serialize(original));

            Assert.AreEqual(Masterclass.CurrentSchemaVersion, restored.schemaVersion);
        }

        [Test]
        public void Serialize_HandPoseBoneRotations_AreWrittenToJson()
        {
            // Guard against JsonUtility silently dropping the nested Quaternion[] (array wrapped in
            // HandPose struct inside PoseFrame struct). Per Unity's serialization rules a struct
            // wrapper is the supported case; this asserts the field actually reaches the wire.
            var mc = new Masterclass { id = "mc", title = "T" };
            var ch = new Chapter { id = "c" };
            ch.timeline.frames.Add(new PoseFrame
            {
                t = 0f,
                leftHand = new HandPose { boneRotations = new[] { Quaternion.identity } },
            });
            mc.chapters.Add(ch);

            string json = new JsonMasterclassSerializer().Serialize(mc);

            StringAssert.Contains("boneRotations", json);
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

        [Test]
        public void RoundTrip_PreservesHandPoseRootAndBoneRotations()
        {
            var mc = new Masterclass { schemaVersion = Masterclass.CurrentSchemaVersion, id = "mc", title = "T" };
            var ch = new Chapter { id = "c" };
            ch.timeline.durationSeconds = 1f;
            ch.timeline.frames.Add(new PoseFrame
            {
                t = 0f,
                head = new Pose(Vector3.zero, Quaternion.identity),
                leftHand = new HandPose
                {
                    root = new Pose(new Vector3(0.1f, 0.2f, 0.3f), Quaternion.Euler(0f, 90f, 0f)),
                    boneRotations = new[]
                    {
                        Quaternion.identity,
                        Quaternion.Euler(10f, 0f, 0f),
                        Quaternion.Euler(0f, 0f, 45f),
                    },
                },
            });
            mc.chapters.Add(ch);

            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            Masterclass restored = serializer.Deserialize(serializer.Serialize(mc));

            HandPose lh = restored.chapters[0].timeline.frames[0].leftHand;
            Assert.AreEqual(3, lh.boneRotations.Length);
            Assert.That(lh.root.position.y, Is.EqualTo(0.2f).Within(1e-4f));
            Assert.That(Quaternion.Angle(lh.boneRotations[2], Quaternion.Euler(0f, 0f, 45f)), Is.LessThan(0.1f));
        }

        [Test]
        public void RoundTrip_FullCapturedTimeline_PreservesAllFramesAndBones()
        {
            // Build a realistic capture: 30 frames, both hands, 19 bones each, via the recorder.
            const int boneCount = 19;
            var recorder = new Ankhora.Domain.Recording.TimelineRecorder(30f);
            recorder.Begin(0f);
            for (int frame = 0; frame < 30; frame++)
            {
                float now = frame / 30f;
                var left = new HandPose { root = new Pose(Vector3.one * frame, Quaternion.identity), boneRotations = new Quaternion[boneCount] };
                var right = new HandPose { root = new Pose(Vector3.one * -frame, Quaternion.identity), boneRotations = new Quaternion[boneCount] };
                for (int b = 0; b < boneCount; b++)
                {
                    left.boneRotations[b] = Quaternion.Euler(frame + b, 0f, 0f);
                    right.boneRotations[b] = Quaternion.Euler(0f, frame + b, 0f);
                }
                recorder.Push(now, new Pose(Vector3.up * frame, Quaternion.identity), left, right);
            }
            Timeline tl = recorder.Finish(29f / 30f);

            var mc = new Masterclass { id = "mc", title = "Captured" };
            var ch = new Chapter { id = "c", timeline = tl };
            mc.chapters.Add(ch);

            IMasterclassSerializer serializer = new JsonMasterclassSerializer();
            Masterclass restored = serializer.Deserialize(serializer.Serialize(mc));

            Timeline rtl = restored.chapters[0].timeline;
            Assert.AreEqual(tl.frames.Count, rtl.frames.Count, "All sampled frames must survive the round-trip.");
            Assert.AreEqual(boneCount, rtl.frames[10].leftHand.boneRotations.Length);
            Assert.AreEqual(boneCount, rtl.frames[10].rightHand.boneRotations.Length);
            Assert.That(Quaternion.Angle(rtl.frames[10].leftHand.boneRotations[5], Quaternion.Euler(15f, 0f, 0f)), Is.LessThan(0.5f));
        }
    }
}

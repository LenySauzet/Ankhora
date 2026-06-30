using Ankhora.Domain.Model;
using Ankhora.Foundation.Persistence;
using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class MasterclassStoreBlobTests
    {
        private string _dir;

        [SetUp] public void SetUp() => _dir = "test-mc-" + System.Guid.NewGuid().ToString("N");
        [TearDown] public void TearDown()
        {
            string p = Path.Combine(Application.persistentDataPath, _dir);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }

        [Test]
        public void Save_ThenWriteAndReadBlob_RoundTripsInSameDir()
        {
            var store = new MasterclassStore(_dir);
            var mc = new Masterclass { id = "mc-local", title = "t" };
            mc.chapters.Add(new Chapter { id = "ch-1", timeline = new Timeline() });

            Assert.IsTrue(store.Save(mc, out _));
            Assert.IsTrue(File.Exists(Path.Combine(store.BaseDir, "manifest.json")));

            byte[] payload = { 1, 2, 3, 4, 5 };
            Assert.IsTrue(store.WriteBlob("voice-ch-1.wav", payload, out _));
            Assert.IsTrue(store.ReadBlob("voice-ch-1.wav", out byte[] back, out _));
            Assert.AreEqual(payload, back);

            Assert.IsTrue(store.TryLoad(out Masterclass loaded, out _));
            Assert.AreEqual("ch-1", loaded.chapters[0].id);
        }

        [Test]
        public void ReadBlob_Missing_ReturnsFalseWithReason()
        {
            var store = new MasterclassStore(_dir);
            Assert.IsFalse(store.ReadBlob("nope.wav", out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void WriteBlob_TraversalOrRootedPath_IsRejected()
        {
            var store = new MasterclassStore(_dir);
            byte[] payload = { 9 };
            Assert.IsFalse(store.WriteBlob("../escape.wav", payload, out string e1), "must reject .. traversal");
            Assert.IsNotEmpty(e1);
            Assert.IsFalse(store.WriteBlob("/tmp/escape.wav", payload, out string e2), "must reject rooted path");
            Assert.IsNotEmpty(e2);
            Assert.IsFalse(store.ReadBlob("../../etc/hosts", out _, out string e3), "must reject .. traversal on read");
            Assert.IsNotEmpty(e3);
        }
    }
}

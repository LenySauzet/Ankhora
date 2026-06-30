using System;
using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Serialization;
using UnityEngine;

namespace Ankhora.Foundation.Persistence
{
    /// <summary>
    /// The single seam for reading/writing a <see cref="Masterclass"/> + its blobs (voice, later Pin images)
    /// under one per-masterclass directory: <c>persistentDataPath/&lt;storageDir&gt;/manifest.json</c> plus
    /// sibling blobs addressed by the relative paths the manifest stores (e.g. <c>voice-ch-1.wav</c>).
    /// </summary>
    public class MasterclassStore
    {
        private const string ManifestName = "manifest.json";
        private readonly IMasterclassSerializer _serializer;

        public MasterclassStore(string storageDir = "mc-local", IMasterclassSerializer serializer = null)
        {
            string dir = string.IsNullOrEmpty(storageDir) ? "mc-local" : storageDir;
            BaseDir = System.IO.Path.Combine(Application.persistentDataPath, dir);
            _serializer = serializer ?? new JsonMasterclassSerializer();
        }

        /// <summary>Absolute directory holding the manifest + blobs for this masterclass.</summary>
        public string BaseDir { get; }

        /// <summary>Absolute path of the manifest file. Named ManifestPath, not Path, to avoid shadowing
        /// <see cref="System.IO.Path"/> inside this class.</summary>
        public string ManifestPath => System.IO.Path.Combine(BaseDir, ManifestName);

        /// <summary>Serialises and writes <paramref name="masterclass"/>; returns false (with a reason) on I/O failure.</summary>
        public bool Save(Masterclass masterclass, out string error)
        {
            try
            {
                Directory.CreateDirectory(BaseDir);
                File.WriteAllText(ManifestPath, _serializer.Serialize(masterclass));
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }

        /// <summary>Reads + deserialises the stored masterclass; returns false (with a reason) if absent or invalid.</summary>
        public bool TryLoad(out Masterclass masterclass, out string error)
        {
            masterclass = null;
            if (!File.Exists(ManifestPath)) { error = $"No recording at {ManifestPath}"; return false; }
            try
            {
                masterclass = _serializer.Deserialize(File.ReadAllText(ManifestPath));
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }

        /// <summary>Writes raw bytes to a sibling blob path inside <see cref="BaseDir"/>; returns false (with a reason) on failure.</summary>
        public bool WriteBlob(string relPath, byte[] bytes, out string error)
        {
            if (!TryResolveBlobPath(relPath, out string full, out error)) return false;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
                File.WriteAllBytes(full, bytes ?? Array.Empty<byte>());
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }

        /// <summary>Reads raw bytes from a sibling blob path inside <see cref="BaseDir"/>; returns false (with a reason) if absent or unreadable.</summary>
        public bool ReadBlob(string relPath, out byte[] bytes, out string error)
        {
            bytes = null;
            if (!TryResolveBlobPath(relPath, out string full, out error)) return false;
            if (!File.Exists(full)) { error = $"No blob at {full}"; return false; }
            try { bytes = File.ReadAllBytes(full); error = null; return true; }
            catch (Exception e) { error = e.Message; return false; }
        }

        /// <summary>Resolves a blob path and confines it to <see cref="BaseDir"/>: rejects rooted paths and
        /// <c>..</c> traversal so a manifest-supplied relative path can never read or write outside the
        /// masterclass folder. Defence-in-depth — today's only blob ref is the fixed <c>voice-ch-1.wav</c>.</summary>
        private bool TryResolveBlobPath(string relPath, out string full, out string error)
        {
            full = null;
            if (string.IsNullOrEmpty(relPath) || System.IO.Path.IsPathRooted(relPath))
            { error = $"Invalid blob path: {relPath}"; return false; }

            string root = System.IO.Path.GetFullPath(BaseDir);
            string combined = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relPath));
            if (combined != root &&
                !combined.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
            { error = $"Blob path escapes storage dir: {relPath}"; return false; }

            full = combined;
            error = null;
            return true;
        }
    }
}

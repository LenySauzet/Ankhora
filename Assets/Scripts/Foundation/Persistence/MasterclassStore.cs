using System;
using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Serialization;
using UnityEngine;

namespace Ankhora.Foundation.Persistence
{
    /// <summary>
    /// The single seam for reading/writing a <see cref="Masterclass"/> to device storage as JSON.
    /// Centralises the <c>persistentDataPath</c> + filename + serializer that the recorder and the
    /// player would otherwise each duplicate, and turns I/O failures into a handled result rather than
    /// an unguarded exception that silently loses a take. The masterclass-browser slice will grow its
    /// enumerate/delete operations here.
    /// </summary>
    public class MasterclassStore
    {
        private readonly string _fileName;
        private readonly IMasterclassSerializer _serializer;

        public MasterclassStore(string fileName = "masterclass.json", IMasterclassSerializer serializer = null)
        {
            _fileName = string.IsNullOrEmpty(fileName) ? "masterclass.json" : fileName;
            _serializer = serializer ?? new JsonMasterclassSerializer();
        }

        /// <summary>Absolute path of the backing file under <see cref="Application.persistentDataPath"/>.</summary>
        public string Path => System.IO.Path.Combine(Application.persistentDataPath, _fileName);

        /// <summary>Serialises and writes <paramref name="masterclass"/>; returns false (with a reason) on I/O failure.</summary>
        public bool Save(Masterclass masterclass, out string error)
        {
            try
            {
                File.WriteAllText(Path, _serializer.Serialize(masterclass));
                error = null;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>Reads + deserialises the stored masterclass; returns false (with a reason) if absent or invalid.</summary>
        public bool TryLoad(out Masterclass masterclass, out string error)
        {
            masterclass = null;
            string path = Path;
            if (!File.Exists(path))
            {
                error = $"No recording at {path}";
                return false;
            }

            try
            {
                masterclass = _serializer.Deserialize(File.ReadAllText(path));
                error = null;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}

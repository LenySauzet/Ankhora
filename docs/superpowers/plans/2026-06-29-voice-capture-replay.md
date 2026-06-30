# Voice Capture + Replay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Capture the Instructor's narration in the same take as the ghost hands and replay it spatialized from the ghost's head, locked to the single replay clock.

**Architecture:** Pure logic (DTO, clock→playhead math, WAV encode/decode) lives in `Ankhora.Domain` and is EditMode-tested without a headset (ADR-0004). Device code (`Microphone`, `AudioSource` + Meta XR Audio, runtime permission) lives in `Ankhora.Foundation` and is verified in Play Mode / on Quest. Audio rides `GhostHandPlayer._clock` — never a second clock.

**Tech Stack:** Unity 6 (`6000.4.10f1`), URP 17.4, Meta XR SDK All-in-One 201.0.0 (Meta XR Audio spatializer), `UnityEngine.Microphone`, `UnityEngine.AudioSource`, Unity Test Framework (EditMode/NUnit).

## Global Constraints

- **ADR-0004 two-assembly split:** `Ankhora.Domain` = pure C# (UnityEngine math types OK; no `MonoBehaviour`, no `Oculus`/OVR). `Ankhora.Foundation` = device layer. Foundation feature folders must not reference each other directly; cross-feature wiring goes through `Foundation/App` (UI may one-way observe a feature — ADR-0004 amendment).
- **Single sync clock:** `GhostHandPlayer._clock` (advanced by `Time.unscaledDeltaTime`) is the only playback clock. `VoicePlayer` is driven from it; it must NOT run its own clock.
- **Audio format:** WAV / PCM, 16-bit, mono, 16 kHz requested. No external encoder dependency.
- **JsonUtility null quirk:** a null nested `[Serializable]` field round-trips to a non-null default object, not null. "No voice" must be tested with `VoiceTrack.HasClip` (clipRef non-empty), mirroring `HandSkeleton.IsValid` — never `voiceTrack == null` after a load.
- **Verification:** pure logic is EditMode-tested on the Mac station; `Microphone`/`AudioSource`/permission/scene are verified in Play Mode or on the Quest (Mac Editor can't render hand tracking, but it CAN record the mic and play audio).
- **Naming:** `PascalCase` types, `camelCase` fields, `[SerializeField] private` for inspector fields; `[Serializable]` DTOs in `Domain/Model` hold data only. English throughout.
- **Scope:** scrub/slow-mo audio behaviour is OUT (deferred to the Player-controls slice). `VoicePlayer` is built clock-driven so it follows for free later.
- **Commits:** Conventional Commits; footer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.

---

## File Structure

**Created (Domain — pure, EditMode-tested):**
- `Assets/Scripts/Domain/Model/VoiceTrack.cs` — voice track metadata DTO.
- `Assets/Scripts/Domain/Sampling/VoiceSync.cs` — pure clock→playhead math.
- `Assets/Scripts/Domain/Audio/WavCodec.cs` — pure float[]↔16-bit-PCM-WAV encode/decode.

**Created (Foundation — device, Play Mode / Quest verified):**
- `Assets/Scripts/Foundation/Recording/IVoiceCaptureSource.cs` — capture seam + `VoiceCaptureResult` struct.
- `Assets/Scripts/Foundation/Recording/VoiceRecorder.cs` — `Microphone` capture + offset detection + WAV encode (MonoBehaviour).
- `Assets/Scripts/Foundation/Replay/VoicePlayer.cs` — spatialized, clock-driven playback (MonoBehaviour).

**Modified:**
- `Assets/Scripts/Domain/Model/Timeline.cs` — add `VoiceTrack voiceTrack`.
- `Assets/Scripts/Foundation/Persistence/MasterclassStore.cs` — per-masterclass directory + `WriteBlob`/`ReadBlob`.
- `Assets/Scripts/Foundation/Recording/RecordingSession.cs` — optional voice capture in `Begin`/`SaveTo`.
- `Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs` — pass the voice source + storage dir.
- `Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs` — load voice blob, drive `VoicePlayer` from `_clock`.
- `Assets/Plugins/Android/AndroidManifest.xml` — `RECORD_AUDIO`.
- `Assets/Scenes/MainVrScene.unity` — VoiceRecorder + VoicePlayer wiring (device task).

**Tests (EditMode):**
- `Assets/Tests/EditMode/VoiceTrackSerializationTests.cs`
- `Assets/Tests/EditMode/VoiceSyncTests.cs`
- `Assets/Tests/EditMode/WavCodecTests.cs`
- `Assets/Tests/EditMode/MasterclassStoreBlobTests.cs`
- `Assets/Tests/EditMode/RecordingSessionVoiceTests.cs`

---

### Task 1: VoiceTrack DTO + Timeline field

**Files:**
- Create: `Assets/Scripts/Domain/Model/VoiceTrack.cs`
- Modify: `Assets/Scripts/Domain/Model/Timeline.cs`
- Test: `Assets/Tests/EditMode/VoiceTrackSerializationTests.cs`

**Interfaces:**
- Produces: `[Serializable] class VoiceTrack { string clipRef; int sampleRate; int channels; float timelineOffsetSeconds; float durationSeconds; bool HasClip; }`; `Timeline.voiceTrack` (nullable in memory).

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/VoiceTrackSerializationTests.cs
using Ankhora.Domain.Model;
using Ankhora.Domain.Serialization;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class VoiceTrackSerializationTests
    {
        [Test]
        public void Timeline_WithVoiceTrack_RoundTripsAllFields()
        {
            var mc = new Masterclass { id = "mc-local", title = "t" };
            var tl = new Timeline { durationSeconds = 2f };
            tl.voiceTrack = new VoiceTrack
            {
                clipRef = "voice-ch-1.wav", sampleRate = 16000, channels = 1,
                timelineOffsetSeconds = 0.12f, durationSeconds = 1.9f
            };
            mc.chapters.Add(new Chapter { id = "ch-1", timeline = tl });

            var ser = new JsonMasterclassSerializer();
            VoiceTrack vt = ser.Deserialize(ser.Serialize(mc)).chapters[0].timeline.voiceTrack;

            Assert.IsTrue(vt.HasClip);
            Assert.AreEqual("voice-ch-1.wav", vt.clipRef);
            Assert.AreEqual(16000, vt.sampleRate);
            Assert.AreEqual(1, vt.channels);
            Assert.That(vt.timelineOffsetSeconds, Is.EqualTo(0.12f).Within(1e-4f));
            Assert.That(vt.durationSeconds, Is.EqualTo(1.9f).Within(1e-4f));
        }

        [Test]
        public void Timeline_NullVoiceTrack_RoundTripsToNotHasClip()
        {
            // JsonUtility cannot preserve null for nested [Serializable] fields: a null voiceTrack comes back
            // as a non-null default object. "No voice" is therefore discriminated by HasClip, not by null.
            var mc = new Masterclass { id = "mc-local", title = "t" };
            mc.chapters.Add(new Chapter { id = "ch-1", timeline = new Timeline { voiceTrack = null } });

            var ser = new JsonMasterclassSerializer();
            VoiceTrack vt = ser.Deserialize(ser.Serialize(mc)).chapters[0].timeline.voiceTrack;

            Assert.IsFalse(vt == null && true, "JsonUtility may return null or empty; either way must be !HasClip");
            Assert.IsFalse(vt != null && vt.HasClip, "a hands-only take must not read as having a clip");
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests (Unity Test Runner, EditMode tab) — or over MCP via `Unity_RunCommand` exercising the type.
Expected: FAIL — `VoiceTrack` does not exist / `Timeline.voiceTrack` undefined (compile error in the test assembly).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Assets/Scripts/Domain/Model/VoiceTrack.cs
using System;

namespace Ankhora.Domain.Model
{
    /// <summary>
    /// Metadata for one Chapter's recorded narration: a relative blob path + the timing needed to play it
    /// back in sync with the Hands Track. The audio bytes themselves live in a sibling blob, not here.
    /// </summary>
    [Serializable]
    public class VoiceTrack
    {
        public string clipRef;               // relative blob path, e.g. "voice-ch-1.wav"
        public int sampleRate;               // e.g. 16000
        public int channels;                 // 1 (mono)
        public float timelineOffsetSeconds;  // timeline t=0 -> first real audio sample (mic warm-up)
        public float durationSeconds;

        /// <summary>True when this track points at a real clip. The discriminator for "has voice", because
        /// JsonUtility cannot round-trip a null nested object (mirrors <see cref="HandSkeleton.IsValid"/>).</summary>
        public bool HasClip => !string.IsNullOrEmpty(clipRef);
    }
}
```

```csharp
// Assets/Scripts/Domain/Model/Timeline.cs — add this field alongside the existing ones
public VoiceTrack voiceTrack;   // optional; null/!HasClip means a hands-only take
```

- [ ] **Step 4: Run test to verify it passes**

Run the EditMode suite. Expected: PASS (both tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/Model/VoiceTrack.cs Assets/Scripts/Domain/Model/Timeline.cs Assets/Tests/EditMode/VoiceTrackSerializationTests.cs
git commit -m "feat(voice): VoiceTrack DTO + Timeline.voiceTrack with HasClip discriminator"
```

---

### Task 2: VoiceSync.AudioPlayhead (clock → playhead)

**Files:**
- Create: `Assets/Scripts/Domain/Sampling/VoiceSync.cs`
- Test: `Assets/Tests/EditMode/VoiceSyncTests.cs`

**Interfaces:**
- Produces: `static float VoiceSync.AudioPlayhead(float clock, float timelineOffsetSeconds)`.

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/VoiceSyncTests.cs
using Ankhora.Domain.Sampling;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class VoiceSyncTests
    {
        [Test]
        public void AudioPlayhead_AtOffset_IsZero()
            => Assert.That(VoiceSync.AudioPlayhead(0.2f, 0.2f), Is.EqualTo(0f).Within(1e-6f));

        [Test]
        public void AudioPlayhead_MidClip_IsClockMinusOffset()
            => Assert.That(VoiceSync.AudioPlayhead(1.5f, 0.2f), Is.EqualTo(1.3f).Within(1e-6f));

        [Test]
        public void AudioPlayhead_BeforeAudioStarts_IsNegative()
            => Assert.That(VoiceSync.AudioPlayhead(0.1f, 0.2f), Is.LessThan(0f));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — `VoiceSync` undefined.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Assets/Scripts/Domain/Sampling/VoiceSync.cs
namespace Ankhora.Domain.Sampling
{
    /// <summary>Pure replay-clock → audio-playhead mapping. Kept off any MonoBehaviour so the sync contract
    /// is EditMode-testable. A value &lt; 0 means the clock is before the audio's first sample (silence);
    /// the player keeps the source silent until it reaches 0.</summary>
    public static class VoiceSync
    {
        public static float AudioPlayhead(float clock, float timelineOffsetSeconds)
            => clock - timelineOffsetSeconds;
    }
}
```

- [ ] **Step 4: Run test to verify it passes** — Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/Sampling/VoiceSync.cs Assets/Tests/EditMode/VoiceSyncTests.cs
git commit -m "feat(voice): VoiceSync.AudioPlayhead pure clock-to-playhead math"
```

---

### Task 3: WavCodec (float[] ↔ 16-bit PCM WAV)

**Files:**
- Create: `Assets/Scripts/Domain/Audio/WavCodec.cs`
- Test: `Assets/Tests/EditMode/WavCodecTests.cs`

**Interfaces:**
- Produces: `static byte[] WavCodec.Encode(float[] samples, int sampleRate, int channels)`; `static bool WavCodec.TryDecode(byte[] wav, out float[] samples, out int sampleRate, out int channels)`.

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/WavCodecTests.cs
using Ankhora.Domain.Audio;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class WavCodecTests
    {
        [Test]
        public void Encode_WritesRiffWaveHeaderAndPcmDataSize()
        {
            byte[] wav = WavCodec.Encode(new[] { 0f, 0f, 0f }, 16000, 1);
            Assert.AreEqual('R', (char)wav[0]); Assert.AreEqual('I', (char)wav[1]);
            Assert.AreEqual('F', (char)wav[2]); Assert.AreEqual('F', (char)wav[3]);
            Assert.AreEqual('W', (char)wav[8]); Assert.AreEqual('A', (char)wav[9]);
            Assert.AreEqual('V', (char)wav[10]); Assert.AreEqual('E', (char)wav[11]);
            Assert.AreEqual(44 + 3 * 2, wav.Length, "44-byte header + 3 mono 16-bit samples");
        }

        [Test]
        public void Encode_MapsFullScaleSamplesToInt16Extremes()
        {
            byte[] wav = WavCodec.Encode(new[] { 1f, -1f, 0f, 2f }, 16000, 1);  // 2f must clamp to +full-scale
            short S(int i) => (short)(wav[44 + i * 2] | (wav[44 + i * 2 + 1] << 8));
            Assert.AreEqual(32767, S(0));
            Assert.AreEqual(-32767, S(1));
            Assert.AreEqual(0, S(2));
            Assert.AreEqual(32767, S(3));  // clamped
        }

        [Test]
        public void EncodeThenDecode_RoundTripsSamplesAndFormat()
        {
            var src = new[] { 0f, 0.5f, -0.5f, 0.999f, -0.999f };
            Assert.IsTrue(WavCodec.TryDecode(WavCodec.Encode(src, 16000, 1), out float[] outS, out int sr, out int ch));
            Assert.AreEqual(16000, sr);
            Assert.AreEqual(1, ch);
            Assert.AreEqual(src.Length, outS.Length);
            for (int i = 0; i < src.Length; i++)
                Assert.That(outS[i], Is.EqualTo(src[i]).Within(1f / 32767f + 1e-5f));
        }

        [Test]
        public void TryDecode_NonRiffBytes_ReturnsFalse()
            => Assert.IsFalse(WavCodec.TryDecode(new byte[] { 1, 2, 3, 4 }, out _, out _, out _));
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — Expected: FAIL — `WavCodec` undefined.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Assets/Scripts/Domain/Audio/WavCodec.cs
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Ankhora.Domain.Audio
{
    /// <summary>
    /// Pure encode/decode between PCM float samples ([-1, 1]) and a canonical 16-bit little-endian WAV byte
    /// stream. Kept in Domain (no Unity runtime audio types) so the wire format is EditMode-testable; the
    /// Foundation layer bridges it to <c>AudioClip</c>/<c>Microphone</c>. No external encoder dependency.
    /// </summary>
    public static class WavCodec
    {
        private const int HeaderBytes = 44;
        private const int BitsPerSample = 16;

        public static byte[] Encode(float[] samples, int sampleRate, int channels)
        {
            samples ??= Array.Empty<float>();
            int dataBytes = samples.Length * 2;
            using var ms = new MemoryStream(HeaderBytes + dataBytes);
            using var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);
            int byteRate = sampleRate * channels * (BitsPerSample / 8);
            short blockAlign = (short)(channels * (BitsPerSample / 8));

            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataBytes);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);                       // PCM fmt chunk size
            w.Write((short)1);                 // PCM
            w.Write((short)channels);
            w.Write(sampleRate);
            w.Write(byteRate);
            w.Write(blockAlign);
            w.Write((short)BitsPerSample);
            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(dataBytes);
            for (int i = 0; i < samples.Length; i++)
                w.Write((short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f));

            w.Flush();
            return ms.ToArray();
        }

        public static bool TryDecode(byte[] wav, out float[] samples, out int sampleRate, out int channels)
        {
            samples = Array.Empty<float>();
            sampleRate = 0;
            channels = 0;
            if (wav == null || wav.Length < HeaderBytes) return false;
            if (wav[0] != 'R' || wav[1] != 'I' || wav[2] != 'F' || wav[3] != 'F') return false;
            if (wav[8] != 'W' || wav[9] != 'A' || wav[10] != 'V' || wav[11] != 'E') return false;

            channels = wav[22] | (wav[23] << 8);
            sampleRate = wav[24] | (wav[25] << 8) | (wav[26] << 16) | (wav[27] << 24);
            int dataBytes = wav[40] | (wav[41] << 8) | (wav[42] << 16) | (wav[43] << 24);
            dataBytes = Mathf.Clamp(dataBytes, 0, wav.Length - HeaderBytes);

            int count = dataBytes / 2;
            samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                short s = (short)(wav[HeaderBytes + i * 2] | (wav[HeaderBytes + i * 2 + 1] << 8));
                samples[i] = s / 32767f;
            }
            return channels > 0 && sampleRate > 0;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes** — Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/Audio/WavCodec.cs Assets/Tests/EditMode/WavCodecTests.cs
git commit -m "feat(voice): WavCodec pure 16-bit PCM WAV encode/decode"
```

---

### Task 4: MasterclassStore per-masterclass directory + blob I/O

**Files:**
- Modify: `Assets/Scripts/Foundation/Persistence/MasterclassStore.cs`
- Modify: `Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs:28` (and `:62`), `Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs:21` (and `:39`)
- Test: `Assets/Tests/EditMode/MasterclassStoreBlobTests.cs`

**Interfaces:**
- Consumes: `Masterclass`, `JsonMasterclassSerializer` (unchanged).
- Produces: `MasterclassStore(string storageDir = "mc-local", IMasterclassSerializer serializer = null)`; `string BaseDir`; `bool Save(Masterclass, out string error)`; `bool TryLoad(out Masterclass, out string error)`; `bool WriteBlob(string relPath, byte[] bytes, out string error)`; `bool ReadBlob(string relPath, out byte[] bytes, out string error)`.

> Note: this changes the on-device layout from `persistentDataPath/masterclass.json` to `persistentDataPath/mc-local/manifest.json`. Existing device recordings are throwaway test data — no migration.

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/MasterclassStoreBlobTests.cs
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
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — Expected: FAIL — `BaseDir`/`WriteBlob`/`ReadBlob` undefined; `Save` writes a flat file.

- [ ] **Step 3: Write minimal implementation**

Replace `MasterclassStore.cs` body with:

```csharp
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

        /// <summary>Absolute path of the manifest file.</summary>
        public string Path => System.IO.Path.Combine(BaseDir, ManifestName);

        public bool Save(Masterclass masterclass, out string error)
        {
            try
            {
                Directory.CreateDirectory(BaseDir);
                File.WriteAllText(Path, _serializer.Serialize(masterclass));
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }

        public bool TryLoad(out Masterclass masterclass, out string error)
        {
            masterclass = null;
            if (!File.Exists(Path)) { error = $"No recording at {Path}"; return false; }
            try
            {
                masterclass = _serializer.Deserialize(File.ReadAllText(Path));
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }

        public bool WriteBlob(string relPath, byte[] bytes, out string error)
        {
            try
            {
                string full = System.IO.Path.Combine(BaseDir, relPath);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
                File.WriteAllBytes(full, bytes ?? Array.Empty<byte>());
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }

        public bool ReadBlob(string relPath, out byte[] bytes, out string error)
        {
            bytes = null;
            string full = System.IO.Path.Combine(BaseDir, relPath);
            if (!File.Exists(full)) { error = $"No blob at {full}"; return false; }
            try { bytes = File.ReadAllBytes(full); error = null; return true; }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
```

Then update the two call sites to pass a storage dir instead of a filename:
- `PinchRecordingTrigger.cs:28` — rename the field: `[SerializeField] private string _storageDir = "mc-local";` and at `:62` build `_store = new MasterclassStore(_storageDir);`.
- `GhostHandPlayer.cs:21` — rename: `[SerializeField] private string _storageDir = "mc-local";` and at `:39` build `_store = new MasterclassStore(_storageDir);`.

- [ ] **Step 4: Run test to verify it passes** — Expected: PASS (2 tests). Also run the full EditMode suite to confirm nothing else broke.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Foundation/Persistence/MasterclassStore.cs Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs Assets/Tests/EditMode/MasterclassStoreBlobTests.cs
git commit -m "feat(voice): per-masterclass directory + blob I/O in MasterclassStore"
```

---

### Task 5: RecordingSession voice hook + capture seam

**Files:**
- Create: `Assets/Scripts/Foundation/Recording/IVoiceCaptureSource.cs`
- Modify: `Assets/Scripts/Foundation/Recording/RecordingSession.cs`
- Modify: `Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs` (pass the voice source)
- Test: `Assets/Tests/EditMode/RecordingSessionVoiceTests.cs`

**Interfaces:**
- Consumes: `MasterclassStore.WriteBlob`, `VoiceTrack`, `Timeline.voiceTrack`.
- Produces: `interface IVoiceCaptureSource { bool IsAvailable { get; } void BeginCapture(float now); bool TryEndCapture(float now, out VoiceCaptureResult result); }`; `struct VoiceCaptureResult { byte[] wavBytes; int sampleRate; int channels; float timelineOffsetSeconds; float durationSeconds; }`; new `RecordingSession(IHandPoseSource, float, IVoiceCaptureSource = null)` overload.

- [ ] **Step 1: Write the failing test**

```csharp
// Assets/Tests/EditMode/RecordingSessionVoiceTests.cs
using Ankhora.Domain.Model;
using Ankhora.Foundation.Persistence;
using Ankhora.Foundation.Recording;
using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class RecordingSessionVoiceTests
    {
        // Minimal stubs (no headset): a pose source that yields one tracked frame, and a voice source that
        // returns canned WAV bytes.
        private class StubPose : IHandPoseSource
        {
            public bool TryGetHead(out Pose head) { head = Pose.identity; return true; }
            public bool TryGetHand(bool rightHand, ref HandPose pose)
            { pose.boneRotations = new[] { Quaternion.identity }; pose.boneLocalPositions = new[] { Vector3.zero }; return true; }
        }
        private class StubVoice : IVoiceCaptureSource
        {
            public bool Began, Ended;
            public bool IsAvailable => true;
            public void BeginCapture(float now) => Began = true;
            public bool TryEndCapture(float now, out VoiceCaptureResult r)
            {
                Ended = true;
                r = new VoiceCaptureResult { wavBytes = new byte[] { 9, 9, 9 }, sampleRate = 16000, channels = 1,
                    timelineOffsetSeconds = 0.1f, durationSeconds = 0.5f };
                return true;
            }
        }

        private string _dir;
        [SetUp] public void SetUp() => _dir = "test-mc-" + System.Guid.NewGuid().ToString("N");
        [TearDown] public void TearDown()
        {
            string p = Path.Combine(Application.persistentDataPath, _dir);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }

        [Test]
        public void SaveTo_WithVoiceSource_WritesBlobAndVoiceTrack()
        {
            var voice = new StubVoice();
            var session = new RecordingSession(new StubPose(), 30f, voice);
            var store = new MasterclassStore(_dir);

            session.Begin(0f);
            session.Tick(0f);
            Assert.IsTrue(session.SaveTo(store, 1f, out _, out string error), error);

            Assert.IsTrue(voice.Began && voice.Ended);
            Assert.IsTrue(store.TryLoad(out Masterclass mc, out _));
            VoiceTrack vt = mc.chapters[0].timeline.voiceTrack;
            Assert.IsTrue(vt.HasClip);
            Assert.AreEqual("voice-ch-1.wav", vt.clipRef);
            Assert.That(vt.timelineOffsetSeconds, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.IsTrue(store.ReadBlob(vt.clipRef, out byte[] blob, out _));
            Assert.AreEqual(new byte[] { 9, 9, 9 }, blob);
        }

        [Test]
        public void SaveTo_NoVoiceSource_LeavesHandsOnlyTake()
        {
            var session = new RecordingSession(new StubPose(), 30f);   // no voice
            var store = new MasterclassStore(_dir);
            session.Begin(0f); session.Tick(0f);
            Assert.IsTrue(session.SaveTo(store, 1f, out _, out _));
            store.TryLoad(out Masterclass mc, out _);
            Assert.IsFalse(mc.chapters[0].timeline.voiceTrack != null && mc.chapters[0].timeline.voiceTrack.HasClip);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — Expected: FAIL — `IVoiceCaptureSource`/`VoiceCaptureResult` undefined; `RecordingSession` has no voice ctor.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Assets/Scripts/Foundation/Recording/IVoiceCaptureSource.cs
namespace Ankhora.Foundation.Recording
{
    /// <summary>The voice equivalent of <see cref="IHandPoseSource"/>: a capture lifecycle the recorder
    /// drives alongside the hands. A null/unavailable source degrades the take to hands-only.</summary>
    public interface IVoiceCaptureSource
    {
        /// <summary>True when capture is possible right now (mic present + permission granted).</summary>
        bool IsAvailable { get; }

        /// <summary>Start capturing; <paramref name="now"/> is the same timeline zero the recorder uses.</summary>
        void BeginCapture(float now);

        /// <summary>Stop capturing and emit the encoded take. Returns false if nothing usable was captured.</summary>
        bool TryEndCapture(float now, out VoiceCaptureResult result);
    }

    /// <summary>An encoded voice take: WAV bytes + the metadata the manifest stores.</summary>
    public struct VoiceCaptureResult
    {
        public byte[] wavBytes;
        public int sampleRate;
        public int channels;
        public float timelineOffsetSeconds;
        public float durationSeconds;
    }
}
```

Modify `RecordingSession.cs`: add the field + constructor overload, drive the voice source in `Begin`/`SaveTo`.

```csharp
// add field
private readonly IVoiceCaptureSource _voice;

// replace the constructor with an overload that defaults voice to null
public RecordingSession(IHandPoseSource source, float sampleRateHz, IVoiceCaptureSource voice = null)
{
    _source = source;
    _skeletonSource = source as IHandSkeletonSource;
    _recorder = new TimelineRecorder(sampleRateHz);
    _voice = voice;
}

// in Begin(now), after _recorder.Begin(now):
if (_voice != null && _voice.IsAvailable)
    _voice.BeginCapture(now);
```

In `SaveTo`, after the null-guard and before `store.Save`, attach the voice track + write the blob (chapter id is `"ch-1"`):

```csharp
// build the chapter id once; the voice clip is addressed relative to it
const string chapterId = "ch-1";
if (_voice != null && _voice.TryEndCapture(now, out VoiceCaptureResult voice) &&
    voice.wavBytes != null && voice.wavBytes.Length > 0)
{
    string clipRef = $"voice-{chapterId}.wav";
    if (store.WriteBlob(clipRef, voice.wavBytes, out string blobError))
    {
        timeline.voiceTrack = new VoiceTrack
        {
            clipRef = clipRef, sampleRate = voice.sampleRate, channels = voice.channels,
            timelineOffsetSeconds = voice.timelineOffsetSeconds, durationSeconds = voice.durationSeconds
        };
    }
    else
    {
        Debug.LogWarning($"[RecordingSession] Voice blob write failed, saving hands-only: {blobError}");
    }
}

var masterclass = new Masterclass { id = "mc-local", title = "Local recording" };
masterclass.chapters.Add(new Chapter { id = chapterId, timeline = timeline });
return store.Save(masterclass, out error);
```

Wire the source in `PinchRecordingTrigger`: add `[SerializeField] private MonoBehaviour _voiceSourceBehaviour;   // implements IVoiceCaptureSource (optional)` and in `Awake`, build the session with it:

```csharp
var voice = _voiceSourceBehaviour as IVoiceCaptureSource;
_session = new RecordingSession(source, _sampleRateHz, voice);
```

- [ ] **Step 4: Run test to verify it passes** — Expected: PASS (2 tests) + full EditMode suite green.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Foundation/Recording/IVoiceCaptureSource.cs Assets/Scripts/Foundation/Recording/RecordingSession.cs Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs Assets/Tests/EditMode/RecordingSessionVoiceTests.cs
git commit -m "feat(voice): drive optional voice capture from RecordingSession"
```

---

### Task 6: VoiceRecorder (Microphone capture → WAV)

**Files:**
- Create: `Assets/Scripts/Foundation/Recording/VoiceRecorder.cs`

**Interfaces:**
- Consumes: `IVoiceCaptureSource`, `VoiceCaptureResult`, `WavCodec.Encode`.
- Produces: `class VoiceRecorder : MonoBehaviour, IVoiceCaptureSource`.

This is device/Play-Mode code (the `Microphone` API). Its pure parts (offset arithmetic, WAV encoding) are already covered by Tasks 2–3, so this task is verified by recording a real take, not by an EditMode test.

- [ ] **Step 1: Write the implementation**

```csharp
// Assets/Scripts/Foundation/Recording/VoiceRecorder.cs
using Ankhora.Domain.Audio;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Captures the Instructor's narration via <see cref="Microphone"/> alongside a hands take and emits it
    /// as a 16-bit PCM WAV (<see cref="WavCodec"/>). Implements <see cref="IVoiceCaptureSource"/> so
    /// <see cref="RecordingSession"/> drives it. The real start-of-audio offset is measured from the first
    /// frame the mic reports samples, so warm-up latency does not desync replay. Device/Play-Mode verified.
    /// </summary>
    public class VoiceRecorder : MonoBehaviour, IVoiceCaptureSource
    {
        [SerializeField, Min(8000)] private int _requestedSampleRate = 16000;
        [SerializeField, Min(1)] private int _maxSeconds = 600;   // ring-buffer ceiling for one take

        private string _device;
        private AudioClip _clip;
        private bool _capturing;
        private float _beginNow;            // timeline zero (the recorder's clock)
        private float _firstSampleOffset;   // seconds from _beginNow to the first delivered sample
        private bool _firstSampleSeen;

        public bool IsAvailable => Microphone.devices != null && Microphone.devices.Length > 0;

        public void BeginCapture(float now)
        {
            if (!IsAvailable) return;
            _device = Microphone.devices[0];
            _beginNow = now;
            _firstSampleOffset = 0f;
            _firstSampleSeen = false;
            _clip = Microphone.Start(_device, loop: false, _maxSeconds, _requestedSampleRate);
            _capturing = true;
        }

        private void Update()
        {
            if (!_capturing || _firstSampleSeen) return;
            if (Microphone.GetPosition(_device) > 0)
            {
                _firstSampleOffset = Mathf.Max(0f, Time.unscaledTime - _beginNow);
                _firstSampleSeen = true;
            }
        }

        public bool TryEndCapture(float now, out VoiceCaptureResult result)
        {
            result = default;
            if (!_capturing || _clip == null) return false;

            int sampleCount = Microphone.GetPosition(_device);   // samples written so far (per channel)
            Microphone.End(_device);
            _capturing = false;
            if (sampleCount <= 0) return false;

            int channels = _clip.channels;
            int sampleRate = _clip.frequency;
            var data = new float[sampleCount * channels];
            _clip.GetData(data, 0);

            result = new VoiceCaptureResult
            {
                wavBytes = WavCodec.Encode(data, sampleRate, channels),
                sampleRate = sampleRate,
                channels = channels,
                timelineOffsetSeconds = _firstSampleOffset,
                durationSeconds = (float)sampleCount / sampleRate
            };
            _clip = null;
            return true;
        }

        private void OnDisable()
        {
            if (_capturing) { Microphone.End(_device); _capturing = false; }
        }
    }
}
```

- [ ] **Step 2: Verify (Play Mode on Mac — the mic works in the Editor)**

In a scratch scene or the main scene, attach `VoiceRecorder`, enter Play Mode, and over MCP run a `Unity_RunCommand` that calls `BeginCapture(Time.unscaledTime)`, waits a moment, then `TryEndCapture` and logs `result.wavBytes.Length > 44`, `sampleRate`, and `timelineOffsetSeconds >= 0`. Expected: non-empty WAV, a plausible sample rate (often 48000 or 16000), offset ≥ 0.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Foundation/Recording/VoiceRecorder.cs
git commit -m "feat(voice): VoiceRecorder Microphone capture with measured start offset"
```

---

### Task 7: VoicePlayer (spatialized, clock-driven playback)

**Files:**
- Create: `Assets/Scripts/Foundation/Replay/VoicePlayer.cs`

**Interfaces:**
- Consumes: `VoiceTrack`, `WavCodec.TryDecode`, `VoiceSync.AudioPlayhead`.
- Produces: `class VoicePlayer : MonoBehaviour` with `void Load(byte[] wavBytes, VoiceTrack track)`, `void Stop()`, `void Tick(float clock, bool playing, Vector3 headPosition)`.

Device-verified (Meta XR Audio spatializer + AudioSource). Its sync math is `VoiceSync` (Task 2), already tested.

- [ ] **Step 1: Write the implementation**

```csharp
// Assets/Scripts/Foundation/Replay/VoicePlayer.cs
using Ankhora.Domain.Audio;
using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Plays a recorded <see cref="VoiceTrack"/> spatialized from the ghost's head, locked to the replay
    /// clock the <see cref="GhostHandPlayer"/> owns (never its own clock). The AudioSource uses the Meta XR
    /// Audio spatializer (set the project Spatializer Plugin to "Meta XR Audio"). Device-verified.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VoicePlayer : MonoBehaviour
    {
        [Tooltip("Re-seek the clip when |AudioSource.time - target| exceeds this (loop wrap, future scrub).")]
        [SerializeField, Min(0.02f)] private float _resyncThreshold = 0.08f;

        private AudioSource _source;
        private float _offset;
        private bool _loaded;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 1f;   // full 3D; the Meta XR Audio spatializer does the rest
        }

        /// <summary>Decode the WAV blob into a clip and arm playback. No-op if the track has no clip.</summary>
        public void Load(byte[] wavBytes, VoiceTrack track)
        {
            _loaded = false;
            if (track == null || !track.HasClip) return;
            if (!WavCodec.TryDecode(wavBytes, out float[] samples, out int sampleRate, out int channels))
            {
                Debug.LogWarning("[VoicePlayer] Could not decode voice clip; replay is hands-only.", this);
                return;
            }
            var clip = AudioClip.Create(track.clipRef, samples.Length / Mathf.Max(1, channels), channels, sampleRate, false);
            clip.SetData(samples, 0);
            _source.clip = clip;
            _offset = track.timelineOffsetSeconds;
            _loaded = true;
        }

        public void Stop()
        {
            if (_source != null && _source.isPlaying) _source.Stop();
        }

        /// <summary>Drive one frame from the owning player: position the source at the ghost head, keep the
        /// clip aligned to the clock, and play/pause with the replay. Called every frame, never self-clocked.</summary>
        public void Tick(float clock, bool playing, Vector3 headPosition)
        {
            if (!_loaded) return;
            transform.position = headPosition;   // voice emanates from the ghost's head as it moves

            float target = VoiceSync.AudioPlayhead(clock, _offset);

            if (!playing || target < 0f || target >= _source.clip.length)
            {
                if (_source.isPlaying) _source.Stop();
                return;
            }

            if (!_source.isPlaying)
            {
                _source.time = target;
                _source.Play();
            }
            else if (Mathf.Abs(_source.time - target) > _resyncThreshold)
            {
                _source.time = target;   // loop wrap / large jump / future scrub
            }
        }
    }
}
```

- [ ] **Step 2: Set the project spatializer**

Project Settings → Audio → **Spatializer Plugin = Meta XR Audio**. Confirm via `Unity_ReadConsole` that no audio-spatializer errors appear on Play.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Foundation/Replay/VoicePlayer.cs
git commit -m "feat(voice): VoicePlayer spatialized clock-driven playback"
```

---

### Task 8: GhostHandPlayer drives VoicePlayer from the clock

**Files:**
- Modify: `Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs`

**Interfaces:**
- Consumes: `VoicePlayer.Load/Stop/Tick`, `MasterclassStore.ReadBlob`, `TimelineSampler.SampleHead`, `Timeline.voiceTrack`.

- [ ] **Step 1: Write the implementation**

Add a serialized `VoicePlayer` reference and drive it from the existing clock. In `GhostHandPlayer`:

```csharp
[SerializeField] private VoicePlayer _voicePlayer;   // optional; same Replay feature folder
```

In `LoadAndPlay()`, after `_timeline = mc.chapters[0].timeline;` and the bone-buffer/skeleton setup, load the voice blob:

```csharp
if (_voicePlayer != null)
{
    VoiceTrack vt = _timeline.voiceTrack;
    if (vt != null && vt.HasClip && _store.ReadBlob(vt.clipRef, out byte[] wav, out string vErr))
        _voicePlayer.Load(wav, vt);
    else
    {
        if (vt != null && vt.HasClip) Debug.LogWarning($"[GhostHandPlayer] Voice blob missing: {vErr}");
        _voicePlayer.Stop();
    }
}
```

In `Update()`, after the two `DriveHand` calls, drive the voice from the SAME `_clock`:

```csharp
if (_voicePlayer != null)
    _voicePlayer.Tick(_clock, _playing, TimelineSampler.SampleHead(_timeline, _clock).position);
```

In `Stop()`, stop the voice too:

```csharp
if (_voicePlayer != null) _voicePlayer.Stop();
```

- [ ] **Step 2: Verify (compile)**

Over MCP, confirm `Unity_ReadConsole` shows no compile errors after the edit.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs
git commit -m "feat(voice): drive VoicePlayer from the ghost replay clock"
```

---

### Task 9: AndroidManifest RECORD_AUDIO + runtime permission

**Files:**
- Modify: `Assets/Plugins/Android/AndroidManifest.xml`
- Modify: `Assets/Scripts/Foundation/Recording/VoiceRecorder.cs` (request permission on enable)

- [ ] **Step 1: Add the permission**

In `Assets/Plugins/Android/AndroidManifest.xml`, add inside `<manifest>` (next to the other `uses-permission`):

```xml
  <uses-permission android:name="android.permission.RECORD_AUDIO" />
```

- [ ] **Step 2: Request it at runtime + gate availability**

In `VoiceRecorder.cs`, request the Android permission on enable and make `IsAvailable` also require it:

```csharp
// add at top:
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// in the class:
private void OnEnable()
{
#if UNITY_ANDROID && !UNITY_EDITOR
    if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        Permission.RequestUserPermission(Permission.Microphone);
#endif
}

// update IsAvailable:
public bool IsAvailable
{
    get
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone)) return false;
#endif
        return Microphone.devices != null && Microphone.devices.Length > 0;
    }
}
```

When permission is denied, `IsAvailable` is false → `RecordingSession` never calls `BeginCapture`/`TryEndCapture` → the take is hands-only (graceful, no crash).

- [ ] **Step 3: Verify (device)**

Build & run; confirm the OS shows the microphone permission prompt on first launch (`unityplayer.SkipPermissionsDialog` is already `false`). Deny once and confirm a recording still saves hands-only (no exception in `logcat -s Unity`).

- [ ] **Step 4: Commit**

```bash
git add Assets/Plugins/Android/AndroidManifest.xml Assets/Scripts/Foundation/Recording/VoiceRecorder.cs
git commit -m "feat(voice): RECORD_AUDIO permission with hands-only fallback when denied"
```

---

### Task 10: Scene wiring + on-device verification

**Files:**
- Modify: `Assets/Scenes/MainVrScene.unity` (via Unity MCP / Editor — do not hand-edit)

This task has no EditMode test — it is the end-to-end device verification the whole slice builds toward.

- [ ] **Step 1: Add + wire the components (Unity MCP)**

1. Add a `VoiceRecorder` component to the recording rig GameObject (the same object as `OvrHandPoseSource` / the `RecordingTrigger`), and assign it to `PinchRecordingTrigger._voiceSourceBehaviour`.
2. Add a child GameObject `VoiceSource` under `GhostHandPlayer` with an `AudioSource` + `VoicePlayer`; assign it to `GhostHandPlayer._voicePlayer`.
3. Project Settings → Audio → Spatializer Plugin = **Meta XR Audio** (Task 7 Step 2 — confirm it is set).
4. Save the scene (`Unity_ManageScene Save`).

- [ ] **Step 2: Device verification (Quest — the real acceptance test)**

Build & run (`Cmd+B`), then:
- Record a take while narrating. Confirm the OS mic prompt appears on first run.
- On replay, confirm: **voice is in sync** with the ghost hands; it **emanates from the ghost's head** and moves as the head moves; **loop** restarts cleanly; before the audio's first sample the source is silent (offset honoured).
- `logcat -s Unity` shows the saved frame count and no audio/permission errors; the Meta spatializer is active (no "no spatializer" warning).
- Deny mic permission once → the take records and replays hands-only with no error.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/MainVrScene.unity
git commit -m "feat(voice): wire VoiceRecorder + VoicePlayer into the main scene"
```

---

## Self-Review

**Spec coverage:** §1 data model → T1; §1 sync math → T2; §2 capture (Microphone + WAV + offset) → T3 (codec) + T6 (capture) + T9 (permission); §2 `IVoiceCaptureSource` seam + RecordingSession hook → T5; §3 replay (Meta XR Audio, head-positioned, clock-synced, drift threshold) → T7 + T8; §4 storage (per-masterclass dir + blobs) → T4; §6 tests → T1–T5 (EditMode) + T6/T10 (device). All spec sections map to a task.

**Placeholder scan:** No TBD/"handle errors"/"similar to". Every code step has complete code; every device step names the exact thing to observe.

**Type consistency:** `VoiceTrack` (fields `clipRef/sampleRate/channels/timelineOffsetSeconds/durationSeconds`, `HasClip`) is used identically in T1, T5, T7, T8. `IVoiceCaptureSource` (`IsAvailable`, `BeginCapture`, `TryEndCapture`) and `VoiceCaptureResult` (`wavBytes/sampleRate/channels/timelineOffsetSeconds/durationSeconds`) match across T5, T6. `WavCodec.Encode/TryDecode`, `VoiceSync.AudioPlayhead`, `MasterclassStore.WriteBlob/ReadBlob/BaseDir` are consistent across their producers and consumers. `_storageDir` replaces `_fileName` in both `PinchRecordingTrigger` and `GhostHandPlayer` (T4).

**Note on JsonUtility:** the null-nested-object quirk is handled uniformly through `VoiceTrack.HasClip`, asserted in T1 and used as the discriminator in T5/T7/T8.

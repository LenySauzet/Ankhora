# Voice Capture + Replay — design

> The Instructor's narration is captured in the same take as the ghost hands, and replays
> spatialized from the ghost's head, locked to the single replay clock. Next build slice on
> top of the merged record/replay spine (Domain + Foundation, [ADR-0004](../../02-architecture/adr/0004-domain-foundation-two-assembly-split.md))
> and the hand-tracking work (PR #33).
>
> *Authored 2026-06-29 via a brainstorming session. Approved by Lény before planning.*

## Goal

One take captures voice + hands together; the Learner replays it with the narration
spatialized from the expert's recorded head, perfectly in sync with the ghost hands —
because audio and hands are driven by the **same** playback clock, never two.

## Where this sits

- **MVP commitment** (`docs/01-product/mvp-scope.md`): *"Record one take: Voice Track +
  Hands Track captured together"*; *"Hands + Voice replay in sync"*. The glossary defines a
  **Voice Track** as a time-continuous recording on the Chapter Timeline (MVP — core).
- **Spine it builds on:** `Timeline` / `PoseFrame` / `HandPose` (Domain model),
  `RecordingSession` + `TimelineRecorder` (capture), `GhostHandPlayer` + `TimelineSampler`
  (replay). `GhostHandPlayer` owns `_clock` (advanced by `Time.unscaledDeltaTime`), the one
  sync anchor everything rides.
- **Skill:** `.claude/skills/voice-spatial-audio/SKILL.md` (Microphone capture, Meta XR
  Audio spatializer, share the clock). This spec deviates from its "compressed Vorbis/AAC"
  note — see §2.

## Scope (YAGNI-tight)

**In this slice:**
- Voice capture riding the existing pinch-triggered take (no new trigger).
- WAV/PCM blob persistence.
- Spatialised, in-sync linear replay; loop (already exists in `GhostHandPlayer`).
- Runtime `RECORD_AUDIO` permission request; graceful hands-only fallback if denied.

**Deferred (NOT this slice):**
- Scrub & slow-motion audio behaviour — the Player-controls slice doesn't exist yet.
  `VoicePlayer` is built clock-driven so it follows for free when those land; the *audio*
  policy under slow-mo (mute vs pitch-shift) is decided in that slice, not here.
- Compression, noise suppression, multi-take mixing, TTS (all V2+).

## Architecture

Two assemblies, per ADR-0004. Pure timing/serialisation math in `Ankhora.Domain`
(EditMode-tested, no engine); `Microphone` / `AudioSource` / OVR in `Ankhora.Foundation`
(device-verified). Feature folders don't reference each other — wiring lives in
`Foundation/App/RecordReplayLink`.

### 1. Data model — `Ankhora.Domain` (pure, EditMode-tested)

New `VoiceTrack`, referenced (nullable) by `Timeline`. Null = no voice (hands-only take).

```csharp
// Assets/Scripts/Domain/Model/VoiceTrack.cs
[Serializable]
public class VoiceTrack
{
    public string clipRef;               // relative blob path, e.g. "voice-<chapterId>.wav"
    public int sampleRate;               // 16000
    public int channels;                 // 1 (mono)
    public float timelineOffsetSeconds;  // timeline t=0 -> first real audio sample (mic warm-up)
    public float durationSeconds;
}
```

```csharp
// Added to Assets/Scripts/Domain/Model/Timeline.cs
public VoiceTrack voiceTrack;   // optional; null when the take has no voice
```

**Sync is pure math**, and is the EditMode test target. A new pure helper maps the replay
clock to the audio playhead:

```csharp
// Assets/Scripts/Domain/Sampling/VoiceSync.cs
public static class VoiceSync
{
    /// <summary>Audio playhead (seconds into the clip) for a given replay clock.
    /// Returns a value < 0 when the clock is before the audio starts (silence);
    /// callers clamp/skip playback until it reaches 0.</summary>
    public static float AudioPlayhead(float clock, float timelineOffsetSeconds)
        => clock - timelineOffsetSeconds;
}
```

(Loop wrap is handled by the caller resetting `clock`; the helper stays a pure subtraction so
the test pins the exact contract, including the negative-before-start case.)

### 2. Capture — `Foundation/Recording/VoiceRecorder.cs` (new, MonoBehaviour)

- Wraps `Microphone.Start(deviceName: null, loop: false, lengthSec, frequency: 16000)`.
  Started by the **same** `RecordingSession.Begin(now)` call that starts the hands recorder,
  so audio and hands share timeline t=0.
- **Captures the real offset:** record the wall-time of the first frame where
  `Microphone.GetPosition(null) > 0`, minus `Begin`'s `now` → `timelineOffsetSeconds`. This
  absorbs mic start-up latency precisely instead of assuming zero.
- On `Finish`: `Microphone.End(null)`, trim the captured `AudioClip` to the real recorded
  length, write a `.wav` blob via a small PCM byte-writer (16-bit mono — no external
  dependency), and produce the `VoiceTrack` for `RecordingSession.SaveTo`.
- **Format decision (deviation from the skill):** WAV/PCM 16 kHz mono. Unity ships no
  runtime OGG/AAC encoder; PCM is ~2 MB/min, so a 2-min take ≈ 4 MB — negligible for a
  side-loaded local MVP. Compression is a V2 optimisation that would cost a native encoder
  plugin + device validation we don't have time for.
- **Permission:** first run requests `android.permission.RECORD_AUDIO` (manifest entry added
  via `meta_update_android_manifest`; runtime request via Unity's Android permission API).
  If denied, the take records hands-only (`voiceTrack` stays null) — no crash, no block.

`RecordingSession` gains an optional `IVoiceCaptureSource` seam (same pattern as
`IHandPoseSource`) so the capture lifecycle (`Begin` / `Finish`) drives voice alongside
hands, and so a null/absent source degrades to hands-only cleanly.

### 3. Replay — `Foundation/Replay/VoicePlayer.cs` (new) + Meta XR Audio

- `GhostHandPlayer` owns `_clock`; it composes a `VoicePlayer`, wired through
  `Foundation/App/RecordReplayLink` (ADR-0004 — no cross-feature references).
- `VoicePlayer` loads the WAV blob → `AudioClip`, on an `AudioSource` configured
  `spatialBlend = 1` (full 3D) with the **Meta XR Audio** spatializer (Project Settings →
  Audio → Spatializer Plugin = Meta XR Audio).
- Each `Update` (driven by `GhostHandPlayer`, not its own clock):
  1. Position the `AudioSource` at `TimelineSampler.SampleHead(_timeline, _clock).position`
     — the ghost's head, so the voice moves with the expert.
  2. Keep `AudioSource.time` synced to `VoiceSync.AudioPlayhead(_clock, offset)`, with a
     drift threshold: re-seek on loop wrap, large jumps, and future scrub; otherwise let the
     clip free-run to avoid per-frame stutter.
  3. Play/pause follows `GhostHandPlayer._playing`; before the audio playhead reaches 0
     (clock < offset) the source stays silent.

### 4. Storage — `Foundation/Persistence/MasterclassStore` (extended)

Move from a single `masterclass.json` at `persistentDataPath` root to a **per-masterclass
directory**:

```
persistentDataPath/
  <masterclassId>/
    manifest.json            # the Masterclass (was masterclass.json)
    voice-<chapterId>.wav    # the voice blob
    images/pin-<id>.jpg      # (future Pins slice reuses this home)
```

This is the natural home Pin image blobs will reuse, and matches the `clipRef` relative-path
model. `MasterclassStore` gains `WriteBlob(relPath, bytes)` / `ReadBlob(relPath) -> bytes`
plus a base-directory resolve; `JsonMasterclassSerializer` is unchanged. Existing on-device
recordings are throwaway test data — no migration path needed.

## Testing & verification

- **EditMode (Domain, no headset):**
  - `VoiceTrack` JSON round-trip through `JsonMasterclassSerializer` (field-for-field).
  - `VoiceSync.AudioPlayhead`: at offset, mid-clip, and the negative-before-start case.
  - `Timeline` with a null `voiceTrack` round-trips (hands-only take stays valid).
- **Device (Mac Editor can't render hand tracking):** record a take, then confirm:
  voice replays **in sync** with the ghost hands; it emanates from the ghost's head as the
  head moves; loop restarts cleanly; the Meta spatializer is active (console); the
  permission prompt appears on first run and a denied permission yields a clean hands-only take.

## Files

| File | Change |
|---|---|
| `Assets/Scripts/Domain/Model/VoiceTrack.cs` | **new** — track metadata DTO |
| `Assets/Scripts/Domain/Model/Timeline.cs` | add `VoiceTrack voiceTrack` |
| `Assets/Scripts/Domain/Sampling/VoiceSync.cs` | **new** — pure clock→playhead math |
| `Assets/Scripts/Foundation/Recording/VoiceRecorder.cs` | **new** — Microphone capture + WAV write |
| `Assets/Scripts/Foundation/Recording/IVoiceCaptureSource.cs` | **new** — capture seam |
| `Assets/Scripts/Foundation/Recording/RecordingSession.cs` | drive voice capture in `Begin`/`Finish`/`SaveTo` |
| `Assets/Scripts/Foundation/Replay/VoicePlayer.cs` | **new** — spatialized clock-driven playback |
| `Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs` | compose + drive `VoicePlayer` from `_clock` |
| `Assets/Scripts/Foundation/App/RecordReplayLink.cs` | wire recorder↔player voice ends |
| `Assets/Scripts/Foundation/Persistence/MasterclassStore.cs` | per-masterclass dir + blob read/write |
| `Assets/Plugins/Android/AndroidManifest.xml` | `RECORD_AUDIO` permission |
| `Assets/Tests/EditMode/VoiceTrackSerializationTests.cs` | **new** |
| `Assets/Tests/EditMode/VoiceSyncTests.cs` | **new** |

## Open follow-ups (out of this slice)

- Slow-mo audio policy (mute vs pitch-shift) — decided in the Player-controls slice.
- The Building-Block hand-rig migration ([ADR-0005](../../02-architecture/adr/0005-adopt-meta-building-blocks.md))
  is the first task of the Pins slice, not this one; voice keeps reading the current rig's head/skeleton.

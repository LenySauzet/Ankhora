# Domain · Define the masterclass record/replay data model + local persistence

- Status: in-progress
- Owner: @LenySauzet
- Tracking issue: #25 · PR #26
- Last updated: 2026-06-25

## Progress

Built TDD (`Unity -runTests` EditMode via CLI), each slice RED → GREEN → committed:

- **Done (full model)** — `Masterclass` → `Chapter` → `Timeline` (frames + pins) → `PoseFrame`
  (head + left/right `HandPose`); `IMasterclassSerializer` / `JsonMasterclassSerializer`
  (round-trip, robust `Deserialize` guard, `schemaVersion` migration); `Timeline.Sample(t)`
  (clamp + Lerp/Slerp, allocation-free). Hand set confirmed against Meta docs (OVRSkeleton 19
  skinnable / OpenXR 26 joints) → `HandPose` is count-agnostic. **12/12 EditMode tests.**
- **Next (separate follow-up features)** — capture (`recorder`); replay rendering + hand
  interpolation in `Sample(t)` (`playback`); pin authoring UI (`ui`).

## Why

The record/replay data model is **Ankhora's spine** (`.claude/skills/record-replay-contract`):
capture, replay, audio, pins and (V2) anchors all flow through one contract. Getting it
right first — as plain, EditMode-testable C# — unblocks both the Instructor (record) and
Learner (Player) sides of the committed MVP (`docs/01-product/mvp-scope.md`). It is the
ideal first build step on the Mac station: pure logic, no headset, no hand-tracking-in-Editor
needed.

## What

No user-facing behaviour by itself. It is the in-memory + on-disk format that:

- the **Instructor** writes when recording one take (voice + hands) and placing Text/Image Pins;
- the **Learner**'s Player reads to replay hands + voice in sync, show Pins on their time range,
  scrub / slow-mo / loop, and persist the per-Chapter `completed` flag.

## How

- **Plain C# DTOs, no `MonoBehaviour`** (`unity-testability`). Unity components only *produce*
  and *consume* the data; the contract itself is serialisable data, fully EditMode-tested. The
  `Ankhora.Domain` assembly consciously references UnityEngine (for `JsonUtility` and the
  `Pose`/`Vector3`/`Quaternion` math types) — acceptable for the MVP; a separate serialisation
  assembly is a V2 option, not needed now.
- **Versioned**: `schemaVersion` at the top of the persisted unit; plan migration, never break
  old files silently. `Deserialize` rejects `null`/empty/malformed payloads with a clear error
  rather than returning a silent `null`.
- **Single monotonic clock**: one timeline in seconds per Chapter. Hands, voice and pins all
  read the *same* clock. Sampled at a fixed capture rate (default **30 Hz**, revisit at 60 Hz for
  fast gestures), decoupled from frame rate.
- **Interpolation contract** (`Timeline.Sample(t)`, pure math, EditMode-tested): position uses
  `Vector3.LerpUnclamped`, rotation uses `Quaternion.SlerpUnclamped`, between the two bracketing
  frames. `t` outside the recorded range **clamps** to the first / last frame (it never throws,
  and clamps to frame times — not to `durationSeconds`).
- **Value types for the hot path**: `PoseFrame`, `Pose` and (later) `HandPose` are `struct`, so
  `Sample(t)` and per-frame iteration allocate nothing in the replay loop.
- **Deterministic**: same file → same replay. No wall-clock, no per-run randomness in the data.
- **Serialiser behind an interface**: JSON manifest for dev readability + binary blobs for the
  heavy tracks (hand poses, audio, images). Swap to a compact/compressed frame format later
  without touching capture/replay.
- **Hand pose representation**: store **local bone rotations + a root pose** (compact,
  retargetable), not world transforms. ⚠️ Confirm the exact `OVRSkeleton`/`OVRHand` bone set
  and pose representation via context7 / Meta docs **before** fixing field shapes — do not
  invent bone counts.

Files this will create (under `Assets/Scripts/Domain/` + `Assets/Tests/EditMode/`): the DTOs,
an `IMasterclassSerializer` (JSON impl), and a `Timeline.Sample(t)` sampler. No scene/prefab
changes in this feature.

## Data model

Reconciles the product view (`mvp-scope.md`) with the engineering contract
(`record-replay-contract`): Masterclass → Chapters → a per-Chapter sampled Timeline.

```text
Masterclass
  schemaVersion : int
  id, title
  chapters : [ Chapter ]            # ordered

Chapter
  id, title, order
  modelRef? : string                # bundled-model reference, optional
  completed : bool                  # learner-side progress (see "Recording vs progress" below)
  timeline : Timeline

Timeline
  durationSeconds : float                          # authoritative metadata; Sample() clamps to frame times, not this
  audio? : { clipRef, offsetSeconds }            # see voice-spatial-audio
  frames : [ PoseFrame ]                          # sampled at the fixed rate
  pins : [ Pin ]

PoseFrame
  t : float                                        # seconds from start (single clock)
  head : Pose                                       # position + rotation
  leftHand?  : HandPose                             # local bone rotations + root pose
  rightHand? : HandPose

Pin
  id
  type : text | image
  payload : string                                 # inline UTF-8 text when type==text; relative blob path when type==image
  pose : Pose                                       # position + rotation (panel uses rotation to face the learner)
  timeRange? : { start, end }                       # when the Pin is visible
```

Persisted as a JSON manifest + blob files (audio, hand recording, images) under the app's
device storage. This local format is the same one a V2 backend will later sync.

**Recording vs progress.** For the MVP the on-device manifest is the **learner's mutable working
copy**: marking a Chapter complete writes `completed` back to that file, and the authored /
bundled recording is treated as the read-only source. When a V2 backend lands, learner progress
moves to a separate `MasterclassProgress` record (same `id`) so the recording itself stays
immutable and syncable — not split now (YAGNI for a local, single-device MVP).

## Acceptance criteria

- [x] DTOs are plain C# (no `MonoBehaviour`) under `Assets/Scripts/Domain/`.
- [x] EditMode: round-trip serialise → deserialise equality on a representative fixture.
- [x] EditMode: `Timeline.Sample(t)` correct at frame boundaries and interpolated at midpoints.
- [x] Serialiser sits behind an interface so the on-disk format can change in isolation.
- [x] No allocation in the `Sample(t)` hot path (value-type frames; Profiler check when wired to replay).
- [x] `Deserialize` rejects `null`/empty/malformed payloads with a clear error (no silent `null`).
- [x] `schemaVersion` migration hook exists (no-op for v1; throws on unknown version).
- [x] EditMode: an unknown/future `schemaVersion` is rejected with a clear error (forward
      migration of old fixtures is a no-op until a v2 exists).
- [x] Nested `Chapter` / `Pin` round-trip preserved.
- [x] `HandPose` fields fixed against the confirmed bone set (count-agnostic: OVRSkeleton 19 /
      OpenXR 26 — the recorder picks the count).

## Out of scope

- Capture (writing real hand/voice data) — follow-up `recorder` feature.
- Replay rendering (ghost hands, audio playback, Player controls) — follow-up `playback` feature.
- Pin authoring UI / world-space panels — follow-up `ui` feature.
- Spatial anchors, multi-user, backend sync, marketplace packaging — V2.

## Open questions

- Capture sample rate: **defaulted to 30 Hz**; revisit at 60 Hz if fast gestures look choppy,
  with a real recording size estimate.
- Hand pose: exact `OVRSkeleton` bone count + whether to store root pose separately — confirm
  against the live Meta API before locking field shapes.
- Pins as data (`pins[]` with `timeRange`) vs an event stream (`AnnotationShown/Hidden`) — MVP
  uses `pins[]`; revisit if Cues/attention-director (stretch) lands.

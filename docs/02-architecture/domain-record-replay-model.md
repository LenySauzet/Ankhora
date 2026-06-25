# Domain · Define the masterclass record/replay data model + local persistence

- Status: draft
- Owner: @LenySauzet
- Tracking issue: #25
- Last updated: 2026-06-25

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
  and *consume* the data; the contract itself is serialisable data, fully EditMode-tested.
- **Versioned**: `schemaVersion` at the top of the persisted unit; plan migration, never break
  old files silently.
- **Single monotonic clock**: one timeline in seconds per Chapter. Hands, voice and pins all
  read the *same* clock. Sampled at a fixed capture rate (decide: **30–60 Hz**), decoupled from
  frame rate. Replay interpolates between frames (`Sample(t)` = pure math, EditMode-tested).
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
  completed : bool                  # learner-side flag
  timeline : Timeline

Timeline
  durationSeconds : float
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
  payload : string                                 # text content, or image blob ref
  position : Pose
  timeRange? : { start, end }                       # when the Pin is visible
```

Persisted as a JSON manifest + blob files (audio, hand recording, images) under the app's
device storage. This local format is the same one a V2 backend will later sync.

## Acceptance criteria

- [ ] DTOs are plain C# (no `MonoBehaviour`) under `Assets/Scripts/Domain/`.
- [ ] `schemaVersion` present; a migration hook exists (even if a no-op for v1).
- [ ] EditMode: round-trip serialise → deserialise equality on a representative fixture.
- [ ] EditMode: `Timeline.Sample(t)` correct at frame boundaries and interpolated at midpoints.
- [ ] EditMode: an old-`schemaVersion` fixture migrates (or is rejected with a clear error).
- [ ] Serialiser sits behind an interface so the on-disk format can change in isolation.
- [ ] No allocation in the `Sample(t)` hot path (verify via Profiler when wired to replay).

## Out of scope

- Capture (writing real hand/voice data) — follow-up `recorder` feature.
- Replay rendering (ghost hands, audio playback, Player controls) — follow-up `playback` feature.
- Pin authoring UI / world-space panels — follow-up `ui` feature.
- Spatial anchors, multi-user, backend sync, marketplace packaging — V2.

## Open questions

- Capture sample rate: 30 Hz (smaller files) vs 60 Hz (smoother fast gestures)? Decide with a
  real recording size estimate.
- Hand pose: exact `OVRSkeleton` bone count + whether to store root pose separately — confirm
  against the live Meta API before locking field shapes.
- Pins as data (`pins[]` with `timeRange`) vs an event stream (`AnnotationShown/Hidden`) — MVP
  uses `pins[]`; revisit if Cues/attention-director (stretch) lands.

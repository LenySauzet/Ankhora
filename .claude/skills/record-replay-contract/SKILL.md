---
name: record-replay-contract
description: Use when defining, extending, or consuming Ankhora's core record/replay data model — the masterclass timeline of hand poses, head pose, events (annotation/anchor), and the audio track reference. The spine of the product; read this before touching capture or replay code. Triggers: record format, replay data, masterclass file, timeline, hand pose serialisation, data model, schema, frame format, what gets saved, serialize masterclass, DTO.
---

# Record / Replay data contract (Ankhora's spine)

Everything in Ankhora flows through one data model: a **Masterclass** is a time-ordered
recording that the learner replays. Get this contract right and capture, replay, audio,
anchors, and annotations all line up. This skill defines the canonical shape; other skills
(`spatial-anchors`, `voice-spatial-audio`, `world-space-annotations`) plug into it.

## Design principles

- **Plain C# DTOs, no `MonoBehaviour`.** The contract is serialisable data, fully
  EditMode-testable (see `unity-testability`). Unity components only *produce* and *consume* it.
- **Versioned.** Put a `schemaVersion` at the top. A masterclass recorded today must still
  replay after the format evolves — plan migration, never silently break old files.
- **Sampled timeline, single clock.** One monotonic timeline in seconds. Replay samples it;
  every subsystem (hands, head, audio, events) reads the *same* clock. Never two clocks.
- **Deterministic.** Same file → same replay. No wall-clock, no per-run randomness in the data.

## Canonical shape (illustrative — confirm bone counts against the live OVRSkeleton API)

```
Masterclass
  schemaVersion : int
  durationSeconds : float
  audio : { clipRef, timelineOffsetSeconds }        // see voice-spatial-audio
  anchors : [ { id, uuid } ]                          // see spatial-anchors
  frames : [ PoseFrame ]                              // sampled at a fixed rate

PoseFrame
  t : float                                           // seconds from start
  head : Pose                                          // position + rotation
  leftHand  : HandPose                                 // per-bone local rotations + root pose
  rightHand : HandPose
  events : [ Event ]                                   // sparse, attached to the frame at time t

Event (tagged union)
  AnnotationShown { annotationId, anchorId, text }     // see world-space-annotations
  AnnotationHidden { annotationId }
  StepAdvanced { stepIndex }
```

> Hand poses come from `OVRSkeleton`/`OVRHand`. **Confirm the exact bone set and pose
> representation via context7/Meta docs** before fixing field shapes — do not invent bone
> counts. Store local bone rotations + a root pose (compact, retargetable), not world transforms.

## Storage

- Serialise to JSON for readability during dev; consider a binary/compressed form later if
  files get large (a 2-min recording at a high sample rate is many frames). Keep the
  serialiser behind an interface so the format can change without touching capture/replay.
- A `ScriptableObject` wrapper is fine for *authoring/inspecting* in-Editor, but the
  source of truth is the serialisable DTO.

## Sampling & interpolation

- Capture at a fixed rate (decide it — e.g. 30–60 Hz) decoupled from frame rate.
- Replay interpolates between frames for smooth ghost hands at any display rate. Interpolation
  is pure math → EditMode-test it (`Sample(t)` between two known frames).

## Verify

- EditMode tests via the `unity-test-author` agent: round-trip serialise→deserialise equality;
  `Sample(t)` at boundaries and midpoints; schema-version migration of an old fixture.
- Replay a real captured file on device and confirm hands/audio/annotations stay in sync.

## Out of scope

Multi-user recordings, server-side storage, marketplace packaging — V2.

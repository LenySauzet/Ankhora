# MVP scope

> The concrete, time-boxed slice we commit to build. For the long-term picture see
> [`../00-vision.md`](../00-vision.md); for terminology see
> [`../06-glossary.md`](../06-glossary.md).
>
> *Last updated: 2026-05-31*

## Goal of the MVP

A **technical prototype** that proves the core concept and is **presentable at the next
follow-up**. Primary purpose: a portfolio / proof-of-competence demo — beautiful on
screen, readable, and tellable in ~2 minutes.

**Demo format: both** — a recorded video as a guaranteed fallback, plus a live attempt on
the Quest when the environment allows. The build must therefore work reliably, but a
canned capture is always the safety net.

The concept to prove end-to-end: **an expert captures a gesture once (voice + ghost
hands), and a learner replays it in 3D and learns from it.**

## The plan — three layers, one data model

We do not pick a single scope; we commit to a layered, risk-managed plan. The **data
model is identical across all three layers**, so nothing built is ever thrown away.

- **Floor (A) — Player-first.** If authoring proves too costly, fall back to a great
  **Learner Player** fed by a Masterclass the team pre-authored with a minimal internal
  tool. Guarantees we are never empty-handed for the demo.
- **Committed (B) — the minimal full loop.** Build **both** sides, each stripped to
  essentials: the Instructor records one chapter, the Learner replays it. This is the
  target — it proves the "capture once → replay" loop live.
- **Ceiling (C) — stretch backlog.** Pull in extra features **only if ahead of
  schedule**: Sketch, Video Pins, Cues, a 3rd chapter, polished menus, a QR-launch
  experiment.

## Committed scope (B)

The MVP is **VR**, with a Learner-side **Passthrough** toggle (MR-lite). Target: **one
Masterclass, 1–2 Chapters.**

**Instructor (minimal authoring, in-headset)**

- Create a Masterclass locally (title only).
- Add a Chapter; optionally attach a Model from a **bundled library** (no import).
- **Record one take**: Voice Track + Hands Track captured together.
- Place **Text Pins** and **Image Pins** in space.
- Save to local device storage.

**Learner (the Player)**

- Pick a Masterclass from a local list; see its Chapters and Completion state.
- Enter a Chapter: Model loads in front (if any).
- **Hands + Voice** replay in sync; **Pins** appear on their time range.
- **Player controls**: play/pause, scrub the Timeline, rewind, **slow-motion**, **loop**.
- **Recenter** the Stage; toggle **Passthrough** any time.
- **Mark as complete** → next Chapter.

**Platform**

- Quest 3, VR + Passthrough toggle.
- Local persistence: JSON manifest + binary blobs (audio, hand poses, images); models
  bundled in the build. **No backend, no network, no accounts.**

## Stretch backlog (C — only if ahead)

Sketch (3D drawing) · Video Pins · Cues (attention director) · a 3rd Chapter · nicer
overview/menus · QR-launch experiment (scan → open a local Masterclass).

## Out of scope (V2+)

Auth · Organizations · Marketplace · web companion + backend · custom model import ·
Room Stage / Anchored Stage / real spatial anchors · instructor analytics · AI/RAG
assistant · subtitle auto-translation · active pose validation · certificates ·
multi-user / co-located sessions.

Any request landing here triggers an explicit re-scoping discussion before
implementation.

## Data model (local, MVP)

```text
Masterclass
  id, title
  chapters[]            # ordered
Chapter
  id, title, order
  modelRef?             # reference to a bundled model, optional
  timeline
    voiceTrack?         # audio blob + timing
    handsTrack?         # hand poses per frame (binary), + timing
    pins[]              # { type: text|image, payload, position, timeRange? }
  completed             # learner-side flag
```

Persisted under the app's device storage as a manifest (JSON) + blob files (audio, hand
recording, images). This local format is the same one a V2 backend will sync.

## Technical baseline

- Unity `2022.3.62f3` LTS, URP `14.0.12`.
- Meta XR SDK (All-in-One); Building Blocks + Project Setup Tool preferred over manual
  rigging; hand-tracking interactions through Meta's Interaction SDK.
- Quest 3, IL2CPP / ARM64 / Vulkan; distribution = side-loaded APK.

### Known constraints (context, not task allocation)

- Hand tracking cannot be iterated inside Unity Editor Play Mode on macOS (Quest Link is
  Windows-only); it is exercised via Meta XR Simulator or build-and-run on device.
- The riskiest piece is the **Hands record → replay pipeline** (capturing XR Hands joint
  poses per frame and replaying them as ghost hands).

> Work allocation across the team is intentionally **not** specified here — it will be
> decided with the team later.

## Success criteria

The demo must show, end-to-end:

1. An Instructor records a short Chapter (voice + ghost hands) on a Model.
2. At least one Text Pin and one Image Pin placed in space.
3. A Learner replays it: synced ghost hands + voice, Pins visible, with slow-motion and
   loop usable on the gesture.
4. Passthrough toggled on/off live.
5. Mark as complete advancing the Masterclass.

A recorded video covering the above is the guaranteed deliverable; the live run is the
bonus.

## Open points to revisit

- Minor adaptations to the flows/lexicon as we build (expected).
- Which bundled 3D-model source/set to ship (e.g. CC0 from Poly Haven / Quaternius /
  Kenney, or CC-filtered Sketchfab).
- Team work allocation.

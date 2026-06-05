# Milestones — Ankhora MVP

> Outcome-based checkpoints for the committed MVP. Each milestone has a **demo-able**
> success criterion — something you can show on the Quest (or in the fallback video).
> For the per-feature build order see
> [`01-product/feature-roadmap.md`](01-product/feature-roadmap.md); for the committed
> scope see [`01-product/mvp-scope.md`](01-product/mvp-scope.md).
>
> *Last updated: 2026-06-05*

## How to read this

- Milestones group the feature slices of the [feature roadmap](01-product/feature-roadmap.md)
  into demo-able checkpoints. A milestone is **done** only when its success criterion is
  reproducible on device.
- The **layer** column maps to the layered MVP plan
  ([mvp-scope §The plan](01-product/mvp-scope.md#the-plan--three-layers-one-data-model)):
  **A — Floor** (Player-first fallback), **B — Committed** (the minimal full loop),
  **C — Ceiling** (stretch, only if ahead).
- Timeline is **~10–14 effective working days** (Thu/Fri, 3 devs). Milestones are ordered
  by dependency and **risk-first** — the riskiest piece (hands record → replay) is M1, not
  deferred to the end.

## The arc

| # | Milestone | Demo-able success criterion | Layer |
|---|---|---|---|
| **M0** | **Foundation XR shell** | The URP scaffold builds to a Quest 3 APK, shows the user's tracked hands, and toggles Passthrough on/off. Headless build script + `adb install` loop works. | B (enabler) |
| **M1** | **Record/replay spine** | An expert records **one take** (Voice + Hands together); it serialises to the local manifest + blobs; a Learner replays it as **ghost hands + voice in sync**. | B (core, riskiest) |
| **M2** | **Player** | Learner controls the replay: play/pause, **scrub** the Timeline, rewind, **slow-motion**, **loop**, **Recenter**, Passthrough toggle, **Mark as complete** → next Chapter. | B |
| **M3** | **Pins** | At least one **Text Pin** and one **Image Pin** are placed in space by the Instructor and appear for the Learner on their time range. | B |
| **M4** | **Masterclass shell** | A Learner picks a Masterclass from a **local list**, sees its **Chapters + Completion** state, enters one; a minimal in-headset authoring menu creates a Masterclass/Chapter and saves locally. | B |
| **M5** | **Demo** | A **recorded video** covering the full success criteria exists (guaranteed deliverable); a **live run** on the Quest works (bonus). Stretch (C) items pulled in only if ahead. | B → close |

## Milestone detail

### M0 — Foundation XR shell
The base every later feature stands on. Fully specified in the implementation plan
[`docs/superpowers/plans/2026-05-31-ankhora-foundation-xr-shell.md`](superpowers/plans/2026-05-31-ankhora-foundation-xr-shell.md):
Android platform switch, XR packages, Quest Player Settings, Camera Rig + Hand Tracking
Building Blocks, Passthrough block, a testable `PassthroughController`, Meta XR Simulator,
and a headless build script. **Done when** the APK installs and the hands + Passthrough
toggle work on device.

### M1 — Record/replay spine
The heart of Ankhora and the **single biggest technical risk** (capturing XR Hands joint
poses per frame, serialising them, and replaying them as ghost hands in sync with voice).
Built spine-first: the [record/replay data contract](01-product/feature-roadmap.md) lands
as plain C# (EditMode-tested) before any capture code. **Done when** a take recorded by one
run replays identically in another.

### M2 — Player
Turns a replayable Chapter into a controllable learning surface. All controls drive the
single **Timeline** abstraction. Passthrough toggle and Mark-as-complete live here.
**Done when** a Learner can slow down and loop a gesture and advance the Masterclass.

### M3 — Pins
Adds space-placed media (Text + Image) with an optional time range. **Done when** pins
authored by the Instructor render for the Learner at the right time and place.

### M4 — Masterclass shell
The thin authoring + navigation layer that makes the local list of Masterclasses real:
overview, chapter list, completion, and a minimal create/record/save menu in-headset.
**Done when** a Masterclass can be authored and re-opened entirely on device, no editor.

### M5 — Demo
Capture the guaranteed fallback video and rehearse the live run. Pull in stretch (C)
features — Sketch, Video Pins, Cues, a 3rd Chapter, nicer menus, QR-launch — **only** if
ahead of schedule. **Done when** the video exists and the live demo is rehearsed.

## Fallback (Layer A) — if authoring slips

If in-headset authoring (M4 / the Instructor side of M1) proves too costly, fall back to a
**Player-first** demo: the team pre-authors a Masterclass with a minimal internal tool and
ships a great **Learner Player** (M2 + M3 replay). The data model is identical across
layers, so nothing built for B is thrown away. This guarantees we are never empty-handed
for the demo.

## Open points

- Exact day-level allocation across the 3 devs — decided with the team, not here.
- Which bundled 3D-model set ships (CC0 source — Poly Haven / Quaternius / Kenney, or
  CC-filtered Sketchfab).
- Whether M3 (Pins) or M2 (Player) is the better cut line if time runs short — revisit
  after M1 lands and the real velocity is known.

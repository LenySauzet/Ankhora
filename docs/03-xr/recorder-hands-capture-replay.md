# Recorder · Capture hand joints to a JSON timeline and replay them as ghost hands

- Status: draft
- Owner: @LenySauzet
- Tracking issue: #30
- Last updated: 2026-06-26

## Why

Roadmap **S3** — the riskiest, most foundational slice of Ankhora (per
[`docs/01-product/mvp-scope.md`](../01-product/mvp-scope.md), which names the *Hands record →
replay pipeline* as the riskiest piece): capturing XR hand joint poses per frame and replaying
them as ghost hands. The record/replay data model (the "spine") already exists from #26
(`Masterclass → Chapter → Timeline → PoseFrame → HandPose`, `JsonMasterclassSerializer`,
`TimelineSampler.SampleHead`); this slice is the first to **produce** and **consume** it
end-to-end. Proving capture → JSON → replay de-risks every later slice (voice, pins, Player
controls).

## What

- **Instructor:** presses a controller button (A/X) to start recording; their tracked hands +
  head are sampled while recording; presses again to stop. The take is written to device storage
  as a JSON masterclass file.
- **Learner:** presses a button to load that file and replay it; the recorded hands appear as
  translucent **ghost hands** animating in place, smoothly interpolated. Replays once (loop
  optional).
- Scope is **hands-only**: no voice, no pins, no Player scrub/slow-motion UI (later slices).

## How

Mirrors the passthrough feature's architecture (pure logic + interface seam + thin OVR adapter)
to maximise EditMode coverage — essential because hand-tracking cannot be iterated in Editor Play
Mode on macOS (Quest Link is Windows-only; see `@CLAUDE.md`).

**Domain (`Ankhora.Domain`, pure, EditMode-tested):**

- `Recording/TimelineRecorder` — fixed-rate accumulator. `Begin(startTime)` /
  `Push(now, head, leftHand, rightHand)` (emits a `PoseFrame` every `1/30 s` on one monotonic
  clock; `t = now − start`) / `Finish(endTime)` sets `durationSeconds`. Pure timing logic.
- `Sampling/TimelineSampler.SampleHand(timeline, t, rightHand, Quaternion[] into, out Pose root, out bool tracked)`
  — alloc-free extension of the existing sampler (the TODO already noted in its remarks); shares
  the binary-search bracket with `SampleHead`, slerps each bone rotation into the
  **caller-owned** array.

**Foundation (`Ankhora.Foundation`, MonoBehaviour, device, refs `Oculus.VR`):**

- `IHandPoseSource` (seam) — `TryGetHead(out Pose)` / `TryGetHand(rightHand, ref HandPose)`. A
  `SimulatedHandPoseSource` driving synthetic motion lets the record → replay loop be
  smoke-tested headless.
- `OvrHandPoseSource : MonoBehaviour, IHandPoseSource` — wraps two `OVRSkeleton` (left/right) +
  the centre-eye transform; reads per-bone local rotations + wrist root + tracking confidence
  into reused arrays. **Exact `OVRSkeleton` bone enumeration and API confirmed via context7 /
  Meta docs at implementation** (anti-hallucination; `@CLAUDE.md`). Meta `OVRSkeleton` exposes 19
  skinnable bones (`Hand_WristRoot`..`Hand_Pinky3`).
- `MasterclassRecorderController : MonoBehaviour` — button A/X toggles record; pushes samples with
  `Time.unscaledTime`; on stop, serialises (existing `JsonMasterclassSerializer`) and writes to
  `Application.persistentDataPath`.
- `GhostHandPlayer : MonoBehaviour` — button toggles load + play; advances a playback clock
  `0 → duration`; samples head + both hands via `TimelineSampler`; drives an `IHandView`.
- `IHandView` (seam) + `MetaGhostHandView` (a duplicated Meta hand rig in replay mode, captured
  rotations applied, rendered with the translucent fresnel ghost shader — see the
  `urp-shadergraph` skill) and `DebugJointsHandView` (spheres at joints, diagnostic behind the
  same seam).

**Scene (`MainVrScene`):** the existing `OVRCameraRig` supplies centre-eye + the live
`OVRSkeleton`s (instructor hands); add the recorder controller, ghost player, and two ghost hand
rigs (hidden until replay).

Meta APIs to confirm at build: `OVRSkeleton` (bones, `GetCurrentStartBoneId`, bone local
rotations), `OVRHand` / `OVRSkeleton.IsDataHighConfidence`. Docs:
<https://developers.meta.com/horizon/documentation/unity/unity-handtracking/>.

## Data model

**No schema change.** S3 *uses* the existing model from #26:

```
Masterclass → Chapter → Timeline → PoseFrame { t, head, leftHand, rightHand }
                                    HandPose  { root, boneRotations[19] }
```

The slice is the first to populate `boneRotations` (19 entries/hand) with real capture data and
round-trip it through `JsonMasterclassSerializer`. The `Quaternion[]` IL2CPP round-trip already
has a guard test from #26; this slice exercises it on device with real data.

## Acceptance criteria

- [ ] EditMode: `TimelineRecorder` emits frames at the fixed rate, monotonic `t`, correct
      `durationSeconds`.
- [ ] EditMode: `TimelineSampler.SampleHand` returns the exact frame at frame times, slerped
      midpoints, clamped out-of-range, propagates not-tracked, and writes into a caller-owned
      array with **no allocation**.
- [ ] EditMode: serialise → deserialise round-trip of a captured-shaped `Timeline`
      (19 bones × 2 hands) is equal, incl. `boneRotations` reaching the JSON wire.
- [ ] Device: record a hand gesture, stop, replay → ghost hands match the recorded motion; the
      JSON file is present in `persistentDataPath`.
- [ ] Runs at 90 FPS on Quest 3 with passthrough enabled.
- [ ] No allocation in the replay hot loop (verify via Profiler; the sampler writes into reused
      arrays).

## Out of scope

Voice track, spatial pins, Player controls UI (scrub / slow-motion / loop / recenter), spatial
anchors, multi-chapter, a file/masterclass browser, web companion. Each is a later slice (see
`mvp-scope.md`).

## Open questions

- Ghost-hand rig source: instantiate a second `OVRSkeleton`-driven rig vs a static skinned hand
  mesh whose bones we drive directly — decide at implementation after confirming the Meta
  hand-mesh prefab API.
- Sample rate fixed at 30 Hz: revisit if fast gestures look steppy on device (interpolation
  should cover it).

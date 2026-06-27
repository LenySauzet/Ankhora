# Hands Consolidation — Design

- **Status:** Approved (brainstorming) — pending implementation plan
- **Date:** 2026-06-27
- **Author:** Lény Sauzet (Claude Code pairing)
- **Builds on:** S3 record/replay spine (PR #31), [ADR-0004](../../02-architecture/adr/0004-domain-foundation-two-assembly-split.md)
- **Related skills:** `new-xr-interaction`, `record-replay-contract`, `urp-shadergraph`, `unity-testability`

## Goal

Turn the bring-up scaffolding of the hands record/replay loop into a clean, production-ready
slice: a **pinch-triggered take** (interim trigger; the real one will come from the product UI
later — deferred and agreed), **live tracked hands** rendered while recording, and a **skinned
translucent ghost-hand mesh** for replay in place of the debug joint spheres.

## Context

After S3, the loop works end to end but through transitional pieces:

- **Trigger:** `FirstLightAutoCapture` + `AutoCaptureClock` run a fixed *countdown → record N s → save*
  schedule with no user input (a buttonless bring-up harness).
- **Recording rig:** the scene carries **hand-rolled** `OVRHand` + `OVRSkeleton` GameObjects (the
  origin of the past `HandType = -1` / 0-bone bug). They feed capture only — **no live hand is
  rendered** while recording.
- **Ghost:** `FkGhostHandView` draws one sphere per joint (validated debug visual).

The stable spine stays: `RecordingSession` (shared capture core — drives `TimelineRecorder`,
captures the per-hand `HandSkeleton` once, persists via `MasterclassStore`), the
`IHandPoseSource` / `IHandSkeletonSource` capture seams, `GhostHandPlayer` + the `IHandView`
replay seam, and the `Timeline`/`HandSkeleton`/`HandPose` domain model. This slice changes only
the **trigger**, the **rig source**, and the **ghost renderer** — never the contract between them.

## Decisions (agreed)

1. **Trigger = pinch toggle + countdown.** Non-dominant index pinch *arms* a take → fixed 3-2-1
   countdown → recording starts → a second pinch *stops & saves*. The countdown keeps the arming
   gesture out of the recorded window; stop is event-driven (not a timer).
2. **Live hands = recording only.** The expert sees their live hands while recording (required in
   VR). Replay shows the ghost only; the learner's own hands belong to the later Player/Passthrough
   slice (in MR they already see their real hands through passthrough).
3. **Ghost = skinned Meta mesh, driven by our bone rig (approach A), with the FK view kept as a
   bring-up fallback** behind the same `IHandView` seam. The mesh becomes the default; the joint
   spheres are deleted once the mesh is verified on device.
4. **One Meta-provided rig, no hand-rolled skeleton.** The Meta hand-tracking Building Block
   (`OVRHandPrefab` L/R) is the single source for both live-hand rendering and capture. This is the
   "consolidation": one correctly-configured rig instead of the fragile hand-rolled one.

## Architecture

Follows ADR-0004: pure logic in `Ankhora.Domain` (EditMode-tested), device/OVR code in
`Ankhora.Foundation`, cross-feature wiring via a composition root in `Foundation/App`.

### New — Domain (pure, EditMode-tested)

- **`Domain/Recording/RecordingCountdown.cs`** — pure countdown gate. Given an arm time and a
  countdown duration, reports the phase (`Counting` / `Live`) and the integer seconds remaining.
  Replaces `AutoCaptureClock` (whose fixed record-duration no longer fits an event-stopped take).
- **`Domain/Recording/PinchEdgeDetector.cs`** — pure rising-edge detector over a boolean pinch
  signal (debounced), so the toggle logic is testable without a headset. Returns whether *this*
  sample is a fresh pinch-down.

### New — Foundation (device layer)

- **`Foundation/Recording/PinchRecordingTrigger.cs`** (`MonoBehaviour`, replaces
  `FirstLightAutoCapture`) — reads the non-dominant `OVRHand.GetFingerIsPinching(HandFinger.Index)`,
  feeds it through `PinchEdgeDetector`, and runs the state machine
  `Idle → CountingDown → Recording → (save) → Idle` using `RecordingCountdown` for the lead-in and
  `RecordingSession` for capture. Exposes `UnityEvent OnRecordingSaved` (reused wiring) and an
  optional `UnityEvent<int>` for the countdown value (a hook for future visual feedback; logs for now).
- **`Foundation/Replay/SkinnedGhostHandView.cs`** (`IHandView`, approach A) — `Bind(HandSkeleton)`
  builds the parented bone-transform hierarchy from the captured skeleton (reusing the rig-build
  logic proven in `FkGhostHandView`) and attaches a `SkinnedMeshRenderer` wired to the Meta hand
  mesh (`OVRHand_L/R` geometry + bind poses) with the ghost material and the built bones in skeleton
  order; `Apply(root, boneRotations, count)` sets the wrist pose and per-bone local rotations (Unity
  skinning follows); `Show(bool)` toggles visibility.

### New — Assets

- **`Art/Shaders/GhostHands_URP.shadergraph`** + a `M_GhostHands` material — URP, **Unlit**,
  transparent (alpha ≈ 0.3), soft Fresnel rim, subtle emissive tint, single-pass-instanced safe
  (per `urp-shadergraph`: mobile-VR cost rules, no scene-color/refraction).

### Modified

- **`Foundation/App/FirstLightReplayLink.cs` → `RecordReplayLink.cs`** — same composition-root role,
  renamed off the "FirstLight" scaffolding name; wires `PinchRecordingTrigger.OnRecordingSaved` →
  `GhostHandPlayer.LoadAndPlay`.
- **`Assets/Scenes/MainVrScene.unity`** — replace the hand-rolled `OVRHand`/`OVRSkeleton` objects
  with the Meta hand-tracking Building Block (`OVRHandPrefab` L/R) under the camera rig; point
  `OvrHandPoseSource`'s skeleton references and `PinchRecordingTrigger`'s `OVRHand` reference at that
  rig; swap the ghost views to `SkinnedGhostHandView` (FK kept wired as the fallback option).

### Unchanged

`RecordingSession`, `OvrHandPoseSource`, `IHandPoseSource`, `IHandSkeletonSource`,
`GhostHandPlayer`, `IHandView`, `TimelineSampler`, and the whole `Domain/Model` + persistence layer.

### Deleted (sequenced)

- `FirstLightAutoCapture.cs` + `AutoCaptureClock.cs` (+ its EditMode tests) — replaced by
  `PinchRecordingTrigger` + `RecordingCountdown`.
- `FkGhostHandView.cs` — **only after** the skinned mesh is verified on device; kept as the
  fallback during bring-up.

## Data flow

```
RECORD
  non-dom OVRHand pinch ─▶ PinchRecordingTrigger (PinchEdgeDetector)
     ▶ CountingDown (RecordingCountdown)  ▶ RecordingSession.Begin
     ▶ per-frame RecordingSession.Tick  (OvrHandPoseSource reads the Meta rig's
        OVRSkeleton → HandPose per hand; HandSkeleton captured once)
     ▶ second pinch ▶ RecordingSession.SaveTo (MasterclassStore JSON) ▶ OnRecordingSaved
REPLAY
  OnRecordingSaved ─▶ GhostHandPlayer.LoadAndPlay
     ▶ TimelineSampler.SampleHand per hand
     ▶ SkinnedGhostHandView.Apply(root, boneRotations) ▶ Meta skinned mesh deforms
```

## Error handling

- **Hand not tracked / low confidence:** capture already gates on tracking (`OvrHandPoseSource`);
  the trigger ignores pinch input from an untracked hand (no false arming).
- **Missing references** (rig, pose source, store, hand): components log a clear error in `Awake`
  and no-op, as the existing scaffolding does.
- **Bone-count mismatch** between the captured skeleton and the ghost rig: `Bind` sizes everything
  from the captured `HandSkeleton`; `Apply` treats `boneRotations` length as count-agnostic
  (per the OpenXR 26-joint rule in `CLAUDE.md`).
- **Save failure:** `RecordingSession.SaveTo` already returns an error string; the trigger surfaces
  it (log) and returns to `Idle` without emitting `OnRecordingSaved`.

## Testing strategy

- **EditMode (Mac, headless):** `RecordingCountdown` (phase transitions, remaining seconds,
  boundaries) and `PinchEdgeDetector` (rising-edge only, debounce, no double-fire) — TDD. The
  existing `TimelineSampler*` and `HandSkeleton*` tests must stay green.
- **Device (Quest 3 — mandatory; Mac Editor cannot render hand tracking):** pinch → 3-2-1 →
  record → pinch → the translucent ghost mesh replays the correct pose with correct left/right
  hands; live hands visible while recording; A/B against the FK fallback. Verify bone parity
  (26-joint OpenXR) and that the Meta mesh binds to our transforms. Windows teammates can iterate
  via Quest Link; the Mac station verifies via Build & Run.

## Risks & mitigations

1. **Binding the Meta hand mesh to our bone transforms** (bind poses, weights, bone order) is the
   delicate part. *Mitigation:* capture and ghost derive from the **same** OVR skeleton, so order
   and count match by construction; the FK fallback stays behind the seam; if mesh binding proves
   too fiddly, ship the slice on FK and revisit the mesh separately.
2. **OpenXR 26-joint skeleton vs the Meta mesh rig.** *Mitigation:* device verification; the
   `IHandView`/`HandSkeleton` contract is count-agnostic.
3. **No hand tracking in Mac Editor Play Mode.** *Mitigation:* all logic that can be pure is pure
   and EditMode-tested; the MonoBehaviour layer is verified on device.

## Out of scope (explicit)

Voice track, annotations/pins, Player controls (scrub / slow-mo / loop / recenter / passthrough
toggle), the learner's own hands during replay, and the real product-UI record trigger. The pinch
trigger is a deliberate interim; the UI trigger is a later platform/product slice.

## Links

- ADR: [0004 — Domain + Foundation split](../../02-architecture/adr/0004-domain-foundation-two-assembly-split.md)
- Code map: [`Assets/Scripts/CONTEXT.md`](../../../Assets/Scripts/CONTEXT.md)
- MVP scope: [`docs/01-product/mvp-scope.md`](../../01-product/mvp-scope.md)

## Addendum (2026-06-27, device-driven correction)

On-device, the skinned ghost replayed as an **exploded mesh** — the materialised Risk 1.
Root cause: `SkinnedGhostHandView` bound the `OVRHand_*.fbx` mesh, whose vertex bone indices
follow the FBX armature's own order and have **no relation to `OVRPlugin.BoneId`**. The real
Meta hand mesh is fetched from the headset at runtime by **`OVRMesh`** (`OVRPlugin.GetMesh`);
the FBX is an unused reference asset.

**Corrected approach (supersedes "Approach A / bind the FBX mesh"):** `SkinnedGhostHandView`
now replicates `OVRMeshRenderer.Initialize()` exactly — it takes a copy of the live hand's
`OVRMesh.Mesh`, recomputes `bindposes` from the rest rig
(`bone.worldToLocal * meshRoot.localToWorld * OpenXR-180°Y-fixup`), and skins to our bone
array in `BoneId` order (the captured order already is). The serialized field changed from
`_handMesh` (`Mesh`) to **`_ovrMesh` (`OVRMesh`)**, referencing the matching live hand's
`OVRMesh` (left ghost ← left hand). The FK fallback (`FkGhostHandView`) stays behind the seam.

**Ghost look (new requirement):** live hands render as a **blue** ghost (`M_GhostHands_Blue`)
during recording; the replay ghost renders **yellow** (`M_GhostHands_Yellow`). Both use the
existing `GhostHands_URP` Fresnel shader. The live `OVRHandPrefab` hands' material is swapped
to the blue ghost. (Replaces the single cyan `M_GhostHands`.)

Still device-pending: confirm the corrected mesh skins correctly + the blue/yellow read.

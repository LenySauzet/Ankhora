# ADR-0005: Adopt Meta Building Blocks as a curated set; replace the hand-rolled hand rig

- **Status:** Accepted
- **Date:** 2026-06-29
- **Deciders:** Lény Sauzet
- **Tags:** xr, hand-tracking, meta-sdk, scene, tooling

## Context and problem

`CLAUDE.md` (§ *Conventions*) and the dossier flag **"prefer Meta Building Blocks +
Project Setup Tool over manual rigging"** as the highest-leverage efficiency move, and the
hands-consolidation slice spec explicitly called for the Meta **hand-tracking Building
Block**. During bring-up, however, we ended up with a **hand-rolled** `OVRHand` /
`OVRSkeleton` / `OVRMesh` rig (`LeftHand` / `RightHand` GameObjects configured by hand).
That rig is now green and device-verified (record → ghost replay, PR #33) — but it deviates
from both the spec and the convention, and the deviation is exactly what caused the
`HandType = -1 → 0 bones` debug session (a misconfigured default a block would have set
correctly).

Before swapping a working, device-verified pipeline, we did a deliberate, full-catalogue
analysis of Meta's ~30 Building Blocks (Meta XR SDK All-in-One `201.0.0`) against the MVP
scope and the V2 vision. The full analysis lives at
[`research/meta-building-blocks-analysis.md`](../../../research/meta-building-blocks-analysis.md).

The load-bearing finding: **a Building Block is an editor-time installer, not a runtime
dependency.** It instantiates *standard* OVR / Interaction-SDK objects (`OVRCameraRig`,
`OVRHandPrefab`, `OVRInteraction`), pulls in dependency blocks, and flips the right project
config / manifest flags — then we own ordinary GameObjects with no proprietary wrapper at
runtime. Critically, the `Hand Tracking` block ships the `OVRHandPrefab`, whose components
are exactly the `OVRSkeleton` (joint poses) + `OVRMesh` / `OVRMeshRenderer` (runtime hand
mesh) that `OvrHandPoseSource` reads and `SkinnedGhostHandView` skins. The Core hand path is
**not** removed by the Interaction SDK; both run at once. The only real risk (OVR-vs-OpenXR
skeleton incompatibility) is already neutralised — Ankhora runs the OpenXR 26-joint skeleton
and binds the *runtime* mesh, not a baked FBX. So adopting blocks is a low-risk re-wiring,
not a rewrite.

## Decision drivers

- 3-person team, ~10–14 effective coding days — hand-rolling Meta rigs is pure downside.
- Hand-tracking interactions **must** go through Meta's Interaction SDK (Horizon Store
  requirement); the Interactions Rig is the Store-compliant substrate.
- Must **not** regress the green, device-verified PR #33 pipeline (Mac Editor can't render
  hand tracking — every hand change is device-verified).
- YAGNI: adopt only the blocks that map to MVP success criteria; defer the rest.

## Considered options

1. **Keep the hand-rolled rig** — works today, but off-spec, off-convention, fragile
   defaults, and we re-implement by hand what Meta maintains.
2. **Adopt the entire Building Blocks catalogue** — most of it (Anchors, Scene, Multiplayer,
   Avatars, AI, Movement, Eye Gaze) is V2 or inert on Quest 3; scope creep.
3. **Adopt a curated Building Block set** — the few blocks that map to the MVP, replacing the
   hand-rolled rig with the `Hand Tracking` block; defer the rest to V2; ignore what Quest 3
   hardware / MVP scope makes irrelevant.

## Decision

We chose **Option 3: adopt Building Blocks as a curated set, and replace the hand-rolled
hand rig with the `Hand Tracking` block.**

The single most important reason: blocks emit the exact standard `OVRSkeleton` + `OVRMesh`
objects our record/replay pipeline already depends on, so we get Meta's correct, maintained
rig with no runtime lock-in and no pipeline loss — for free, on a tight timeline.

**The Ankhora block set:**

- **Adopt now (MVP):** Camera Rig · Hand Tracking · Passthrough · Interactions Rig + Poke
  Interaction · Spatial Audio · Controller Buttons Mapper.
- **Stretch (only if ahead):** Ray Interaction · Controller Tracking · Grab / Distance Grab.
- **Defer to V2:** Spatial Anchor Core + Colocation/Shared Anchors · Scene blocks (Room
  Stage) · Occlusion · Multiplayer suite + Networked Avatar · AI (LLM/STT/TTS) · Passthrough
  Camera Access.
- **Ignore on this project:** Eye Gaze (no Quest 3 eye-tracking hardware) ·
  Movement/Character Retargeter · Haptics (no hand-tracking haptics) · Platform
  Init/Entitlement (no MVP accounts).

**Sequencing:** do **not** redo the swap inside PR #33 — it works, ship it. Adopt the blocks
as the **first task of the Pins slice**, where the Interactions Rig + Poke earn their keep
(the annotation panel needs ISDK for Store compliance) and re-verifying hands is natural.

## Consequences

- **Positive:** on-spec, on-convention rig with correct Meta defaults; Store-compliant
  interaction substrate ready for the Pin panels; record/replay pipeline preserved; no
  proprietary runtime wrapper; the adopted set maps 1:1 to MVP success criteria.
- **Negative / accepted trade-offs:**
  - The hand-rig swap is a **device-verified** step (Mac Editor can't validate it), so it
    costs one on-device re-verify cycle.
  - Brief divergence: PR #33 ships the hand-rolled rig; the block migration lands in the next
    slice (tracked, not silent).
- **Follow-ups:**
  - Migration plan (see analysis § 6): `meta_get_config_information` → add Camera Rig + Hand
    Tracking → re-point `OvrHandPoseSource._leftSkeleton/_rightSkeleton` and
    `SkinnedGhostHandView._ovrMesh` at the block's `OVRHandPrefab` → add Interactions Rig +
    Poke when the panel lands → re-verify record→replay on device → delete the hand-rolled
    `LeftHand`/`RightHand` rig only **after** the re-verify is green.
  - Open the migration as the first task of the Pins slice.

## Links

- Source / analysis: [`research/meta-building-blocks-analysis.md`](../../../research/meta-building-blocks-analysis.md)
- Related: [ADR-0001](0001-unity6-migration.md) (Unity 6 / Meta SDK 201 baseline),
  [ADR-0002](0002-no-mrtk-graphics-tools.md) (URP Shader Graph, Meta Interaction SDK for input),
  [ADR-0004](0004-domain-foundation-two-assembly-split.md) (Domain/Foundation split the rig sits in)
- Convention: `CLAUDE.md` § *Conventions* (Meta Building Blocks preferred over manual rigging),
  skill `.claude/skills/new-xr-interaction/SKILL.md`
- PR #33 — the hand-rolled rig this supersedes (record→ghost replay, device-verified)

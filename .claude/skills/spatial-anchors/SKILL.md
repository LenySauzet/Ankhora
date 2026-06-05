---
name: spatial-anchors
description: Use when persisting or restoring a position in the user's real space in Ankhora — placing, saving, loading, or erasing a Meta Spatial Anchor so an annotation or object stays put across a session. Triggers: spatial anchor, OVRSpatialAnchor, persist position, anchor an annotation, world-locked, save anchor, load anchor, erase anchor, anchor in real space, where did the user place it.
---

# Spatial Anchors (Meta)

Spatial anchors are the **"spatial" in Ankhora's masterclass** — they let an expert
drop an annotation in real space and have the learner find it in the same physical spot.
This is code (no `meta_*` Building Block tool exists for anchors), built on
`OVRSpatialAnchor`.

> **Verify the API first.** The `OVRSpatialAnchor` save/load/erase surface changed across
> Meta XR SDK versions (older `Save/Erase` callbacks vs newer `SaveAnchorAsync` /
> `LoadUnboundAnchorsAsync` async APIs). The project is on `com.meta.xr.sdk.all` 201.0.0 —
> confirm the exact signatures via context7 (`mcp__plugin_context7_context7__*`) or the Meta
> docs before writing code. Do not guess method names.

## Config / permissions (do this before code)

1. `meta_get_config_information` — confirm **Anchor Support** is enabled in `OculusProjectConfig`.
2. The Android manifest needs the anchor permission (`com.oculus.permission.USE_ANCHOR_API`
   / scene permission as required by the SDK version). Use `meta_update_android_manifest`
   and verify against the config — do not hand-edit the manifest blindly.

## Approach (keep it MVP-small)

- **Place:** instantiate an `OVRSpatialAnchor` at the target pose (e.g. where the expert
  pinched). One anchor per annotation for the MVP.
- **Persist within session:** save the anchor and keep its `Guid`/`UUID` in the
  record/replay data (see the `record-replay-contract` skill) so replay can re-bind to it.
- **Restore:** on replay, load the unbound anchor by UUID, localize it, then attach the
  annotation transform to it.
- **Erase:** clean up anchors you created when a masterclass is deleted.

Keep anchor *logic* (which UUID maps to which annotation, lifecycle) in plain C# so it is
EditMode-testable (see `unity-testability`); only the `OVRSpatialAnchor` calls touch Unity.

## Verify (never claim done without this)

- On device or Meta XR Simulator: place → save → reload the scene → load → confirm the
  annotation reappears in the same physical spot. Editor Play Mode on Mac cannot do this
  (no hand tracking / no runtime localization) — say so if you could only static-check.
- `Unity_ReadConsole`: no anchor API errors.

## Out of scope (V2 — `@CLAUDE.md` § *Out of scope*)

Cross-session anchor persistence at scale, multi-room, shared/colocated anchors, cloud
anchors. The MVP only needs single-session, single-room anchors. Flag a re-scoping
discussion if a request needs more.

---
name: passthrough-mr
description: Use when setting up or tuning Mixed Reality in Ankhora — enabling Meta Passthrough so the learner sees their real room with virtual content composited in, configuring underlay/overlay, depth/occlusion, or deciding MR-vs-VR for a scene. Triggers: passthrough, mixed reality, MR, see-through, OVRPassthroughLayer, composite, occlusion, depth, real room, see the real world, environment.
---

# Passthrough / Mixed Reality (Meta)

Ankhora is **MR training**, not VR: the learner stands in their real workshop/kitchen and
sees virtual ghost hands + annotations composited over the real world. That requires
**Passthrough**, built on `OVRPassthroughLayer` and passthrough enabled in `OVRManager`.

> **Verify the API/config first** via `meta_get_config_information` and context7/Meta docs —
> passthrough support flags and the `OVRPassthroughLayer` surface depend on the SDK version
> (`com.meta.xr.sdk.all` 201.0.0). Don't guess field names.

## Setup order

1. **`meta_get_config_information`** — confirm **Passthrough Support** is enabled in the
   project config; check the camera rig's clear flags.
2. Add/enable an **`OVRPassthroughLayer`** (an *underlay* — virtual content renders on top
   of the real-world feed). Set the camera background to transparent (clear color alpha 0)
   so the real world shows through.
3. Manifest: passthrough capability via `meta_update_android_manifest` if the SDK version
   requires it; verify against the config.

## Key decisions for Ankhora

- **Underlay vs overlay:** annotations and ghost hands are virtual content over reality →
  underlay passthrough, content on top. Overlay only for deliberate "tint the world" effects.
- **Occlusion / depth:** if virtual content must hide behind real objects (a tool occluding
  a ghost hand), you need the Depth API / scene mesh. This is **costly on Quest** — only add
  it if the masterclass genuinely needs occlusion; for the MVP, simple composited content
  without depth occlusion is usually enough. Decide explicitly.
- **Comfort & legibility:** content read against a bright real room needs strong contrast
  and emissive (see `xr-ui-design` and `urp-shadergraph`). No full-screen passthrough tints
  that wash out content.

## Verify

- On device / Meta XR Simulator: the real world is visible, ghost hands + annotations
  composite correctly, no black background bleeding through. Mac Editor Play Mode cannot
  render passthrough — verify on device or simulator and say which.
- `Unity_SceneView_Capture2DScene` for a static sanity check of layer setup.
- `Unity_ReadConsole`: no passthrough/layer errors.

## Out of scope

Scene understanding at scale, room-mesh-driven physics, dynamic relighting — V2. Keep MVR
passthrough to "real room + composited content."

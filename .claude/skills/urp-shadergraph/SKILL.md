---
name: urp-shadergraph
description: Use when authoring or editing a shader/material for Ankhora's three real visual needs on Quest 3 — translucent "ghost hands" for replay, an annotation highlight, or a spatial-anchor halo. Covers URP Shader Graph choices, mobile-VR cost, and the Unity_ManageShader/Unity_ManageAsset workflow. Triggers: shader, shader graph, material, ghost hands, translucent hands, hologram, annotation highlight, anchor halo, transparent, fresnel, emissive, URP shader.
---

# URP Shader Graph (Ankhora)

Ankhora renders on **URP 17.4.0** on a **Quest 3** (mobile GPU, target 90 Hz,
tile-based, fill-rate bound — see `@.cursor/rules/004-vr-performance.mdc`). Shaders are
**not** a generic playground here: there are exactly three visual needs in the MVP/V1.
Build for those; do not add shader complexity the masterclass does not require.

## The three real needs

1. **Ghost hands (replay)** — the expert's recorded hands shown as a translucent,
   slightly emissive overlay so the learner can match pose without occluding their view.
   - Transparent surface, low alpha (~0.25–0.4), soft **Fresnel** rim for a "presence"
     read, subtle emissive tint. No refraction, no per-pixel lighting model heavier than
     needed. Render after opaque; watch for sorting against passthrough.
2. **Annotation highlight** — make an annotated object/area pop without a heavy outline.
   - Cheapest acceptable: emissive boost + Fresnel rim on the existing material, or a thin
     additive shell. Avoid full-screen post outline (fill-rate cost on Quest).
3. **Spatial-anchor halo** — a soft ground/space marker showing where an anchor sits.
   - Unlit transparent, radial gradient / soft ring, gentle pulse via Time. Unlit keeps it cheap.

## Mobile-VR cost rules (non-negotiable on Quest)

- **Unlit > Lit** whenever you can get away with it. Most of the above can be Unlit.
- Transparency is **overdraw** — keep ghost hands and halos small in screen space; never
  stack many large transparent layers.
- No `GrabPass` / scene-color / refraction (expensive or unsupported on tiled mobile GPU).
- Keep texture samples and node count low; prefer math (Fresnel, gradients) over textures.
- Author **single-pass instanced**-safe graphs (stereo). Test in the Meta XR Simulator or
  on device — Mac Editor Play Mode does not render hand tracking (`@CLAUDE.md`).

## Workflow over MCP

1. Create/edit the shader with **`Unity_ManageShader`** (or a Shader Graph asset via
   **`Unity_ManageAsset`**). Keep the graph small and named clearly
   (e.g. `GhostHands_URP`, `AnnotationHighlight_URP`, `AnchorHalo_URP`).
2. Create the material and assign it; wire exposed properties (alpha, rim power, emissive
   color, pulse speed) so they are tweakable without editing the graph.
3. **Validate compilation:** `Unity_ReadConsole` shows no shader errors; `Unity_ValidateScript`
   is for C#, so for shaders rely on console + a capture.
4. **Verify visually:** `Unity_SceneView_Capture2DScene` — confirm the transparent read is
   correct and not z-fighting / mis-sorted against passthrough.

## Out of scope

Stylised lighting overhauls, post-processing stacks, compute/GPU-particle effects — not in
the MVP. If a request heads there, flag the perf budget before building.

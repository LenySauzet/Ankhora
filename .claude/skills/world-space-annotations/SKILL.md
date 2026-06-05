---
name: world-space-annotations
description: Use when building Ankhora's in-MR annotation panels — a world-space text/label placed in real space (bound to a spatial anchor), readable and pokeable by the learner. Triggers: annotation, label, text panel, world-space canvas, callout, note in space, TextMeshPro in VR, poke panel, info bubble, step instruction panel.
---

# World-space annotations

An annotation is Ankhora's atomic teaching unit: a short piece of text/marker placed in the
real room (via a spatial anchor) that the learner reads and can interact with. This skill is
the recipe for one; it sits on top of `spatial-anchors`, `new-xr-interaction`, and the
`xr-ui-design` design language.

## Anatomy of an annotation

1. **A world-space `Canvas`** (not screen-space) holding **TextMeshPro** text (TMP is sharp
   at distance; default UI text is not). Size it for arm's-reach-to-2 m legibility — see `xr-ui-design`.
2. **Anchored in space:** parent the annotation under an `OVRSpatialAnchor` (see
   `spatial-anchors`) so it stays on the real object as the learner moves. Store its
   `annotationId` + `anchorId` in the record/replay data (`record-replay-contract`).
3. **Interaction (optional):** make it pokeable with `meta_add_canvas_interaction_poke`
   (near, in-reach) or ray for far panels — go through Meta's Interaction SDK, never custom
   raycasts (Horizon Store requirement; see `horizon-store-compliance`).

## Behaviour choices (decide per annotation)

- **World-locked vs billboard:** instructions tied to a physical spot stay world-locked;
  a floating hint may billboard to face the learner. Default to world-locked for "this part here".
- **Legibility against passthrough:** the real room is the background — use a solid/contrasting
  panel backing and emissive text so it reads over a bright kitchen/workshop (see `urp-shadergraph`).
- **Reveal timing:** annotations appear/disappear as timeline events during replay
  (`AnnotationShown` / `AnnotationHidden` in the contract), not all at once.

## Workflow over MCP

- Build the canvas + TMP via `Unity_ManageGameObject` / `Unity_ManageAsset`; add poke
  interaction via `meta_add_canvas_interaction_poke` (call `meta_get_config_information` first).
- Keep annotation *data* (id, text, anchor binding, show/hide times) in the contract DTO;
  the MonoBehaviour just renders it.

## Verify

- `Unity_SceneView_Capture2DScene`: panel is legible, correctly placed, faces sanely.
- On device/simulator: poke works, text stays anchored as you move, readable over passthrough.
- `Unity_ReadConsole`: clean.

## Out of scope

Rich media annotations (video/3D widgets), authoring UI for end-users, localisation — V2.
MVP = one short text annotation, anchored, optionally pokeable.

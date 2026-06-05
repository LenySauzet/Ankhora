---
name: new-xr-interaction
description: Use when adding or assembling an XR interaction in the Ankhora Quest 3 scene — a camera/hand rig, a grabbable, a poke/ray canvas, a teleport hotspot, or a spatial-anchor + annotation. Drives the Meta XR Building Blocks (meta_* tools) in the correct order, defines the scene's required-object contract, and verifies the result with a scene capture. Triggers: add XR rig, hand tracking rig, grabbable, interaction rig, poke canvas, ray canvas, teleport, spatial anchor, annotation, build the masterclass scene.
---

# New XR interaction (Meta Building Blocks)

Ankhora prefers **Meta Building Blocks + the Project Setup Tool** over manual rigging
(`@CLAUDE.md` § *Conventions* — highest-leverage efficiency move), and hand-tracking
interactions **must** go through Meta's Interaction SDK (Horizon Store requirement).
This skill encodes that as a repeatable, verifiable procedure over the `meta_*` tools.

## Always step 0: read the live config

Call **`meta_get_config_information` once** before any other `meta_*` tool. It returns
the live `OculusProjectConfig` / `OVRManager` layout you need to set correct values
(hand tracking support, passthrough, anchor support). Never assume the config — read it.

## Ordered assembly

Add only what the interaction needs, in this order (later blocks depend on earlier ones):

1. **`meta_add_camerarig`** — the OVRCameraRig / camera + tracking space. Foundation of every XR scene.
2. **`meta_add_interactionrig`** — hands + controllers via the Interaction SDK. Required
   for any hand-tracking interaction (Ankhora's ghost-hand replay is hand-tracking-first).
3. Then the specific interactor(s):
   - **`meta_add_grabbable`** / **`meta_add_distance_grabbable`** — pick up / distance-grab an object.
   - **`meta_add_canvas_interaction_poke`** / **`meta_add_canvas_interaction_ray`** — UI you
     poke (near) or ray-point (far). Use *poke* for Ankhora's in-reach annotation panels.
   - **`meta_add_teleport_hotspot`** — locomotion target.
4. **`meta_update_android_manifest`** — when the interaction needs a manifest capability
   (hand tracking, passthrough, anchors). Verify against the config from step 0.

## Scene contract (define before you wire)

For the interaction you are adding, state the contract explicitly and keep it minimal:

- **Required objects:** what must exist in the scene for this to work (e.g. a camera rig,
  an interaction rig, the target object, an anchor).
- **Required references:** wire them **explicitly in the scene** — do not rely on runtime
  `GameObject.Find` / `Camera.main` chains (they are fragile and a perf red flag on Quest).
- **Bootstrap:** keep any bootstrap object small and single-purpose.

For the Ankhora MVP the whole scene contract is intentionally tiny: **camera rig +
interaction rig (ghost hands) + 1 spatial anchor + 1 poke annotation panel**. Resist adding more.

## Verify (do not claim done without this)

1. Capture the scene: **`Unity_SceneView_Capture2DScene`** (or `Unity_Camera_Capture`) and
   visually confirm the rig/interactor is present and placed sanely.
2. Check the console: **`Unity_ReadConsole`** — no new errors introduced.
3. If you changed config/manifest, re-run `meta_get_config_information` and confirm the
   expected fields flipped.

Report: which blocks were added, the scene contract, and the capture result.

## Out of scope

Multi-user/co-located rigs, teleport networks, large anchor graphs — all V2
(`@CLAUDE.md` § *Out of scope*). If a request needs them, raise a re-scoping discussion first.

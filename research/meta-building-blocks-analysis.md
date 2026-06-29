# Meta XR Building Blocks — analysis & recommendation for Ankhora

> Decision-support analysis: which of Meta's Building Blocks (Meta XR SDK All-in-One
> `201.0.0`) Ankhora should adopt, deferred, or ignore, given the MVP scope and the V2
> vision. Cross-checked against (a) the **installed** SDK packages in this repo — the
> authoritative list for *our* version — and (b) Meta's official Horizon Unity docs.
>
> *Authored 2026-06-29. Sources at the end. Should be promoted to an ADR once the team agrees.*

---

## TL;DR — the decision

**Adopt Building Blocks, as a curated set, and replace the hand-rolled hand rig with the
`Hand Tracking` block.** Building Blocks are Meta's state-of-the-art, intended way to stand
up a Quest app: they drag-and-drop **standard** scene objects (`OVRCameraRig`,
`OVRHandPrefab`, `OVRInteraction`) with all dependencies + project config wired
automatically — then you own the result. There is **no runtime lock-in** and **no loss of
our pipeline**: the `Hand Tracking` block ships the `OVRHandPrefab`, which exposes the exact
`OVRSkeleton` (joint poses) + `OVRMesh`/`OVRMeshRenderer` (runtime hand mesh) we read for
recording and skinning the ghost. Our current rig is hand-rolled only by incidental drift
during bring-up — the source of the `HandType = -1 → 0 bones` debug session.

For a 3-person, ~10–14-working-day team, hand-rolling Meta rigs is pure downside. The blocks
that map to the MVP are few; most of the catalogue is V2 or irrelevant on Quest 3.

| Verdict | Blocks |
|---|---|
| **Adopt now (MVP)** | Camera Rig · Hand Tracking · Passthrough · Interactions Rig + Poke Interaction · Spatial Audio · Controller Buttons Mapper |
| **Stretch (only if ahead)** | Ray Interaction · Controller Tracking · Grab/Distance Grab |
| **V2** | Spatial Anchor Core · Colocation/Shared Anchors · Scene blocks (Room/Effect Mesh/Instant Placement) · Occlusion · Multiplayer suite · Networked Avatar · AI (LLM/STT/TTS) · Passthrough Camera Access |
| **Ignore (here)** | Eye Gaze (no HW on Quest 3) · Movement/Character Retargeter · Haptics (no hand-tracking haptics) · Entitlement/Platform Init (no accounts in MVP) |

---

## 1. What we're deciding, and from where

- The hands-consolidation slice **spec** said to use the Meta **hand-tracking Building
  Block**; during bring-up we ended up with a **hand-rolled** `OVRHand`/`OVRSkeleton`/
  `OVRMesh` rig (`LeftHand`/`RightHand` GameObjects we configured by hand). That rig now
  works (record → ghost replay device-verified, PR #33), but it deviates from the spec and
  from the project convention (CLAUDE.md: *"prefer Meta Building Blocks + Project Setup Tool
  over manual rigging — the highest-leverage efficiency move"*).
- This analysis exists to **decide deliberately**, with full information, rather than swap
  blindly and risk regressing a green, device-verified pipeline.

## 2. What a Building Block actually is (and isn't)

A Building Block is an **editor-time installer**, not a runtime dependency. Dragging one in:

1. instantiates a **standard** prefab/components (e.g. `OVRCameraRig`, `OVRHandPrefab`,
   the `OVRInteraction` rig) into your scene,
2. pulls in **dependency blocks** automatically (Hand Tracking → requires Camera Rig), and
3. flips the required **project config / Android manifest** flags (hand-tracking permission,
   `OVRManager` support toggles, etc.).

After it runs you hold ordinary OVR/Interaction-SDK GameObjects you can edit, reference, and
extend like any other. **No proprietary wrapper sits between us and the API at runtime** — so
adopting blocks is not lock-in; it's a better, Meta-maintained *starting point* than
hand-assembling the same objects (and getting the `HandType`/`_skeletonType` defaults wrong).

## 3. The load-bearing technical finding

> **Adopting the blocks keeps our record/replay pipeline intact.** Verified two ways.

- **Installed-SDK inventory:** the `Hand Tracking` (Core) block →
  `OVRHandPrefabBuildingBlock.prefab`, whose components are `OVRHand` + **`OVRSkeleton`** +
  **`OVRMesh`** + **`OVRMeshRenderer`** + `SkinnedMeshRenderer`. That is exactly what
  `OvrHandPoseSource` reads (`OVRSkeleton`) and what `SkinnedGhostHandView` skins
  (`OVRMesh`). It depends only on **Camera Rig**, and is **independent of** the Interactions
  Rig.
- **Official docs:** `OVRSkeleton`, `OVRMesh`, `OVRMeshRenderer`, `OVRCustomSkeleton` are all
  still first-class Core components in the current API (v203). The Interaction SDK adds a
  *parallel*, higher-level hand path (`IHand` / `HandVisual` via the `OVRInteraction` rig);
  it does **not** remove the Core path. Both run at once.
- **The real caveat is skeleton format, not blocks:** Meta deprecated the legacy **OVR**
  hand skeleton (~v78) in favour of the **OpenXR** skeleton (26 joints); *"models based on
  the OVR skeleton will not work with the OpenXR skeleton and vice-versa,"* and bone **names**
  changed. Ankhora already runs the **OpenXR** skeleton (CLAUDE.md confirms `XRHandLeft(4)/
  XRHandRight(5)`, 26 joints), and our ghost binds the **runtime** `OVRMesh` (not a baked
  FBX), so we are already on the correct, current path. Swapping to the block stays within it.

**Conclusion:** the migration is a low-risk re-wiring (point `OvrHandPoseSource._leftSkeleton/
_rightSkeleton` and `SkinnedGhostHandView._ovrMesh` at the block's `OVRHandPrefab`), not a
rewrite — provided we re-verify on device.

## 4. Catalogue by category, mapped to Ankhora

The installed SDK exposes ~30 user-facing blocks (and ~69 `BlockData` assets total once you
count hidden/sample/deprecated/internal entries). Mapped to our needs:

### Core
| Block | What it is | Ankhora |
|---|---|---|
| **Camera Rig** | `OVRCameraRig` — head/body tracking, the camera. Foundation of every scene; replaces Main Camera. | **MVP — adopt.** The base everything else needs. |
| **Hand Tracking** | `OVRHandPrefab` (`OVRHand`+`OVRSkeleton`+`OVRMesh`+`OVRMeshRenderer`). | **MVP — adopt; replaces our hand-rolled rig.** Keeps record/ghost pipeline. |
| **Controller Tracking** | 3D controller models + input. | **Stretch.** Hands-first product; controllers are a fallback / player-control convenience. |
| **Passthrough** | `OVRPassthroughLayer` — real world behind virtual content. | **MVP — adopt.** The Learner passthrough toggle. |
| **Eye Gaze** | Eye tracking (foveation, gaze). | **Ignore. Quest 3 has no eye-tracking hardware** (Quest Pro only) — inert on our device. |

### Interaction (Interaction SDK)
| Block | What it is | Ankhora |
|---|---|---|
| **Interactions Rig** | `OVRInteraction` — pre-wired grab/poke/ray/teleport interactors over hands+controllers; exposes `IHand`. | **MVP — adopt.** Required substrate for Store-compliant hand interactions. |
| **Poke Interaction** | Fingertip touch → buttons / world-space UI panels. | **MVP — adopt.** Drives the annotation/Pin panels and in-headset menus. |
| **Ray Interaction** | Far pointing/selection. | **Stretch.** Useful for a distant Masterclass menu; not essential for the in-reach MVP. |
| **Grab / Touch Hand Grab / Distance Grab** | Pick up / move objects. | **Stretch / V2.** Only if the Learner manipulates the Model; MVP just views it. |
| **Teleport** | Point-and-teleport locomotion. | **Ignore (MVP).** MVP is recenter-only, no locomotion. |
| **Controller Buttons Mapper** | Bind controller buttons to actions, no input code. | **MVP — adopt.** Cheap, clean wiring for player controls (play/pause/scrub/loop) on the controller. |

### Passthrough (beyond Core Passthrough)
| Block | Ankhora |
|---|---|
| **Occlusion** (Depth API — real objects hide virtual) | **V2.** Real per-frame depth cost on Quest 3; MVP Model Stage doesn't need it. |
| **Passthrough Window / Surface-Projected** | **V2 / ignore.** Niche MR framing. |
| **Passthrough Camera Access / Visualizer** | **V2.** Gated entitlement; only for custom CV/AI. |

### Audio / Voice
| Block | Ankhora |
|---|---|
| **Spatial Audio** (Meta XR Audio SDK) | **MVP — adopt.** Spatialise the Instructor's recorded narration so voice has direction/distance (our `voice-spatial-audio` skill). |
| **Player Voice Chat** (Photon Voice) | **V2.** Real-time multi-user chat — not single-user narration capture. |
| **Voice SDK / Dictation** (Wit.ai) | **Out of scope.** Voice *commands*, not what we need. |

### Spatial Anchor — all **V2** (Anchored / Room Stage, multi-user)
`Spatial Anchor Core` (`OVRSpatialAnchor`), `Shared Spatial Anchor Core`, `Colocation`,
`Sample Spatial Anchor Controller`. MVP is the **Model Stage** (local play-space frame) — no
real anchors. These power the V2 Room/Anchored Stages and co-located sessions.

### Scene (Scene API / MRUK) — **V2**
`Room Mesh/Model`, `Effect Mesh`, `Instant Content Placement`, `Anchor Prefab Spawner`,
`Find Spawn Positions`, `Room Guardian`, `Scene Debugger`. These are the building bricks of
the **Room Stage** (V2). Need Scene/Spatial-Data permission + a device Space Setup.

### Movement / Avatars / Multiplayer / Haptics / AI
| Family | Ankhora |
|---|---|
| **Movement** (Character Retargeter, body tracking) | **Ignore.** We replay *recorded hands*, not live full-body avatar embodiment. |
| **Avatars** (Networked Avatar) | **V2.** Multi-user only; needs Avatars SDK + platform entitlement. |
| **Multiplayer** (Network Manager, Matchmaking ×4, Networked objects) | **V2.** Co-located/shared sessions are explicitly out of MVP. Needs Photon/Netcode. |
| **Haptics** | **Ignore (here).** Hand tracking has **no haptics** (controllers only); our UX is hands-first, low value. |
| **AI** (LLM, Speech-to-Text, Text-to-Speech, Object Detection) | **V2.** Brand-new ("New"); in-headset RAG assistant is explicitly out of MVP. Several need the gated Passthrough Camera Access. |
| **Platform Init / Entitlement Check** | **Ignore (MVP).** No accounts/store entitlement in the MVP. |

## 5. Recommendation — the Ankhora block set

**Adopt now (the MVP rig):**

1. **Camera Rig** — the base.
2. **Hand Tracking** — *replaces the hand-rolled rig*; preserves `OVRSkeleton` + `OVRMesh`.
3. **Passthrough** — Learner MR toggle.
4. **Interactions Rig + Poke Interaction** — Store-compliant hand interaction substrate for
   the annotation/Pin panels and in-headset UI.
5. **Spatial Audio** — spatialised narration replay.
6. **Controller Buttons Mapper** — player controls wiring.

**Stretch (only if ahead):** Ray Interaction (far menu), Controller Tracking, Grab/Distance
Grab (if Model manipulation lands).

**Defer to V2:** Spatial Anchor Core + Colocation/Shared Anchors (Room/Anchored Stage),
Scene blocks (Room Stage), Occlusion, the Multiplayer suite + Networked Avatar, AI blocks,
Passthrough Camera Access.

**Ignore on this project:** Eye Gaze (no Quest 3 hardware), Movement/Character Retargeter,
Haptics, Platform Init/Entitlement (no MVP accounts).

This set is **exactly the MVP success criteria** (record voice+hands, place pins, replay with
controls, passthrough toggle) and nothing more — YAGNI-clean.

## 6. Migration plan (adopt without regressing)

The hand-rig swap is the only delicate part because PR #33 is green and device-verified. Do
it as a **deliberate, device-verified step**, not a blind drop-in:

1. `meta_get_config_information` first (read the live config — per the `new-xr-interaction`
   skill) and confirm hand-tracking + passthrough support flags.
2. Add **Camera Rig** → **Hand Tracking** via the Building Blocks window (or `meta_*` tools).
3. Re-point the scene references: `OvrHandPoseSource._leftSkeleton/_rightSkeleton` →
   the block's `OVRSkeleton`s; `SkinnedGhostHandView._ovrMesh` → the block's `OVRMesh`
   (same hand). Move the `MasterclassRecorder`, `GhostHandPlayer`, HUD, and the
   `OvrHandPoseSource._trackingSpace`/`_centerEye` wiring onto the block's rig.
4. Add **Interactions Rig + Poke** when the annotation panel lands.
5. **Re-verify on device**: record → ghost replay (the exact PR-#33 check) must still pass;
   confirm `OVRSkeleton` reports 26 OpenXR joints and the ghost mesh is intact.
6. Delete the hand-rolled `LeftHand`/`RightHand` rig only **after** the re-verify is green.

Because blocks emit standard objects, steps 3–6 are re-wiring + a device check — reversible
via git if anything regresses.

## 7. Sequencing & risk

- **Do NOT** redo the swap inside PR #33: it works, ship it. Adopt the blocks as the **first
  task of the Pins slice**, where the Interactions Rig + Poke earn their keep (the annotation
  panel needs ISDK for Store compliance anyway) and where re-verifying hands is natural.
- **Document the deviation:** PR #33 shipped a hand-rolled rig; the Building-Block migration
  is tracked as the opening task of the next slice. Promote this analysis to an ADR.
- **Residual risk:** low. The only real failure mode (OVR-vs-OpenXR skeleton mismatch) is
  already neutralised — we're on OpenXR and bind the runtime mesh.

## Sources

**Installed SDK (authoritative for our `201.0.0`):**
- `Hand Tracking` block → `com.meta.xr.sdk.core@…/Editor/BuildingBlocks/BlockData/HandTracking/…/OVRHandPrefabBuildingBlock.prefab` (OVRHand + OVRSkeleton + OVRMesh + OVRMeshRenderer)
- `Interactions Rig` → `com.meta.xr.sdk.interaction.ovr@…/Editor/Blocks/Interactions/Interactions.asset` (`OVRComprehensiveRigWizard`)

**Meta official docs (verified):**
- Building Blocks overview & list — https://developers.meta.com/horizon/documentation/unity/unity-building-blocks-overview/ , https://developers.meta.com/horizon/documentation/unity/bb-overview/
- Hand Visual / `IHand` vs `OVRSkeleton`/`HandVisual` — https://developers.meta.com/horizon/documentation/unity/unity-isdk-hand-visual/
- `OVRMesh` (runtime mesh + OVRSkeleton + OVRMeshRenderer) — https://developers.meta.com/horizon/reference/unity/v203/class_o_v_r_mesh/
- OVR→OpenXR skeleton deprecation / incompatibility — https://developers.meta.com/horizon/documentation/unity/unity-isdk-openxr-hand/ , https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/
- Hand-tracking modes (FMM/WMM/Multimodal/OpenXR) — https://developers.meta.com/horizon/documentation/unity/unity-handtracking-overview/
- Multiplayer / Avatars version requirements — https://developers.meta.com/horizon/documentation/unity/bb-multiplayer-blocks/
- AI Agents/Providers — https://developers.meta.com/horizon/documentation/unity/unity-ai-agents/

**Could not fully confirm (open the in-Editor Building Blocks window for the exact `201.0.0`
labels/versions):** exact Haptics block row; "introduced" versions for Controller Tracking /
Core Passthrough / Eye Gaze; whether a standalone "Scene Mesh" / "Body Tracking" block exists
under those names; the marketing `v##` ↔ package `201.0.0` mapping.

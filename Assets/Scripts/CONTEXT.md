# Assets/Scripts — architecture map

Two assemblies: a pure **`Domain`** kernel + a single **`Foundation`** device layer, organised by
feature folders. Decision + rationale: [ADR-0004](../../docs/02-architecture/adr/0004-domain-foundation-two-assembly-split.md)
(supersedes the per-feature-assembly layout of [ADR-0003](../../docs/02-architecture/adr/0003-feature-based-script-architecture.md);
its kernel principles still hold).

## Where things live

```
Scripts/
  Domain/                 # pure kernel — the record/replay spine. Assembly: Ankhora.Domain. No MonoBehaviour, no OVR.
    Model/                # pure [Serializable] DTOs = the persisted contract. NO logic.
                          #   Masterclass → Chapter → Timeline → PoseFrame (head + L/R HandPose) + Pin
                          #   Timeline also carries left/right HandSkeleton (per-hand bone topology + bind poses)
    Serialization/        # persist the model: IMasterclassSerializer, JsonMasterclassSerializer, MasterclassMigrator
    Sampling/             # pure read logic: TimelineSampler (interpolated head + per-hand bone playback)
    Recording/            # pure timing: TimelineRecorder (fixed-rate accumulator), AutoCaptureClock
    Spatial/              # pure transforms: PoseSpace (world ↔ reference-frame conversions)
  Foundation/             # device layer — MonoBehaviour + OVR. Assembly: Ankhora.Foundation → Ankhora.Domain, Oculus.VR.
    Recording/            # IHandPoseSource / IHandSkeletonSource seams, OvrHandPoseSource (OVRSkeleton reader),
                          #   RecordingSession (shared capture core), FirstLightAutoCapture (bring-up trigger)
    Replay/               # IHandView seam, GhostHandPlayer (drives views from a Timeline), FkGhostHandView
    Persistence/          # MasterclassStore (load/save JSON to persistentDataPath)
    Passthrough/          # OvrPassthroughSurface + shader props
    App/                  # composition roots that wire features together (e.g. FirstLightReplayLink)
```

Tests live in `Assets/Tests/EditMode/` (assembly `Ankhora.Tests.EditMode`, references `Ankhora.Domain` + `Ankhora.Foundation`).

## Conventions (apply to every new feature)

1. **Folder == namespace.** `Domain/Model` → `Ankhora.Domain.Model`; `Foundation/Replay` → `Ankhora.Foundation.Replay`.
2. **Model is data-only.** DTOs are `[Serializable]` and hold no behaviour; logic sits beside them
   (e.g. `TimelineSampler`), keeping the wire format clean and the model trivially testable.
3. **One responsibility per type.** Parsing ≠ migrating; sampling ≠ rendering; capture ≠ persistence.
4. **Pure logic goes in `Domain`, device/OVR code in `Foundation`.** `Domain` must never reference
   `MonoBehaviour`/`Oculus`. Inside `Foundation`, feature folders **must not reference each other**
   directly — connect them with a composition root in `Foundation/App/` (convention, not compiler-enforced).
5. **EditMode-test the logic.** Anything without a frame loop is plain C# tested without a headset. See `unity-testability`.
6. **Confirm Meta API shapes before coding** (context7 / Meta docs) — never invent bone counts,
   signatures, or enum values.

## Canonical vs. scaffolding (read before extending)

- **Canonical path:** `OvrHandPoseSource` → `RecordingSession` (+ `MasterclassStore`) → JSON; then
  `GhostHandPlayer` → `FkGhostHandView`. `RecordingSession` is the shared capture core every trigger
  delegates to (it captures the per-hand skeleton — a recorder that skips that produces unreplayable files).
- **Scaffolding (transitional):** `FirstLightAutoCapture` + `AutoCaptureClock` are the buttonless
  bring-up trigger, to be replaced by a hand-pinch trigger in the hands-consolidation slice;
  `FkGhostHandView` (joint spheres) is the validated debug visual, to be joined/replaced by a skinned
  Meta ghost mesh. `SimulatedHandPoseSource` is the Mac/headless stand-in.

## Planned next slices

- **Consolidation:** Meta Building-Blocks hand rig (live hands) + skinned ghost mesh + pinch trigger.
- **Voice Track**, **Annotations (Text/Image Pins)**, **Player controls** (scrub / slow-mo / loop / recenter / passthrough toggle).

Out of scope (V2): anchors at scale, backend sync, marketplace, multi-user — see `docs/01-product/mvp-scope.md`.

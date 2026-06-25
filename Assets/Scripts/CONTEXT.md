# Assets/Scripts — architecture map

Feature-based layout with a shared `Domain` kernel. Decision + rationale:
[ADR-0003](../../docs/02-architecture/adr/0003-feature-based-script-architecture.md).

## Where things live

```
Scripts/
  Domain/                 # shared kernel — the record/replay spine. Assembly: Ankhora.Domain.
    Model/                # pure [Serializable] DTOs = the persisted contract. NO logic, NO MonoBehaviour.
                          #   Masterclass → Chapter → Timeline → PoseFrame (head + L/R HandPose) + Pin
    Serialization/        # persist the model: IMasterclassSerializer, JsonMasterclassSerializer,
                          #   MasterclassMigrator (schemaVersion upgrades)
    Sampling/             # pure read logic over the model: TimelineSampler (interpolated playback)
  Recording/  Replay/  Annotations/   # features (planned) — see below
```

Tests live in `Assets/Tests/EditMode/` (assembly `Ankhora.Tests.EditMode`, references `Ankhora.Domain`).

## Conventions (apply to every new feature)

1. **Folder == namespace.** `Domain/Model` → `Ankhora.Domain.Model`.
2. **Model is data-only.** DTOs are `[Serializable]` and hold no behaviour; logic sits beside them
   (e.g. `TimelineSampler`), keeping the wire format clean and the model trivially testable.
3. **One responsibility per type.** Parsing ≠ migrating; sampling ≠ rendering.
4. **A feature = a folder + its own asmdef** that references `Ankhora.Domain` (and only what it needs).
   Features depend on Domain, never on each other.
5. **EditMode-test the logic.** Anything without a frame loop is plain C# tested without a headset
   (run: `Unity -runTests -batchmode -testPlatform EditMode`, Editor closed). See `unity-testability`.
6. **Confirm Meta API shapes before coding** (context7 / Meta docs) — never invent bone counts,
   signatures, or enum values.

## Planned features (each will be its own slice)

- **Recording/** — capture voice + hands into a `Masterclass` (writes `Domain.Model`).
- **Replay/** — the Player: drives ghost hands + audio from a `Masterclass` via `Domain.Sampling`,
  with scrub / slow-mo / loop / recenter and a passthrough toggle.
- **Annotations/** — author and display Text/Image `Pin`s as world-space panels.

Out of scope (V2): anchors at scale, backend sync, marketplace, multi-user — see
`docs/01-product/mvp-scope.md`.

# ADR-0004: Two-assembly split — `Domain` kernel + `Foundation` device layer

- **Status:** Accepted
- **Date:** 2026-06-27
- **Deciders:** Lény Sauzet (Claude Code pairing)
- **Tags:** architecture, domain, conventions
- **Supersedes:** the assembly/folder *layout* of [ADR-0003](0003-feature-based-script-architecture.md) (its kernel principles are kept)

## Context and problem

[ADR-0003](0003-feature-based-script-architecture.md) specified **feature-based vertical slices,
each its own top-level folder and assembly** (`Recording/`, `Replay/`, `Annotations/` as siblings
of `Domain/`, every feature an asmdef, "features depend on Domain, never on each other"). When the
record→replay slice (S3) was actually built, the code instead grew as **`Domain/` + a single
`Foundation/`** device assembly, with `Recording/`, `Replay/`, `Passthrough/`, `Persistence/`, `App/`
as **folders inside `Foundation`**. That is closer to ADR-0003's rejected "layer-based" option than
to its accepted decision. The drift was never recorded, so the codebase silently contradicted an
accepted ADR. This ADR makes the real shape the decision.

## Decision drivers

- The genuine axis of change for this project is **pure logic vs. device/OVR code**, not
  feature-by-feature isolation: every device feature needs `Oculus.VR` and `UnityEngine`, and the
  features are small and tightly co-developed on a 10–14 day MVP.
- Per-feature asmdefs are real ceremony (one asmdef + cross-references per feature, slower domain
  reloads, more to keep in sync) with little payoff at this size.
- The kernel discipline from ADR-0003 — a pure, headless, EditMode-testable `Domain` — is the part
  that actually pays off (the Mac station can't run hand tracking; pure logic is where the tests
  live) and must be kept.

## Decision

**Two assemblies:**

- **`Ankhora.Domain`** — the pure kernel. Plain C# (`UnityEngine` math types like `Quaternion`/`Pose`
  allowed; no `MonoBehaviour`, no `Oculus`/OVR). Holds the record/replay spine: `Model/`,
  `Serialization/`, `Sampling/`, `Recording/` (pure timing), `Spatial/` (pure transforms). EditMode-tested.
- **`Ankhora.Foundation`** — the single device layer. `MonoBehaviour` + OVR. References `Ankhora.Domain`
  and `Oculus.VR`. Internally organised by **feature folders** (`Recording/`, `Replay/`,
  `Passthrough/`, `Persistence/`, `App/`).

**Rules kept from ADR-0003:** folder == namespace; the Model is data-only (logic lives beside it);
one responsibility per type; `Domain` stays genuinely cross-cutting (no feature-specific dumping).

**Cross-feature wiring:** feature folders inside `Foundation` still must not reference each other
directly (e.g. `Recording` must not `using Ankhora.Foundation.Replay`). When two features must be
connected, a **composition-root component** in `Foundation/App/` does the wiring (e.g.
`RecordReplayLink` subscribes the recorder's `OnRecordingSaved` event to the player). This keeps
the no-cross-feature-dependency intent of ADR-0003 without per-feature assemblies — enforced by
convention, not by the compiler.

**UI is a presentation layer, not a peer feature (exception).** `Foundation/UI` may *observe* a
feature by holding an inspector-wired (`[SerializeField]`) reference to it and subscribing to its
events one-way (e.g. `RecordingHud` reads `PinchRecordingTrigger`'s `OnCountdownTick` /
`OnRecordingStarted` / `OnRecordingSaved`). This is a read-only presentation dependency, not the
peer-to-peer *logic* coupling the rule above forbids — routing every HUD through the composition root
would add indirection for no boundary benefit. The constraints: the dependency is **one-way**
(feature → UI never UI → feature beyond invoking the feature's own public API), UI holds **no
business logic**, and a feature must never `using Ankhora.Foundation.UI`. Two non-UI feature folders
wanting to talk to each other still go through `Foundation/App/`.

## Considered options

1. **Split `Foundation` into per-feature asmdefs** (honour ADR-0003 literally). Rejected: ceremony
   without payoff for this team/size; every device feature shares the same OVR/Unity dependency anyway.
2. **Leave the drift undocumented.** Rejected: an accepted ADR contradicted by the code is a trap for
   the next contributor/agent.
3. **`Domain` + single `Foundation` device assembly, documented here.** Chosen.

## Consequences

- **Positive:** less assembly ceremony; the pure/device boundary (the one that matters for testing)
  is explicit and enforced by the compiler; navigation stays feature-folder based; the ADR and code agree.
- **Negative / accepted trade-offs:** the no-cross-feature-dependency rule inside `Foundation` is now
  a *convention* (compiler won't catch a stray `Recording → Replay` reference); the composition-root
  pattern is the mitigation. If `Foundation` later grows large or a feature needs true isolation, a
  feature can be promoted to its own asmdef then.

## Links

- Supersedes layout of: [ADR-0003](0003-feature-based-script-architecture.md)
- Related: [ADR-0001](0001-unity6-migration.md)
- Convention summary (next to the code): `Assets/Scripts/CONTEXT.md`
- MVP: `docs/01-product/mvp-scope.md`

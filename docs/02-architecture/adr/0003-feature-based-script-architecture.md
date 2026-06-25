# ADR-0003: Feature-based C# architecture with a shared Domain kernel

- **Status:** Accepted
- **Date:** 2026-06-25
- **Deciders:** Lény Sauzet (Claude Code pairing)
- **Tags:** architecture, domain, conventions

## Context and problem

Ankhora's first feature code — the record/replay data model (`#25`, PR `#26`) — is the
**spine** every later feature depends on (recorder, replay/player, annotations). With a 3-person
team and ~10–14 effective coding days, the code must be cheap to navigate (for both humans and
LLM agents, which do most of the authoring here) and cheap to extend, without an architecture
astronaut tax on an ultra-thin MVP. We need a single, documented layout convention applied from
the first script so features don't each invent their own.

## Decision drivers

- **LLM- and human-navigable:** a new contributor (or agent) should infer where code lives from
  the folder tree alone.
- **Testable without a headset:** domain logic stays plain C# (EditMode-testable on the Mac
  station), no `MonoBehaviour`.
- **Cheap to extend:** adding the recorder/replay features must not require reshaping the model.
- **No over-engineering:** no empty layers or speculative abstractions (YAGNI on an MVP).

## Considered options

1. **Flat scripts folder** — everything under `Assets/Scripts/`. Simplest, but unnavigable as it grows.
2. **Layer-based (technical layers top-level)** — `Models/`, `Services/`, `Views/` across the
   whole app. Familiar, but scatters one feature across many folders.
3. **Feature-based (vertical slices) + a shared Domain kernel** — features are top-level folders
   with their own assemblies; cross-cutting model/serialisation lives in a shared `Domain` kernel.

## Decision

We chose **Option 3: feature-based slices with a shared `Domain` kernel.**

The single most important reason: a feature should be readable as one folder, while the
record/replay *contract* — used by every feature — has exactly one home.

Layout:

```
Assets/Scripts/
  Domain/                     # shared kernel (assembly Ankhora.Domain) — the record/replay spine
    Model/                    # pure [Serializable] DTOs = the persisted contract (no logic)
    Serialization/            # persistence of the model (JSON now; migration; a binary form later)
    Sampling/                 # pure read logic over the model (interpolated playback sampling)
  Recording/   Replay/   Annotations/   # features (future): each its own assembly, depends on Domain
```

Rules:
- **Folder == namespace** (`Ankhora.Domain.Model`, `…Serialization`, `…Sampling`).
- **Model is data-only**; logic lives beside it (e.g. `TimelineSampler`), never on the DTO.
- **One responsibility per type** (e.g. parsing JSON vs. migrating a schema are separate classes).
- **Each feature gets its own assembly definition** referencing `Ankhora.Domain`, so compile units
  stay small and dependencies are explicit (features depend on Domain, never on each other).

## Consequences

- **Positive:** predictable navigation; the model contract has one home; features can be built and
  tested in isolation; agents can be pointed at a single folder.
- **Negative / accepted trade-offs:** a little more ceremony (per-feature asmdefs, sub-namespaces)
  than a flat layout; the `Domain` kernel must stay genuinely cross-cutting (resist dumping
  feature-specific code there).
- **Follow-ups:** apply the convention to the `recorder` and `playback` features; see
  `Assets/Scripts/CONTEXT.md` for the working summary kept next to the code.

## Links

- Related: [ADR-0001](0001-unity6-migration.md)
- Spec: [`docs/02-architecture/domain-record-replay-model.md`](../domain-record-replay-model.md)
- Convention summary (next to the code): `Assets/Scripts/CONTEXT.md`
- Skill: `.claude/skills/record-replay-contract` · MVP: `docs/01-product/mvp-scope.md`

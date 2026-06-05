# ADR-0001: Migrate from Unity 2022.3 LTS to Unity 6

- **Status:** Accepted
- **Date:** 2026-06-05
- **Deciders:** Ankhora team (3)
- **Tags:** unity, xr, build, ci, tooling

## Context and problem

The project and the master research dossier originally pinned **Unity 2022.3 LTS**
(`2022.3.62f3`) — the conservative, well-supported baseline for Quest 3 development.

While wiring the Claude Code ↔ Unity integration, we found that the live editor
control loop we wanted (Unity native MCP via `com.unity.ai.assistant` + the Meta XR
MCP Extension) is built around **Unity 6**: the per-OS relay binary that bridges
Claude Code to the Editor ships with Unity 6, and the AI Assistant package that
exposes the native MCP server targets Unity 6. Staying on 2022.3 would have meant
giving up the MCP-driven workflow that is central to how this 3-person, time-boxed
team intends to work (see `CLAUDE.md` § *AI tooling — Claude Code ↔ Unity (MCP)*).

The migration was performed in place on branch `chore/claude-unity-integration`.
This ADR formalises a decision that was already executed; `CLAUDE.md` flagged it as
"still pending an ADR."

## Decision drivers

- We want Claude Code to drive the Editor live over MCP (the project's core dev-loop bet).
- The MCP relay + native MCP server are Unity 6 features; 2022.3 cannot host them.
- All three machines must run the **exact same** Editor version (version mismatch is a
  frequent cause of avoidable scene/prefab diffs in a small team).
- The MVP is ultra-thin and the codebase is still in spec phase, so the cost of moving
  now (almost no gameplay code to break) is far lower than moving later.

## Considered options

1. **Stay on Unity 2022.3 LTS** — maximally stable, but no native MCP relay; the
   Claude-Code-drives-Unity workflow would be lost or downgraded to brittle externals.
2. **Migrate to Unity 6 (`6000.4.10f1`)** — unlocks the native MCP workflow; LTS-class
   stability on the Unity 6 line; URP 17.
3. **Wait for a later Unity 6 patch** — marginally more mature, but blocks the dev-loop
   we are building the whole tooling story around, for no concrete benefit today.

## Decision

We chose **Option 2: migrate to Unity 6 (`6000.4.10f1`)** on all three machines.

The single most important reason: the live Claude Code ↔ Unity MCP loop — the
team's primary efficiency bet given ~10–14 effective coding days — only exists on
Unity 6.

### What changed

- **Editor:** `2022.3.62f3` → `6000.4.10f1` (Apple Silicon on the Mac station).
- **Render pipeline:** URP `14.0.12` → `17.4.0`.
- **XR / Meta:** added `com.meta.xr.sdk.all` `201.0.0` (all-in-one Meta XR SDK,
  bundling the Interaction SDK), `com.meta.xr.unity-mcp.extension` (Meta XR MCP
  Extension, `meta_*` tools), `com.unity.xr.openxr` `1.17.1`.
- **AI tooling:** added `com.unity.ai.assistant` `2.10.0-pre.1` (native AI Assistant +
  native MCP server — the relay Claude Code connects to).
- **Generated config:** `Assets/Oculus/OculusProjectConfig.asset`, MetaXR
  audio/acoustics/runtime settings under `Assets/Resources/`, XR loader settings under
  `Assets/XR/`.

## Consequences

- **Positive:** Claude Code can drive the Editor live (`Unity_*` + `meta_*` tools);
  URP 17 and the current Meta XR SDK are available; the team works on a single modern baseline.
- **Negative / accepted trade-offs:**
  - **All three machines must install exactly `6000.4.10f1`.** Mismatch = scene/prefab diffs.
  - **CI cannot build the Quest APK.** Meta XR SDK `201.0.0`'s `OVRProjectConfig` static
    ctor throws `ArgumentOutOfRangeException` on a headless Linux editor (OVRPlugin reports
    no version). It is a Meta SDK bug, deterministic on GameCI's Linux runner, unfixable
    project-side, and `201.0.0` is the latest published SDK. CI therefore runs
    `unity-test-runner` (EditMode) — it compiles the whole project and runs tests without
    `BuildPlayer`. The APK is built locally on device; reintroduce a CI APK build on a
    **Windows** runner (where OVRPlugin loads) if/when Meta fixes the Linux path.
  - `com.unity.ai.assistant` is a `-pre` (pre-release) package — acceptable for an
    internal student project, to be re-evaluated before any production claim.
- **Follow-ups:**
  - Keep all three Editors pinned to `6000.4.10f1`.
  - Revisit a Windows-runner APK build once Meta ships a Linux-safe `OVRProjectConfig`.

## Links

- `CLAUDE.md` § *Current technical state*, § *What "build" / "run" / "test" mean today*
- `docs/05-operations/setup-unity6-claude-integration.md` — per-OS MCP setup
- `research/xr-platform-master-research.md` — canonical XR/Unity knowledge base

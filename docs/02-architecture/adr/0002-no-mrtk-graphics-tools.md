# ADR-0002: Do not adopt MRTK Graphics Tools in Ankhora (use URP Shader Graph)

- **Status:** Accepted
- **Date:** 2026-06-06
- **Deciders:** Lény Sauzet
- **Tags:** xr, rendering, urp, vfx, tooling

## Context and problem

Exploring XR visual effects (following a Valem tutorial), we installed Microsoft's
**MRTK Graphics Tools** (`com.microsoft.mrtk.graphicstools.unity` `v0.8.0`) into Ankhora.
The Editor immediately failed to compile. Investigation surfaced a **hard, mutually
exclusive version conflict** between MRTK Graphics Tools and Ankhora's own stack:

- **MRTK Graphics Tools requires URP ≤ 17.0.** Its **Acrylic** feature (frosted-glass
  blur) ships URP render features — `AcrylicBlurRenderPass`, `DrawFullscreenPass`,
  `ClearRenderTargetPass` — that override the `ScriptableRenderPass` compatibility-mode
  API (`Configure`, `Execute(ref RenderingData)`, `OnCameraSetup`) **removed in URP 17.4**
  → 5× `CS0115`. The latest tag (`v0.8.1`) and the repo's default branch still use the
  obsolete API; Microsoft has not ported it to Render Graph and the repo is largely dormant.
- **Ankhora is pinned to Unity `6000.4.10f1` / URP `17.4.0` by Meta XR SDK `201.0.0`**
  (see [ADR-0001](0001-unity6-migration.md)). `MetaXRAcousticMap.cs` calls
  `UnityEngine.GUID`, a runtime type absent from Unity 6.0 LTS (`6000.0.76f1`) — verified:
  0 occurrences in that Editor's `UnityEngine.CoreModule.dll`. So we cannot downgrade to
  the Unity 6.0 LTS line where MRTK GT would compile.

The two cannot coexist in one project: Unity 6.0 + URP 17.0 makes MRTK GT compile but
breaks Meta SDK 201; Unity 6000.4 + URP 17.4 keeps Meta SDK 201 but breaks MRTK GT.

A deep read of the official MRTK docs (2026-06-06) refined the picture: **only Acrylic is
render-feature-based.** Every other Graphics Tools feature — the Fluent **Standard
shader**, **hover/proximity lights**, **clipping primitives**, **mesh outlines** (an
object-level pass, not fullscreen), and the **Unity UI** plates — is `shader/material` or
`MonoBehaviour` and is Render-Graph-agnostic. They *would* compile on URP 17.4; the package
only fails as a whole because Acrylic's render-feature files are compiled with it.

## Decision drivers

- Ankhora's Editor/URP version is **not negotiable** — Meta XR SDK 201.0.0 dictates it.
- The project standardised on **URP Shader Graph** for shaders (skill `urp-shadergraph`)
  and on **Meta XR Interaction SDK** for hand input.
- Ankhora's real VFX needs — translucent **ghost hands**, an **annotation highlight**, a
  **spatial-anchor halo** — are all achievable directly in URP Shader Graph.
- Adopting Graphics Tools means **embedding a forked, dormant Microsoft package** and
  maintaining it (delete Acrylic to make it compile).
- Adopting the **rest of MRTK3** (Input/UX/Spatial Manipulation) is off the table: it is
  built on Unity XR Interaction Toolkit and would **conflict with / duplicate** the Meta
  Interaction SDK; MRTK3's Quest support is "experimental" and targets Unity 2021/2022.
- 3-person team, ~10–14 effective coding days — avoid optional dependencies that do not
  pay for themselves.

## Considered options

1. **Adopt MRTK Graphics Tools as-is** — fails: Acrylic's render features break the build on URP 17.4.
2. **Embed + trim** — copy the package into `Packages/`, delete the Acrylic folder + its
   render features; keep the shaders/lights/UI. Works, but creates a forked dependency to
   maintain, and brings a HoloLens/Fluent design language that diverges from Ankhora's
   visionOS / Meta Spatial UI direction (skill `xr-ui-design`).
3. **Do not adopt; use URP Shader Graph** — author Ankhora's effects natively in the
   project-standard pipeline; cherry-pick a specific Graphics Tools effect later (re-author
   in Shader Graph, or do a minimal embed+trim) only if one proves its value.

## Decision

We chose **Option 3: do not adopt MRTK Graphics Tools.** Ankhora's visual effects are
authored in **URP Shader Graph**.

The single most important reason: Ankhora's stack is locked to URP 17.4 by Meta SDK 201,
where MRTK Graphics Tools cannot compile — and everything Ankhora actually needs is
reachable in Shader Graph without a forked dependency or an interaction-layer conflict.

## Consequences

- **Positive:** no forked/dormant dependency; no MRTK-vs-Meta interaction overlap; VFX live
  in the project-standard Shader Graph pipeline, cost-controllable for Quest 3.
- **Negative / accepted trade-offs:**
  - We **lose the Acrylic frosted-glass effect** outright on the current URP.
  - Any Graphics-Tools-style effect we later want must be **re-authored in Shader Graph**,
    or pulled in via a **targeted embed+trim** (effort owned at that time).
- **Follow-ups:**
  - Learning/exploration of MRTK Graphics Tools continues in a **separate Unity 6.0 LTS
    sandbox** project (plain URP, no Meta SDK), outside this repo.
  - When a specific effect is wanted in Ankhora, decide per-effect: Shader Graph re-author
    (default) vs minimal embed+trim. Open a feature slice at that point.
  - MRTK Graphics Tools was removed from the project (commit after snapshot `127f961`).

## Links

- Related: [ADR-0001](0001-unity6-migration.md) — the Unity 6000.4 / Meta SDK 201 baseline that pins the URP version.
- Spike log: [`docs/superpowers/experiments/2026-06-05-xr-vfx-mrtk-graphics-tools.md`](../../superpowers/experiments/2026-06-05-xr-vfx-mrtk-graphics-tools.md)
- Skills: `.claude/skills/urp-shadergraph/SKILL.md`, `.claude/skills/xr-ui-design/SKILL.md`
- MRTK Graphics Tools (MIT): https://github.com/microsoft/MixedReality-GraphicsTools-Unity

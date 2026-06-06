# Spike — XR visual effects (Valem / MRTK Graphics Tools)

> A learning spike, **not** committed product scope. Branch `feat/xr-vfx-spike`.
>
> *Started: 2026-06-05*

## Goal

Follow Valem's tutorial *"How to Make XR Visual Effects in Unity — MRTK Graphics Tool"*
to learn how to author XR-friendly visual effects, and judge what carries over to
Ankhora's three real VFX needs: translucent **ghost hands** (replay), an **annotation
highlight**, and a **spatial-anchor halo**.

## Scope of this branch

- Install / try the **MRTK Graphics Tools** package and reproduce the tutorial's effects.
- Keep experiments isolated (a throwaway scene + materials); do **not** rewire the main
  rig or the build pipeline.
- Note findings below as the spike progresses.

## Caveat to reconcile before anything ships

Ankhora standardised on **Meta XR SDK + URP Shader Graph** (see the
[`urp-shadergraph`](../../../.claude/skills/urp-shadergraph/SKILL.md) skill), not MRTK.
MRTK Graphics Tools is a Microsoft package and may pull MRTK dependencies. Treat anything
learned here as input; a keeper effect should be **re-authored in URP Shader Graph** and
cost-checked for mobile VR (Quest 3) before it lands in a real feature slice
(S3 ghost hands, S6 pins — see [`feature-roadmap.md`](../../01-product/feature-roadmap.md)).

## Findings — MRTK Graphics Tools cannot live in Ankhora (2026-06-06)

The spike hit a hard, verified incompatibility. MRTK Graphics Tools and Ankhora's Meta
XR SDK have **mutually exclusive Unity-version requirements**:

- **MRTK Graphics Tools `v0.8.0`/`v0.8.1` requires URP ≤ 17.0.** Its render features
  (`AcrylicBlurRenderPass`, `DrawFullscreenPass`, `ClearRenderTargetPass`) override the
  `ScriptableRenderPass` compatibility-mode API (`Configure`, `Execute(ref RenderingData)`,
  `OnCameraSetup`) that **URP 17.4 removed** → 5× `CS0115`. Even the latest tag and the
  repo's default branch still use the obsolete API (no RenderGraph port).
- **Meta XR SDK `201.0.0` requires Unity ≥ 6000.4.** `MetaXRAcousticMap.cs` calls
  `UnityEngine.GUID.TryParse(...)`, a runtime type that **does not exist in Unity 6.0 LTS
  `6000.0.76f1`** (verified: 0 occurrences in `UnityEngine.CoreModule.dll`) → `CS0234`.
  The package's `package.json` claims `2022.3.15f1` min, but that metadata is inaccurate.

So: Unity 6.0 LTS + URP 17.0 makes MRTK compile but breaks Meta SDK 201; Unity 6000.4 +
URP 17.4 makes Meta SDK 201 work but breaks MRTK. **They cannot coexist in one project.**

This also retroactively explains the 6000.4 migration: it is **required** by Meta SDK
201.0.0, not merely convenient for the Claude/Unity integration.

### Decision

- **MRTK Graphics Tools is removed from Ankhora** (commit after the snapshot `127f961`).
  Ankhora stays on `6000.4.10f1` with Meta SDK 201 and URP 17.4.
- **The Valem tutorial is followed in a separate Unity 6.0 LTS sandbox project** (plain
  URP, MRTK GT `v0.8.0`, no Meta SDK needed — the VFX are Graphics Tools shaders).
- **Keeper effects return to Ankhora re-authored in URP Shader Graph** (skill
  [`urp-shadergraph`](../../../.claude/skills/urp-shadergraph/SKILL.md)), cost-checked for
  Quest 3 — exactly the reconciliation this doc's Caveat already called for.

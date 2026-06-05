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

## Findings

- _(to fill as the tutorial is followed)_

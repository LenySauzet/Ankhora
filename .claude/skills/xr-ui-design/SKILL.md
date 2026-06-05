---
name: xr-ui-design
description: Use when designing or reviewing any spatial UI in Ankhora — annotation panels, the masterclass menu, step indicators, dialogs — so it follows established XR design systems (Apple visionOS HIG, Meta Horizon OS / Meta Spatial design) for ergonomics, legibility, comfort, and input. Triggers: XR UI, spatial UI, UI design, UX, design system, visionOS, Horizon OS, comfort zone, ergonomics, gaze, pinch, dwell, legibility, panel layout, where to place UI, how big should the panel be, spatial design.
---

# XR UI design language (visionOS + Meta Spatial)

Ankhora's interfaces should feel native to the headset, not like a flat app pasted into 3D.
This skill encodes the spatial-UI principles shared by **Apple visionOS HIG** and **Meta
Horizon OS / Meta Spatial design**, applied to Ankhora's surfaces (annotation panels,
masterclass browser, step indicators, recording controls).

> **Pull current guidelines, don't trust memory.** Both design systems evolve. When a
> decision hinges on specifics (exact angular sizes, recommended distances, material specs),
> fetch the live guidance via context7 / web (Apple "visionOS Human Interface Guidelines",
> Meta "Designing for Hands" / Meta Spatial design docs) rather than asserting numbers.

## Core principles (stable across both systems)

- **Comfort zone first.** Place primary content roughly head-height, centred in the field of
  view, at a comfortable distance (~0.5 m for direct/poke, further for ray). Avoid forcing
  the user to look up/down or turn far. No content at the extreme periphery.
- **Reach matches input.** Direct **poke** for in-reach panels; **ray/pinch** for distant
  ones; **gaze + pinch** (visionOS-style) where supported. Match the affordance to the
  distance — don't make users poke a panel 3 m away.
- **Legibility at distance & over passthrough.** Text must hold up at its placement distance
  (mind angular size) and against a real-room background — strong contrast, panel backing,
  TextMeshPro, generous sizing. Ankhora renders over passthrough, so never assume a dark void.
- **Depth, not flatness.** Use real depth/layering and soft materials (visionOS glass / Meta
  panels) sparingly to separate foreground controls from background content; avoid stacking
  many transparent layers (Quest fill-rate — see `urp-shadergraph`).
- **Motion comfort.** Ease transitions, avoid large/sudden movement of UI, no UI locked
  rigidly to the head that induces nausea. Respect comfort ratings (see `horizon-store-compliance`).
- **Curved / facing layout.** Lay wide panels on a gentle curve facing the user; keep
  related controls grouped within a single comfortable arc.
- **Accessibility.** Minimum target sizes for hands, adequate contrast, don't rely on colour
  alone, give clear focus/hover feedback.

## Ankhora surfaces (apply the above to these)

- **Annotation panel** — small, anchored, world-locked, poke-near (see `world-space-annotations`).
- **Masterclass browser / step indicator** — a comfortable mid-distance curved panel; clear
  current-step affordance; minimal chrome so it doesn't fight the real room.
- **Recording controls (expert)** — reachable, unambiguous start/stop, hard to trigger by accident.

## How to use this skill

- When *designing*: produce the layout decisions (placement, distance, input mode, sizing,
  contrast) before building. Hand off construction to the `xr-ui-builder` agent.
- When *reviewing*: check an existing panel against the principles above; flag comfort,
  legibility, reach, and passthrough-contrast issues.

## Out of scope

Full theming/branding systems, 2D companion-app UI, marketing screens — not the MVP. Keep
to the in-headset surfaces the masterclass needs.

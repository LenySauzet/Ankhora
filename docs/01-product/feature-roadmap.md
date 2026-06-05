# Feature roadmap — Ankhora MVP

> The committed MVP, sliced into **buildable feature slices**. Each slice is sized to be
> **one branch / one PR** (scaffold it with `/add-feature`). Slices are ordered by
> dependency and **risk-first**. For demo-able checkpoints see
> [`../07-milestones.md`](../07-milestones.md); for scope see
> [`mvp-scope.md`](mvp-scope.md); for vocabulary see [`../06-glossary.md`](../06-glossary.md).
>
> *Last updated: 2026-06-05*

## How to use this

- Each slice is a row below: **what it delivers**, **what it depends on**, its **risk**,
  the **layer** (A/B/C from [mvp-scope](mvp-scope.md#the-plan--three-layers-one-data-model)),
  and the `.claude/` **skill** that guides its implementation.
- Build **top to bottom**. The order is dependency-correct: you cannot replay (S3) before
  the data contract exists (S2); the Player (S5) needs something to play (S3/S4).
- "Risk" is implementation uncertainty, not importance. The riskiest slice (S3, hands
  capture → replay) comes **early** on purpose, right after its prerequisite contract.
- Every slice gets EditMode tests for its logic-only core
  (skill [`unity-testability`](../../.claude/skills/unity-testability/SKILL.md)); the
  XR/scene wiring is verified by build-and-run or Meta XR Simulator.

## The slices

| # | Slice | Delivers | Depends on | Risk | Layer | Skill |
|---|---|---|---|---|---|---|
| **S1** | **Foundation XR shell** | APK builds; tracked hands; Passthrough toggle; headless build + `adb install` loop. | — | Med | B | (plan exists) `passthrough-mr`, `new-xr-interaction` |
| **S2** | **Record/replay contract** | Plain-C# serializable model (Masterclass → Chapter → Timeline → Tracks/Pins) + on-disk JSON manifest + blob layout. **The spine.** | S1 | Low–Med | B | `record-replay-contract`, `unity-testability` |
| **S3** | **Hands capture → replay** | Record XR Hands joint poses per frame to a Hands Track; replay as translucent **ghost hands**. | S2 | **High** | B | `record-replay-contract`, `urp-shadergraph` |
| **S4** | **Voice capture → replay** | Mic capture aligned to the Timeline; spatialised 3D playback synced to the hands. | S2 (S3 for sync) | Med | B | `voice-spatial-audio` |
| **S5** | **Player controls** | play/pause, scrub, rewind, **slow-motion**, **loop**, **Recenter**, **Passthrough toggle**, **Mark as complete**. Drives the one Timeline. | S3, S4 | Med | B | `xr-ui-design`, `unity-testability` |
| **S6** | **Text + Image Pins** | Instructor places Text + Image Pins in space; they appear for the Learner on their **time range**. | S2, S5 | Med | B | `world-space-annotations`, `xr-ui-design` |
| **S7** | **Masterclass shell** | Local Masterclass list, Chapter list, Completion state, and a minimal in-headset **create / record / save** menu. | S2, S5 | Med | B | `xr-ui-design` |
| **S8** | **Demo packaging** | Recorded fallback video + rehearsed live run; pull stretch (C) features only if ahead. | S5–S7 | Low | B→close | — |

## Slice notes

### S1 — Foundation XR shell *(milestone M0)*
Already fully planned in
[`../superpowers/plans/2026-05-31-ankhora-foundation-xr-shell.md`](../superpowers/plans/2026-05-31-ankhora-foundation-xr-shell.md).
This roadmap row exists so the shell sits in the same ordered list as everything that
builds on it. Output: a Quest APK with hand tracking and a testable `PassthroughController`.

### S2 — Record/replay contract *(milestone M1, part 1)*
Land this **before any capture code**. Pure C#, no `MonoBehaviour`, fully EditMode-tested:
the serializable graph plus the on-disk format (JSON manifest + binary blobs for audio and
hand poses, file refs for images). This is the format the V2 backend will later sync, so
get the shape right. Read the
[`record-replay-contract`](../../.claude/skills/record-replay-contract/SKILL.md) skill
first — it owns this schema.

### S3 — Hands capture → replay *(milestone M1, part 2)*
The **highest-risk slice** and the reason M1 is first. Capture XR Hands joint poses per
frame into the Hands Track from S2; replay them as ghost hands using a translucent URP
material. Confirm exact Meta XR Hands API signatures via context7 / Meta docs before
coding (anti-hallucination). Done when a take recorded in one run replays identically in
another — prove it with an EditMode round-trip test on the serialised poses, separately
from the on-device visual check.

### S4 — Voice capture → replay *(milestone M1, part 3)*
Microphone capture written to the Voice Track aligned to the same Timeline clock as the
hands, then spatialised playback via the Meta XR Audio spatialiser. The sync contract with
S3 is the tricky part — single shared timeline clock, not two independent ones.

### S5 — Player controls *(milestone M2)*
Everything the Learner does to the replay. Keep the control logic (timeline stepping,
speed, loop range, completion) as plain testable C# behind the UI; the spatial UI follows
the [`xr-ui-design`](../../.claude/skills/xr-ui-design/SKILL.md) language. Passthrough
toggle reuses S1's `PassthroughController`.

### S6 — Text + Image Pins *(milestone M3)*
Space-placed media with an optional time range, authored by the Instructor and rendered
for the Learner via world-space canvases. Text and Image only — Video Pin is V2/stretch.

### S7 — Masterclass shell *(milestone M4)*
The thin layer that makes the local library real and lets a Masterclass be authored and
re-opened entirely in-headset: list, chapter overview, completion, and a minimal
create/record/save authoring menu. If this slips, fall back to **Layer A** (Player-first;
team pre-authors content) — see [milestones §Fallback](../07-milestones.md#fallback-layer-a--if-authoring-slips).

### S8 — Demo packaging *(milestone M5)*
Capture the guaranteed video, rehearse the live run, and only now consider stretch (C):
Sketch, Video Pins, Cues, a 3rd Chapter, nicer menus, QR-launch.

## Out of scope (do not slice)

Anything in [mvp-scope §Out of scope](mvp-scope.md#out-of-scope-v2): auth, orgs,
marketplace, web companion + backend, custom model import, Room/Anchored Stage and real
spatial anchors, analytics, AI/RAG, multi-user. A request landing here triggers an explicit
re-scoping discussion before any slice is opened.

# Vision — Ankhora

> The long-term product vision. For the concrete, time-boxed build see
> [`01-product/mvp-scope.md`](01-product/mvp-scope.md). For exact terminology see
> [`06-glossary.md`](06-glossary.md).
>
> *Last updated: 2026-05-31*

## What Ankhora is

Ankhora turns a flat tutorial into a **spatial masterclass** — "the tutorial of
tomorrow". An expert's know-how (their voice, their hand gestures, what they point at)
is **captured once** and **replayed infinitely** for every learner, in 3D, around the
object itself.

## The problem

Training a new hire on a complex machine or skill is expensive: it mobilises an expert
who has to repeat the same gesture for every new person. Ankhora captures the expert's
demonstration **once** and lets every learner replay it on demand, in MR/VR. The expert
is mobilised a single time. This is "onboarding of the future" — and no general-purpose,
public tool does this today.

## Who it is for, and where

Two personas:

- **Instructor** — the domain expert who authors a Masterclass.
- **Learner** — the person who learns from it.

Deliberately **domain-agnostic**: a factory machine, a culinary technique, a repair, a
hobby skill. The platform engine never changes; the Marketplace categorises by domain.

## Conceptual model

The whole product rests on four bricks (full definitions in
[`06-glossary.md`](06-glossary.md)):

- **Masterclass** — the publishable unit for one skill/machine.
- **Chapter** — an ordered step; optionally holds one Model.
- **Stage** — the spatial context a Chapter plays in.
- **Tracks & Pins** — what the Instructor lays down: time-continuous **Tracks** (Voice,
  Hands) and space-placed **Pins** (Text, Image, Video), plus **Sketch** and **Cue**.

The Instructor activates only the tracks they want, chapter by chapter — so "a simple
chapter with just a 3D model and a text pin, no hands or voice" costs nothing extra.

### Stage kinds (the anchoring spectrum)

The Stage is the single decision that drives architecture and feasibility. The model is
designed so the Stage is **swappable** — adding a new kind is additive, not a rewrite:

- **Model Stage** — a local play-space frame, optionally holding an imported 3D model;
  content anchors trivially to it. Works anywhere, no physical object, no anchoring risk.
  **The MVP stage.**
- **Room Stage** — a simple Quest room scan (Meta Scene API / MRUK, **not** photoreal
  Gaussian-splat / Hyperscape) used as the spatial reference. The Instructor scans the
  space once; a Learner physically in that same space loads the Masterclass aligned to
  the real machines. **V2.**
- **Anchored Stage** — content registered onto a real object via a spatial Anchor / a
  printed QR on the machine. The purest "on the real machine" experience, the most
  finicky to make reliable. **V2.**

## The ideal flows

Tagged **[MVP]** / **[V2]**. The MVP keeps only the core spine; everything else is parked
without breaking that spine.

### Instructor — "new machine → published Masterclass"

1. **Identity** — authenticate, belong to an Organization. **[V2]** (MVP: single local
   user, no auth.)
2. **Create a Masterclass** — title, description, category, Visibility, cover. **[V2 for
   the metadata]** (MVP: local, title only.)
3. **Add a Chapter** — name it; optionally attach a Model (from a bundled library). **[MVP]**
4. **Record the Chapter** — perform the gesture while narrating: **Voice + Hands captured
   together in one take**; drop **Pins** (Text/Image); optionally **Sketch**; **Cues**
   auto-derive from Pin order. Re-record a bad take. **[MVP core; Sketch & Cue are stretch]**
5. **Preview** as a Learner would (switch to the Player). **[MVP nice-to-have]**
6. **Order & finalise** chapters. **[MVP-light]**
7. **Publish / share** — set Visibility, publish to the Marketplace or share in the
   Organization, generate a **QR launch**. **[V2]** (MVP: local save.)

### Learner — "receive → learn"

1. **Receive / discover** — Marketplace, QR launch, or Org assignment. **[V2]** (MVP: the
   Masterclass is already on the device; pick it from a local list.)
2. **Overview** — title, Chapter list, Completion progress. **[MVP-light]**
3. **Enter a Chapter → the Player** — Stage loads (Model in front, if any); **Recenter**
   and **Passthrough** available any time; **Hands + Voice** replay in sync; **Pins**
   appear on their time range; **Cues** guide attention. **Player controls**: play/pause,
   scrub, rewind, slow-motion, loop. **[MVP]**
4. **Follow the gesture** — mimic, slow down, loop; an Image/Video Pin close-up fills in
   where hands alone aren't precise enough. **[MVP]** (Active pose validation is **[V2]**.)
5. **Mark & advance** — Mark as complete → next Chapter. **[MVP]**
6. **Finish** — completion state; certificate, analytics, ask-AI. **[V2]**

## Architecture posture

- **MVP — no backend.** The app is a single Unity client on Quest 3. A Masterclass is
  persisted **on-device**: a JSON manifest (structure: masterclass → chapters →
  tracks/pins, ordering, model references) plus binary blobs (voice audio; hand poses per
  frame; images). Models are bundled in the build. The "database" is the device
  filesystem.
- **V2 — the platform layer.** A **web app + backend**: an API + a relational database
  (users, orgs, catalogue, visibility) + object storage for the heavy binaries. The Quest
  client authenticates, browses the Marketplace, downloads a Masterclass to play, and
  uploads what it records. The web app is where Instructors import their own models and
  manage metadata — the tasks that are painful in a headset.
- **Continuity:** the MVP's local format (manifest + blobs) **becomes** the V2 synced
  format. The same files later flow through an API instead of living only on-device — so
  the MVP carries no rework debt.

## Roadmap beyond the MVP (V2+)

MR modes (Room Stage, Anchored Stage, real spatial anchors, QR launch), accounts /
Organizations / Marketplace, the web companion + backend, custom model import, instructor
analytics, AI/RAG learner assistant, subtitle auto-translation, active pose validation,
certificates, multi-user / co-located sessions.

## Nature of the project

Ankhora is an **Epitech MSc Pro school / experimentation project**. It has **no link** to
any author's employer or commercial product; any AI/RAG direction is a generic V2
exploration, not tied to a specific company's stack.

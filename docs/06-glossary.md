# Glossary — Ankhora vocabulary

> The canonical lexicon for talking about Ankhora precisely. Every feature, doc, and
> code symbol should use these terms. When a term maps to a Unity concept, the note
> column flags the collision we deliberately avoid.
>
> *Last updated: 2026-05-31*

## Content structure

| Term | Definition | Note |
|---|---|---|
| **Masterclass** | The complete, publishable unit teaching **one** skill or machine. Domain-agnostic (a recipe, a repair, a machine procedure). Owned by an Instructor, has a Visibility. | Chosen over "Course" — carries the "learn from an expert" promise; "Course" sounds academic and undersells. |
| **Chapter** | An ordered segment (a step) of a Masterclass. A Masterclass is an ordered list of Chapters. A Chapter **optionally** holds one Model; there is no "chapter type". | Preferred over "Module"/"Step" — book/video metaphor everyone understands. |
| **Stage** | The spatial context a Chapter plays in. Kinds: **Model Stage** (a local play-space frame, optionally holding a 3D model — MVP), **Room Stage** (a Quest room scan — V2), **Anchored Stage** (a real object via spatial Anchor / QR — V2). | Deliberately **not** "Scene": Unity already owns "Scene", which would guarantee confusion in code. |
| **Timeline** | The time axis of a Chapter, along which Tracks play and Pins appear. | Drives the Player. |

## Content placed inside a Chapter

| Term | Definition | MVP status |
|---|---|---|
| **Track** | A **time-continuous** recording on the Chapter Timeline. MVP tracks: **Voice Track**, **Hands Track** (ghost hands). | MVP — core |
| **Pin** | A media item **placed in 3D space** (position + optional time range): **Text Pin**, **Image Pin**, **Video Pin**. | Text + Image: MVP. Video: V2 / stretch. |
| **Sketch** | Instructor-drawn 3D strokes / shapes used to highlight. | Stretch (C) |
| **Cue** | An attention guide (arrow / comet) that directs the Learner's gaze to the next Pin or action. Auto-derived from Pin order. | Stretch (C) |
| **Anchor** | **Reserved** for the Meta technical concept: registering virtual content onto a real-world location. Barely used in MVP (VR); mostly V2 (MR). | V2 |

> **Pin vs Anchor:** placed media are **Pins**, never "anchors", so the word **Anchor**
> stays free for the Meta spatial-anchor concept when MR arrives. This avoids a naming
> collision the moment we go to MR.

## Playback, personas, distribution

| Term | Definition | MVP status |
|---|---|---|
| **Instructor** | The persona who authors a Masterclass (creator / expert). | MVP |
| **Learner** | The persona who consumes a Masterclass (trainee / new hire). | MVP |
| **Player** | The Learner's playback surface: play/pause, **scrub** the Timeline, rewind, **slow-motion**, **loop** a segment, **Recenter**. | MVP — core |
| **Recenter** | A Player action that re-places the Stage in front of the Learner (comfort). | MVP |
| **Passthrough** | A **Learner-side runtime toggle** (not a Chapter property) that shows the real world behind the virtual content instead of a virtual background. Enables MR use cases (a recipe in your real kitchen). Content renders identically; only the background changes. | MVP |
| **Completion** | The Learner marks a Chapter "complete" (no validation). Progress = Chapters completed. | MVP |
| **Organization** | A group of users (a company / team) that scopes private content. | V2 |
| **Library** | The set of Masterclasses accessible to a user (created + acquired + org). | V2 (MVP: a flat local list) |
| **Marketplace** | The public discovery surface for Masterclasses. | V2 |
| **Visibility** | A Masterclass's access scope: **Private / Organization / Public**. | V2 |
| **QR launch** | Scanning a QR code opens a specific Masterclass. | V2 (MVP: optional experiment) |

## Chapter anatomy — what is authored vs what the Learner controls

A **Chapter is authored content.** The Instructor sets exactly two things:

- an **optional Model** — a 3D model placed at the centre of the Stage (or nothing);
- a **Timeline** carrying **Tracks** (Voice, Hands) and **Pins** (Text, Image, …), plus
  optional **Sketch** and **Cue**.

That is the entire Chapter. Two things are deliberately **not** part of it:

- **Stage kind** is not a per-Chapter choice in the MVP: every Chapter uses the **Model
  Stage** (the local play-space frame, optionally holding the Model). Room Stage and
  Anchored Stage are V2; adding a kind is additive, never a rewrite.
- **Background (VR vs Passthrough)** is **not authored** and has **no effect on the
  Chapter**. It is a **Learner runtime toggle** in the Player: the same Chapter can be
  viewed against a virtual void (VR) or the real world (Passthrough), switchable at any
  time. The Instructor never sets it.

So neither "Stage" nor "Background" is a *type* you stamp on a Chapter. A Chapter only
ever differs by **whether it has a Model**. The table below shows that single authored
difference, plus the Background the Learner *typically* picks for each case — a Learner
choice, not a Chapter setting:

| Use case | Chapter has a Model? | Learner usually views in |
|---|---|---|
| Machine procedure, no physical machine present | Yes | VR |
| Cooking recipe in your real kitchen | No | Passthrough |
| 3D model placed in your real room | Yes | Passthrough |

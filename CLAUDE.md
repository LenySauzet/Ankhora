# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Ankhora

Ankhora is an XR platform for Meta Quest 3 that lets domain experts record immersive spatial "masterclasses" (voice + hand tracking + spatial anchors + annotations) and lets learners replay them as step-by-step guided training in MR. Target industries: manufacturing and culinary arts. A web-side RAG chatbot configurator and a marketplace are planned for V2.

The project is currently in **spec / planning phase**. No feature code has been written. The Unity project is a fresh URP scaffold awaiting Meta XR SDK installation.

## Team & timeline (hard constraints)

- **3-person Epitech MSc Pro group project** (`T-VIR-902-MPL_2`).
- **Working days**: Thursdays + Fridays only, over ~2 months. After meetings / follow-ups, effective coding time is **~10–14 working days max**. The MVP must be ruthlessly small.
- **Dev machines**:
  - **Lény (you, Claude Code): Mac M4 Pro.** No Quest Link / Air Link (Windows-only). Iteration loop is `Build APK → adb install` or **Meta XR Simulator** (Apple Silicon native). Hand tracking inside Unity Editor Play Mode is not available on Mac.
  - **Other two teammates: Windows.** They CAN use Quest Link for fast in-Editor iteration. Treat them as the primary "hands-on-headset live-iterate" station; the Mac station optimises for spec, architecture, build pipeline, and bursts of build-and-run on device.
- **Repositories** (dual-repo workflow):
  - **Working repo (PRs land here)**: `LenySauzet/Ankhora` (public, GitHub).
  - **Epitech submission repo (mirror)**: `EpitechMscProPromo2026/T-VIR-902-MPL_2`. Every merge to `main` on the working repo must be auto-mirrored to the Epitech repo via CI. Do **not** push directly to the Epitech repo by hand.

## Authoritative project context

Read these before any substantive work, in this order:

1. **[research/xr-platform-master-research.md](research/xr-platform-master-research.md)** — the master research dossier (fact-checked May 2026, 33 sources, 19 verified claims, 6 explicitly refuted). Covers Mac → Quest 3 dev pipeline, Meta XR SDK choices and gotchas, MCP / AI tooling for Unity, spec-driven dev framework comparison, argued MVP / V2 split, and a 2-week setup plan. **This is the canonical knowledge base — defer to it on any XR / Unity / Quest 3 / tooling question.** The dossier was originally written assuming solo dev; the team & timeline constraints above override any solo-dev framing inside it (especially the MVP scope, which must shrink further).
2. **[AGENTS.md](AGENTS.md)** — currently a stub. Cursor's `.cursor/rules/general.mdc` points to it as the canonical agent context. Keep AGENTS.md and CLAUDE.md aligned as content is added; the simplest pattern is to have AGENTS.md include a single line `Read @CLAUDE.md for project context.`
3. **[README.md](README.md)** — one-line project description.

## Current technical state

- **Unity Editor**: `2022.3.62f3` LTS (Apple Silicon on Lény's machine; teammates use the same version on Windows).
- **Render pipeline**: URP `14.0.12`.
- **XR**: not yet wired. `Packages/manifest.json` has no Meta XR SDK, no OpenXR plugin, no XR Interaction Toolkit, no XR Hands. Setup will follow the dossier §1.4.
- **Scenes**: only `Assets/Scenes/SampleScene.unity` (URP template default).
- **Assets**: only the URP `TutorialInfo/`, `Settings/`, and a `Readme.asset` carried over from the URP 3D template.
- **Git**: repo is initialised on GitHub (`LenySauzet/Ankhora`) but the local working tree is **not yet `git init`-ed**. First-init steps must include staging `.gitignore` + creating `.gitattributes` for Git LFS + Unity line-ending normalisation before any binary asset commit.

> The dossier discusses Unity 6 LTS as a valid option. This project stays on Unity 2022.3 LTS unless a dedicated ADR documents a migration — all three machines must agree on the exact Editor version, version mismatch is a frequent cause of avoidable scene/prefab diffs.

## Development environment constraints

These are hard constraints, not preferences:

- **Meta Quest Link / Air Link** are Windows-only. On Mac, iteration uses Build & Run on device or Meta XR Simulator. Windows teammates may use Link freely. Do not suggest Mac workflows that depend on Link.
- **Hand tracking inside Unity Editor Play Mode** is not possible on Mac (Link-only feature). Lény tests hand tracking via Meta XR Simulator or build & run on device; Windows teammates test it in Editor via Link.
- **Tooling in active use**: Warp (terminal, Mac), Cursor (IDE, all 3), Claude Code (all 3), Blender (3D, Lény is learning), Meta Quest Developer Hub (sideload + perf monitoring, cross-platform).

## Conventions

- **Language**: all code, comments, file content, and agent output in **English**. (Source: `.cursor/commands/example.md`.) The team communicates in French; deliverables stay in English.
- **C# style**: standard Unity conventions — `PascalCase` types, `camelCase` fields, `[SerializeField] private` preferred over public fields.
- **Asset Serialization Mode**: Force Text (required for diffable scenes/prefabs in a 3-person team). Verify in `Edit > Project Settings > Editor`.
- **XR features**: once the Meta XR SDK is installed, **prefer Meta Building Blocks + Project Setup Tool** over manual rigging. The dossier flags this as the highest-leverage efficiency move.
- **Hand tracking interactions**: must go through Meta's **Interaction SDK**. Bypassing it risks Meta Horizon Store rejection.
- **Commits**: **Conventional Commits** format (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`, etc.). Lény has a `git-commit` Claude Code skill that auto-generates conformant messages from staged diffs.
- **Branches**: short-lived feature branches (`feat/<scope>-<slug>`, `fix/<slug>`) merged into `main` via PR. No direct push to `main`.
- **Pull Requests**: every PR requires (1) at least one human review, (2) green CI (build + AI code review). Merge strategy: **squash and merge** (keeps `main` history linear and conventional-commit-friendly).
- **Cursor commands**: kebab-case filenames, no YAML frontmatter, written in English. Template at `.cursor/commands/example.md`.
- **Architectural decisions** of consequence are recorded as ADRs under `docs/02-architecture/adr/`. Until that folder exists, document trade-offs as new files under `research/`.

## What "build" / "run" / "test" mean today

- **Build**: not yet operational — Android Build Support module not yet installed, no XR plugin, no Quest target configured. Setting this up is the first dev task per dossier §1.4 and the 2-week plan.
- **Run on device**: once the build is operational, the loop is `adb install path/to/build.apk` over USB (Mac & Windows).
- **Test**: `com.unity.test-framework` is in the manifest but no test scripts exist yet. Runtime tests will live under `Assets/Tests/` when added.

Replace this section with concrete commands once the build pipeline is wired.

## Out of scope (MVP — ultra-thin given the timeline)

Per the dossier §3.4 *and* the team-timeline constraint above, these are **not** in the MVP and any request that falls in this bucket should trigger an explicit re-scoping discussion before any implementation:

- RAG chatbot in-headset
- Marketplace + payment
- QR-code launcher (custom Platform SDK deep-link flow)
- Multi-user / co-located sessions
- MDM enterprise integration (ArborXR / ManageXR / Meta Quest for Business)
- Multi-industry customization layer
- Cross-session anchor persistence at scale (multi-room, fleet-wide)

**MVP candidate target (to confirm with the team)**: one short masterclass (≤ 2 min) recorded by an expert (voice + ghost hands + 1 text annotation + 1 spatial anchor), replayed by a learner. Distribution = side-loaded APK on the team's Quest 3.

## Cursor configuration

- `.cursor/rules/general.mdc` — `alwaysApply: true`, redirects to `AGENTS.md`.
- `.cursor/commands/example.md` — slash-command template (kebab-case, no frontmatter, English).
- `.cursorignore` — Unity excludes (`Library/`, `Temp/`, `Logs/`, build outputs, IDE files, addressables artifacts, `.DS_Store`).
- `.gitignore` — Unity + XR Interaction Toolkit standard, already in place.

## Living document

This file is a living source of truth. When you (Claude) learn something durable about the project — a workflow decision, a constraint discovered, a convention agreed on — update this file directly and explain the diff in your final message. Do not create separate "notes" or "decisions" files for that purpose; either update CLAUDE.md inline or, if it's a significant architectural call, add an ADR under `docs/02-architecture/adr/`.

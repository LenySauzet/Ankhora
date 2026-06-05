# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Ankhora

Ankhora is an XR platform for Meta Quest 3 that lets domain experts record immersive spatial "masterclasses" (voice + hand tracking + spatial anchors + annotations) and lets learners replay them as step-by-step guided training in MR. Target industries: manufacturing and culinary arts. A web-side RAG chatbot configurator and a marketplace are planned for V2.

The project is currently in **spec / planning phase**. No feature code has been written yet, but the **dev environment is now wired**: the project has migrated to Unity 6, the Meta XR SDK is installed, and Claude Code is connected to the Unity Editor over MCP. See *Current technical state* and *AI tooling — Claude Code ↔ Unity (MCP)* below.

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

- **Unity Editor**: `6000.4.10f1` (Unity 6, Apple Silicon on Lény's machine; all three machines must install the **same exact** version). Migrated in place from `2022.3.62f3` on branch `chore/claude-unity-integration` — the migration ADR is still pending (see below).
- **Render pipeline**: URP `17.4.0` (was `14.0.12`).
- **XR**: **wired.** `Packages/manifest.json` now has:
  - `com.meta.xr.sdk.all` `201.0.0` — the all-in-one Meta XR SDK (Core, Interaction, Audio, Acoustics, Anchors, etc.). Bundles Meta's **Interaction SDK**, so a separate `com.unity.xr.interaction.toolkit` is not installed.
  - `com.meta.xr.unity-mcp.extension` (from `github.com/meta-quest/Unity-MCP-Extensions`) — the **Meta XR MCP Extension**; adds the `meta_*` building-block tools over the Unity MCP transport.
  - `com.unity.xr.openxr` `1.17.1` — OpenXR plugin.
  - `com.unity.ai.assistant` `2.10.0-pre.1` — Unity's native AI Assistant + **native MCP server** (the relay Claude Code connects to).
  - Quest config assets generated under `Assets/Oculus/` (`OculusProjectConfig.asset`) and `Assets/Resources/` (MetaXR audio/acoustics/runtime settings). XR loader settings under `Assets/XR/`.
- **Scenes**: only `Assets/Scenes/SampleScene.unity` (URP template default — no XR rig added to it yet).
- **Assets**: URP `TutorialInfo/` + `Settings/` + `Readme.asset` from the template, plus the Meta XR `Oculus/`, `Resources/`, `Plugins/`, `XR/` folders generated during setup.
- **Git**: repo is `git init`-ed and pushed to `LenySauzet/Ankhora`. `.gitattributes` (Git LFS + line-ending normalisation) and `.gitignore` are in place.

> **Unity version note:** the dossier and the old CLAUDE.md pinned Unity 2022.3 LTS. This project has since migrated to **Unity 6** (driven by the Claude/Unity integration work). The migration must still be formalised in a dedicated ADR under `docs/02-architecture/adr/`. All three machines must agree on the exact Editor version — version mismatch is a frequent cause of avoidable scene/prefab diffs.

## Development environment constraints

These are hard constraints, not preferences:

- **Meta Quest Link / Air Link** are Windows-only. On Mac, iteration uses Build & Run on device or Meta XR Simulator. Windows teammates may use Link freely. Do not suggest Mac workflows that depend on Link.
- **Hand tracking inside Unity Editor Play Mode** is not possible on Mac (Link-only feature). Lény tests hand tracking via Meta XR Simulator or build & run on device; Windows teammates test it in Editor via Link.
- **Tooling in active use**: Warp (terminal, Mac), Cursor (IDE, all 3), Claude Code (all 3, now MCP-connected to the Unity Editor — see below), Blender (3D, Lény is learning), Meta Quest Developer Hub (sideload + perf monitoring, cross-platform).

## AI tooling — Claude Code ↔ Unity (MCP)

Claude Code drives the Unity Editor live over a **single MCP connection** (`unity-mcp`) that exposes **two tool families**:

- **Unity native MCP** (`Unity_*`) — from `com.unity.ai.assistant`. Editor/scene/asset/script/console operations: `Unity_GetProjectData`, `Unity_ReadConsole`, `Unity_ManageScene`, `Unity_ManageGameObject`, `Unity_ManageScript`, `Unity_CreateScript`, `Unity_RunCommand`, scene/camera capture, etc.
- **Meta XR MCP Extension** (`meta_*`) — from `com.meta.xr.unity-mcp.extension`. Quest-specific building blocks: `meta_add_camerarig`, `meta_add_interactionrig`, `meta_add_grabbable`, `meta_add_distance_grabbable`, `meta_add_canvas_interaction_poke` / `_ray`, `meta_add_teleport_hotspot`, `meta_update_android_manifest`, `meta_get_config_information`, `meta_get_interactors_state`.

**Always call `meta_get_config_information` once before using any `meta_*` tool** — it returns the live config layout (OculusProjectConfig, OVRManager fields) needed to set the right values.

How the transport works (per-OS, per-user — **not** committed as a project `.mcp.json`):

- Unity 6 drops a relay binary under `~/.unity/relay/` when the Editor starts; the MCP client launches it with `--mcp` and it bridges to the running Editor. **The bridge only runs while the Editor is open.**
- Each dev registers it locally:
  - **macOS (Apple Silicon):** `claude mcp add unity-mcp -s local -- "$HOME/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64" --mcp`
  - **Windows:** `claude mcp add unity-mcp -s local -- "%USERPROFILE%\.unity\relay\relay_win.exe" --mcp`
- Then restart Claude Code (servers added mid-session don't hot-load), and approve the pending client in `Project Settings → AI → Unity MCP Server → Connected Clients`. Tools are toggled in that same panel.
- Full setup + troubleshooting: [`docs/05-operations/setup-unity6-claude-integration.md`](docs/05-operations/setup-unity6-claude-integration.md).

**Verify the connection** by calling `Unity_GetProjectData` (Unity side) and `meta_get_config_information` (Meta side) — both must return `success: true`. What still requires a real headset (on-device build & run, hand-tracking validation) stays manual; MCP does not change that.

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
- **Architectural decisions** of consequence are recorded as ADRs under `docs/02-architecture/adr/` (the folder now exists, with a template `0000-adr-template.md` and `0001-unity6-migration.md`). Add new ones with the `/write-adr` command.
- **Formatting** is encoded once in `.editorconfig` at the repo root (C# naming, UTF-8, LF, final newline), mirroring `.cursor/rules/001-unity-conventions.mdc`. Document-grade only — no csharpier/pre-commit enforcement is wired.

## What "build" / "run" / "test" mean today

- **Build**: XR packages (Meta XR SDK + OpenXR) are now installed, but a full Quest APK build has **not yet been verified end-to-end** on the Mac station. Still to confirm before claiming "build works": Android Build Support module installed in the Unity 6 Editor, platform switched to Android, OpenXR/Meta feature groups enabled for Android, IL2CPP + ARM64. **The Quest APK is built locally on device, NOT in CI** — see the next point.
- **CI does not build the APK.** Meta XR SDK 201.0.0's `OVRProjectConfig` static ctor throws on a headless Linux editor (OVRPlugin reports no version → `Enumerable.Range(200, 60-200+1)` has a negative count → `ArgumentOutOfRangeException` during `BuildPlayer`). It is a Meta SDK bug, deterministic on GameCI's Linux runner, unfixable project-side, and 201.0.0 is the latest published SDK. So `ci.yml` runs **`unity-test-runner` (EditMode)** instead — it compiles the whole project and runs tests without `BuildPlayer`. The same cctor exception still spams the CI log but is non-fatal there. Reintroduce an APK build on a **Windows** runner (where OVRPlugin loads) if/when Meta fixes the Linux path.
- **Run on device**: once a local build succeeds, the loop is `adb install path/to/build.apk` over USB (Mac & Windows).
- **Test**: `com.unity.test-framework` `1.6.0` is in the manifest. EditMode tests live under `Assets/Tests/EditMode/` (currently a single smoke test that gates compilation in CI). Add real coverage there as gameplay code lands.

Replace the build/run lines with concrete, verified commands once a Quest APK has been built and sideloaded successfully.

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

## Project Claude Code layer (`.claude/`)

Committed, team-shared Claude Code config — all three devs and `claude-review.yml` get it.
Intentionally small and curated (anchored to real Ankhora needs), with `CLAUDE.md` +
`.cursor/rules/*.mdc` as the single source of truth; `.claude/` files stay thin and point
back here rather than restating conventions. See `.claude/README.md` for the full inventory
and the command/skill/agent distinction. It adds **no** new MCP server — everything runs over
the existing official `unity-mcp` relay (`Unity_*` + `meta_*`); we deliberately did not adopt
a second Unity-control bridge.

The roster covers Ankhora's domains and the agents **compose into teams**
(`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS` is on). Full inventory + the team-composition diagram
+ roadmap live in `.claude/README.md`. Summary:

- **Commands (4)** (`.claude/commands/`): `/build-android` (Quest APK, Unity 6, Mac reality),
  `/write-adr`, plus `/add-feature` and `/mirror-epitech` (thin pointers to the canonical
  `.cursor/commands/*` so Cursor and Claude don't drift).
- **Skills (10)** (`.claude/skills/`): `new-xr-interaction`, `spatial-anchors`, `passthrough-mr`,
  `voice-spatial-audio`, `record-replay-contract` (the core data model — the spine),
  `world-space-annotations`, `xr-ui-design` (visionOS/Meta Spatial design language),
  `urp-shadergraph`, `unity-testability`, plus the shared `git-commit` workflow skill (promoted
  to project scope so all collaborators get the Conventional Commits practice). Every XR skill
  tells Claude to confirm exact Meta API signatures via context7/Meta docs before coding
  (anti-hallucination).
- **Agents (7)** (`.claude/agents/`): `masterclass-author` (team lead — composes the others),
  `xr-scene-builder`, `xr-ui-builder`, `unity-test-author`, `quest-perf-reviewer` (read-only),
  `horizon-store-compliance` (read-only), `xr-build-doctor`.
- **Canonical team:** `masterclass-author` → `unity-test-author` (logic+tests first) →
  `xr-scene-builder` + `xr-ui-builder` → `quest-perf-reviewer` + `horizon-store-compliance`.

> Parity note: `.cursor/commands/build-android.md` and `.claude/commands/build-android.md` are
> both current (Unity 6 `6000.4.10f1`). Keep them in sync when the build pipeline changes.

## Living document

This file is a living source of truth. When you (Claude) learn something durable about the project — a workflow decision, a constraint discovered, a convention agreed on — update this file directly and explain the diff in your final message. Do not create separate "notes" or "decisions" files for that purpose; either update CLAUDE.md inline or, if it's a significant architectural call, add an ADR under `docs/02-architecture/adr/`.

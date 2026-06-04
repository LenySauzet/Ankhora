# Setup — Unity 6 migration + Claude Code / Unity integration

> Tracking doc for PR `chore/claude-unity-integration`. This is the live checklist for the
> dev-environment work; tick items as they land. It will be deleted (or folded into a
> permanent doc) once the setup is stable.
>
> *Created: 2026-06-04*

## Scope of this PR

Two coupled pieces of dev-environment work, done together because the Claude-in-Unity
integration needs Unity 6:

1. **Migrate the project from Unity `2022.3.62f3` to Unity 6** (in place — see below).
2. **Wire Claude Code to Unity** (Unity MCP) and stand up a project-local `.claude/`
   (skills, commands, hooks, agents) for AI-assisted development.

This PR is **not** about product features. The MVP product spec lives in its own PR
(`docs/mvp-spec`, PR #12); the implementation plans are separate.

## Decisions

- **Unity 6, team-aligned.** All three machines (Lény on Mac, two teammates on Windows)
  move to the **same exact** Unity 6 version. Version mismatch across machines causes
  avoidable scene/prefab diffs, so the pinned version is a hard constraint.
- **In-place upgrade, not recreate.** The repo carries a lot of non-Unity scaffolding
  (docs, CI, `.cursor/`, git history) and almost no Unity content (empty URP template).
  Recreating in a fresh folder risks losing that scaffolding. Instead: open the existing
  project in Unity 6, let it upgrade the template, commit the result. Reversible via git.
- **An ADR will record the migration.** Per `CLAUDE.md`, leaving Unity 2022.3 requires a
  dedicated ADR. It will be written **after** the migration is confirmed working, under
  `docs/02-architecture/adr/`.

## Checklist

### Migration to Unity 6
- [ ] Pin the exact Unity 6 version (the LTS/recommended `6000.x` shown in Unity Hub); all 3 machines install it.
- [ ] Open the project in Unity 6 (in place), let the upgrader run, verify the empty scene still opens.
- [ ] Commit the upgrade diff (ProjectSettings, packages, URP assets, ProjectVersion.txt).
- [ ] Verify Meta XR SDK ↔ Unity 6 version pairing (do **before** relying on it). *(deferred — awaiting go-ahead)*
- [ ] Write the migration ADR. *(deferred — after migration confirmed)*

### Claude Code ↔ Unity integration
- [ ] Install + verify a Unity MCP bridge (dossier recommends `CoplayDev/unity-mcp`) in Claude Code.
- [ ] Confirm whether a dedicated "Meta XR MCP" actually exists (claim from a video summary — unverified).
- [ ] (Optional) Unity AI Assistant package `com.unity.ai.assistant` — Unity-native AI, Unity 6 only. Distinct from Claude Code.

### Project-local `.claude/`
- [ ] `.claude/skills/` — project skills (e.g. `unity-editor-ops`, `quest-build-run`, `meta-building-blocks`).
- [ ] `.claude/commands/` — e.g. `/build-android`, `/run-editmode-tests`, `/sideload`.
- [ ] `.claude/agents/` — a Unity-MCP implementer agent.
- [ ] `.claude/settings.json` hooks — e.g. run EditMode tests on C# change, format C#.

### Doc updates (after migration lands, ideally after PR #12 merges)
- [ ] Update version refs `2022.3.62f3` → Unity 6 in `CLAUDE.md`, the research dossier, and the Foundation plan.
- [ ] Drop the "stays on Unity 2022.3" note in `CLAUDE.md` (superseded by the ADR).

## Notes / clarifications

- **Claude Code integration does not require Unity 6.** The Unity MCP works on 2022.3 too.
  Unity 6 is wanted here for modernisation + the Unity AI Assistant package; the two
  topics are independent.
- **What needs a headset stays manual.** Even with Unity MCP, on-device build & run and
  hand-tracking verification require the Quest (or, partially, the Meta XR Simulator).

## Waiting on

Lény is following Valem's tutorials (Claude-in-Unity, then XR) and will hand back a recap
of what got set up. After that: verify Meta XR ↔ Unity 6, write the ADR, build out
`.claude/`, and update the version references.

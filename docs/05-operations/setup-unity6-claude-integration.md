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
- [x] Pin the exact Unity 6 version — **`6000.4.10f1`** (`ProjectSettings/ProjectVersion.txt`); all 3 machines install it.
- [x] Open the project in Unity 6 (in place), let the upgrader run, verify the empty scene still opens. URP upgraded `14.0.12` → `17.4.0`.
- [ ] Commit the upgrade diff (ProjectSettings, packages, URP assets, ProjectVersion.txt). *(changes present in working tree, not yet committed)*
- [x] Verify Meta XR SDK ↔ Unity 6 version pairing — `com.meta.xr.sdk.all` `201.0.0` installed and resolving on Unity 6; `meta_*` MCP tools respond.
- [ ] Write the migration ADR. *(still deferred — after the upgrade diff is committed)*

### Claude Code ↔ Unity integration
- [x] Connect Claude Code to Unity 6's **native** MCP Server (`com.unity.ai.assistant`) — supersedes the community `CoplayDev/unity-mcp`. Registered as `unity-mcp` (relay `relay_mac_arm64`, local scope), transport connected, `Unity_GetProjectData` returns `success: true`.
- [x] **Confirmed: a dedicated Meta XR MCP exists.** It is the package `com.meta.xr.unity-mcp.extension` (`github.com/meta-quest/Unity-MCP-Extensions`), installed in `manifest.json`. It extends the **same** `unity-mcp` relay with `meta_*` building-block tools (camera rig, interaction rig, grabbable, canvas interaction, teleport, android manifest). Verified: `meta_get_config_information` returns `success: true`. Not a separate MCP server — one transport, two tool families.
- [x] Unity AI Assistant package `com.unity.ai.assistant` `2.10.0-pre.1` installed (it is what ships the native MCP server). Distinct from Claude Code.

### Project-local `.claude/`
*(not started — no project-local `.claude/` directory exists yet)*
- [ ] `.claude/skills/` — project skills (e.g. `unity-editor-ops`, `quest-build-run`, `meta-building-blocks`).
- [ ] `.claude/commands/` — e.g. `/build-android`, `/run-editmode-tests`, `/sideload`.
- [ ] `.claude/agents/` — a Unity-MCP implementer agent.
- [ ] `.claude/settings.json` hooks — e.g. run EditMode tests on C# change, format C#.

### Cleanup
- [x] Delete the empty duplicate folders Unity created during XR setup: `Assets/XR 1`, `Assets/XR 2` (+ their `.meta`), and `Assets/XR/Settings 1`. Done 2026-06-04 — confirmed empty, their GUIDs had zero references project-wide, removed folder + `.meta` together.

### Doc updates (after migration lands, ideally after PR #12 merges)
- [x] Update version refs `2022.3.62f3` → Unity 6 in `CLAUDE.md` (done 2026-06-04). *(research dossier + Foundation plan still reference 2022.3 — pending)*
- [x] Drop the "stays on Unity 2022.3" note in `CLAUDE.md` — replaced with a "migrated to Unity 6, ADR pending" note.

## Unity MCP — connecting Claude Code (verified 2026-06-04)

Unity 6 ships a **native MCP server** (package `com.unity.ai.assistant`, panel
`Project Settings → AI → Unity MCP Server`). It supersedes the community
`CoplayDev/unity-mcp` from the dossier — we use the native one.

How it works: Unity installs a **relay binary** under `~/.unity/relay/` when the editor
starts; the MCP client launches it with `--mcp` and it bridges to the running editor.

**The relay path is per-OS and per-user, so it is NOT committed as a project `.mcp.json`.**
Each developer registers it **locally** with the command for their platform:

- **macOS (Apple Silicon):**
  ```bash
  claude mcp add unity-mcp -s local -- "$HOME/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64" --mcp
  ```
- **Windows:**
  ```bash
  claude mcp add unity-mcp -s local -- "%USERPROFILE%\.unity\relay\relay_win.exe" --mcp
  ```

Then:
1. **Restart Claude Code** (start a fresh session) so it loads the Unity tools — servers added mid-session are not hot-loaded.
2. **Approve in Unity:** `Project Settings → AI → Unity MCP Server` → accept the pending Claude Code connection (it appears under *Connected Clients*). Previously approved clients reconnect automatically.
3. **Test:** ask Claude *"Read the Unity console and summarize any warnings or errors"* → it should call `Unity_ReadConsole`.

Keep the Unity editor open — the bridge only runs while the editor is running. More tools
can be enabled in the panel under **Tools (N of 52 enabled)**.

Source: Unity docs — *Get started with Unity MCP* (`com.unity.ai.assistant`).

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

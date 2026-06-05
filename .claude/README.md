# `.claude/` — Ankhora's Claude Code layer

Committed, team-shared Claude Code configuration. All three devs (and the
`claude-review.yml` GitHub Action) get these automatically. The goal: make Claude Code a
**first-class contributor to Ankhora across every domain of the app** — XR interaction,
spatial anchors, passthrough/MR, voice, the record/replay core, spatial UI, shaders, build,
perf, tests, and Store compliance — with specialised agents that **compose into teams**.

Curated, not maximal: every file is anchored to a real Ankhora need. `CLAUDE.md` +
`.cursor/rules/*.mdc` remain the single source of truth; `.claude/` files stay thin and
point back rather than restating conventions.

## The three mechanisms

| Mechanism | Folder | What it is | Use it for |
|-----------|--------|------------|------------|
| **Command** | `commands/` | A user-triggered shortcut (`/build-android`). Main context. | A repeated, procedural action |
| **Skill** | `skills/` | Know-how injected into the *main* context — Claude does the work, following the recipe. | "How we do X in Ankhora" |
| **Agent** | `agents/` | A **sub-agent** with its own isolated context + restricted tools, dispatched for a heavy task. Preserves the main context and composes into teams. | Scene/UI build, review, test generation, orchestration |

Skills and agents pair up: a skill is the *recipe*, an agent is an *executor* that applies it
in isolation (e.g. the `xr-ui-design` skill ↔ the `xr-ui-builder` agent).

## Convention for every XR skill/agent

The Meta XR SDK API surface changes across versions (`com.meta.xr.sdk.all` 201.0.0). Every XR
skill tells Claude to **confirm exact API signatures via context7 / Meta docs before coding** —
never invent class/method names. This is `CLAUDE.md`'s anti-hallucination rule, baked in.

## Inventory

### Commands (4)
- **`build-android`** — build the Quest 3 APK (Unity 6, Android/ARM64/IL2CPP); Mac reality
  (Build & Run / Simulator, no Link); optional `adb install`.
- **`write-adr`** — scaffold the next numbered ADR under `docs/02-architecture/adr/`.
- **`add-feature`** — scaffold a feature (issue + branch + spec). Thin pointer to the canonical
  `.cursor/commands/add-feature.md` so Cursor and Claude stay in sync.
- **`mirror-epitech`** — manual mirror of `origin/main` to the Epitech repo. Thin pointer to
  `.cursor/commands/mirror-epitech.md`.

### Skills (10) — Ankhora domains + shared workflow
- **`new-xr-interaction`** — Meta Building Blocks workflow (`meta_*`) + scene contract + capture verify.
- **`spatial-anchors`** — `OVRSpatialAnchor` save/load/erase; persist a position in real space.
- **`passthrough-mr`** — Passthrough / Mixed Reality setup, occlusion, comfort.
- **`voice-spatial-audio`** — mic capture + timeline-aligned spatialised playback (Meta XR Audio).
- **`record-replay-contract`** — the core data model (timeline of poses/events/audio). The spine.
- **`world-space-annotations`** — anchored, pokeable in-MR text panels.
- **`xr-ui-design`** — spatial UI/UX per visionOS HIG + Meta Spatial (ergonomics, legibility, comfort).
- **`urp-shadergraph`** — ghost hands / annotation highlight / anchor halo (URP, mobile-VR cost).
- **`unity-testability`** — isolate record/replay logic out of `MonoBehaviour` for EditMode tests.
- **`git-commit`** — shared Conventional Commits workflow (promoted from Lény's local skill so all
  collaborators get it). The one non-domain skill: it encodes a team practice, not an app domain.

### Agents (7) — the specialised roster
- **`masterclass-author`** — *team lead*: plans a full record/replay slice and composes the agents below.
- **`xr-scene-builder`** — isolated scene assembly via `meta_*` + `Unity_*`.
- **`xr-ui-builder`** — spatial UI surfaces per `xr-ui-design`.
- **`unity-test-author`** — EditMode/PlayMode tests + `.meta` discipline (TDD).
- **`quest-perf-reviewer`** — read-only C# perf review (Quest 3 hot paths).
- **`horizon-store-compliance`** — read-only Store-readiness review (Interaction SDK, permissions, comfort).
- **`xr-build-doctor`** — diagnose build/sideload failures.

## Agent teams (the composition layer)

Agent teams are enabled (`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS`). The roster is designed to be
orchestrated, not just used one-off. Canonical team:

> **Masterclass feature team** — `masterclass-author` (lead)
> → `unity-test-author` (logic + tests, first, per `record-replay-contract`)
> → `xr-scene-builder` + `xr-ui-builder` (scene, anchors, panels)
> → `quest-perf-reviewer` + `horizon-store-compliance` (review gates).

The lead anchors on the `record-replay-contract`, drives logic-before-scene, and never lets a
slice be "done" without each member's verification (on device/simulator — Mac Editor can't do
hand tracking).

## Roadmap (deliberate future additions — not yet built)

Add only when a real need appears, same curation bar:
- **Skills:** `localization` (FR/EN masterclass content), `analytics-events` (learning telemetry),
  `addressables-content` (if masterclass packaging grows), `mr-scene-understanding` (room mesh, V2).
- **Agents:** `masterclass-content-author` (authoring UX), `release-manager` (build → sideload →
  Epitech mirror), `docs-author` (ADRs/specs from discussion).
- **V2 domains** (out of MVP scope, `@CLAUDE.md`): RAG configurator, marketplace, multi-user —
  each would get its own skill/agent set when the time comes.

## Adding to this layer

Before adding a file, ask: *is this anchored to a concrete Ankhora need, or am I accumulating
generic capability?* If generic, don't. Keep each file short, English, pointing at `CLAUDE.md`.
No new MCP server — everything runs over the existing official `unity-mcp` relay
(`Unity_*` + `meta_*`); we deliberately did not adopt a second Unity-control bridge.

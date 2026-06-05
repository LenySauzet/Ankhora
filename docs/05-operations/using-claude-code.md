# Using Claude Code on Ankhora — the pro playbook

How to get the most out of Claude Code on **this** project, with **our** tools and constraints.
This is the operating guide; `CLAUDE.md` stays the source of truth for project facts, and
`.claude/README.md` is the inventory of our skills/agents/commands. Read those two first; this
doc is about *how to work*.

> **Audience:** all three devs. Most of it is OS-agnostic; the Mac/Windows differences are
> called out explicitly (no Quest Link on Mac, hand-tracking iteration on Windows via Link).

---

## 0. Mental model

Claude Code is an **agentic** tool, not a chatbot: it reads files, runs commands, edits code,
and drives the Unity Editor live over MCP — in a loop — until a goal is met. Two consequences
shape everything below:

- **Context is the scarce resource.** Every file read and every tool output costs window space
  that later reasoning competes for. Keep sessions focused; clear between unrelated tasks.
- **It will confidently assert success it didn't verify.** So the prime directive is
  **evidence over assertions**: make Claude run the check and show the output before "done".
  (This guide was itself written after catching a subagent that *claimed* it wrote a file it
  hadn't — always verify.)

---

## 1. The complete feature flow (end to end)

The recommended loop is **Explore → Plan → Implement → Verify → Review → Ship**. On Ankhora it
maps onto our actual skills, agents, and CI. Worked example: *"let the expert record a hand
gesture and let a learner replay it."*

### 1.1 Explore (understand before deciding)
- Ask questions like you would a senior engineer: *"How does the replay timeline sample poses?"*
- For anything spanning multiple files, dispatch the **Explore** subagent (read-only, keeps your
  main context clean) instead of many sequential greps.
- Pull external API facts from **context7** / Meta docs — never from memory. Our XR skills all
  enforce this (the Meta XR SDK API shifts between versions).

### 1.2 Plan (separate thinking from doing)
- Enter **plan mode** (Shift+Tab, or ask "plan this first"). Claude researches and proposes an
  approach **without editing**. You approve before any code is written. This is the single
  biggest lever against "solved the wrong problem".
- For a feature-sized slice, let the **`record-replay-contract`** skill anchor the design first
  (does the data model change?), then the **`masterclass-author`** agent can produce the ordered
  build plan and compose the team.
- For genuinely fuzzy features, ask Claude to **interview you** with the `AskUserQuestion` tool
  and write a spec, then execute from the spec in a fresh session.

### 1.3 Implement (TDD, logic before scene)
- **Logic first, scene second.** The record/replay core is plain C# (the `unity-testability`
  skill) — get it written test-first via the **`unity-test-author`** agent before wiring any
  GameObjects.
- **Scene & UI second:** the **`xr-scene-builder`** and **`xr-ui-builder`** agents assemble rigs,
  anchors, and panels over `meta_*` + `Unity_*` (always `meta_get_config_information` first; the
  `new-xr-interaction`, `spatial-anchors`, `world-space-annotations`, `xr-ui-design` skills carry
  the recipes).
- Keep `MonoBehaviour`s thin; push decisions into testable plain C#.

### 1.4 Verify (evidence, not assertions)
- EditMode tests run headless in CI; run them locally and **paste the result**.
- XR behaviour that needs hand tracking **cannot be verified in Mac Editor Play Mode** — verify
  on device or in the Meta XR Simulator, and state which. On Windows, Quest Link gives in-Editor
  iteration.
- Use scene captures (`Unity_SceneView_Capture2DScene`) as visual evidence for scene/UI work.

### 1.5 Review (a fresh pair of eyes)
- Run **`/code-review`** — it reviews the current diff for bugs in a *fresh subagent* that never
  saw the reasoning that produced the code, so it grades on its own terms.
- Dispatch **`quest-perf-reviewer`** on the per-frame replay path, and **`horizon-store-compliance`**
  before any milestone (Interaction-SDK usage, permissions, comfort).
- The **`claude-review.yml`** GitHub Action also reviews every PR automatically.

### 1.6 Ship (commit → PR → mirror)
- Commit with the **`git-commit`** skill (Conventional Commits). Branch + push + PR via the
  **`commit-push-pr`** command — base `main`, **squash merge**, never push to `main` directly.
- After a merge to `main`, the Epitech mirror runs (or `/mirror-epitech` as the manual fallback).
- Scaffold the *next* feature with **`/add-feature`** (issue + branch + spec).

> Rule of thumb: **one feature = one focused session.** When you switch to an unrelated task,
> `/clear` first.

---

## 2. Our `.claude/` layer — when each piece fires

Full inventory in `.claude/README.md`. Quick mental map:

| You want to… | Reach for | Type |
|---|---|---|
| Run a repeated action (build APK, scaffold ADR/feature, mirror) | `/build-android`, `/write-adr`, `/add-feature`, `/mirror-epitech` | command |
| Apply project know-how yourself (XR rig, anchors, passthrough, audio, data model, annotations, UI design, shaders, testability, commits) | the 10 **skills** (auto-trigger on topic, or invoke explicitly) | skill |
| Offload a heavy task to an isolated context | `xr-scene-builder`, `xr-ui-builder`, `unity-test-author`, `quest-perf-reviewer`, `horizon-store-compliance`, `xr-build-doctor` | agent |
| Orchestrate a whole feature slice | `masterclass-author` (composes the agents above) | agent (lead) |

**Skills** trigger automatically from their `description`/triggers, or you can name them. **Agents**
you dispatch deliberately (or the lead composes them). **Commands** you type as `/name`.

Canonical team for a feature:
`masterclass-author` → `unity-test-author` → `xr-scene-builder` + `xr-ui-builder` →
`quest-perf-reviewer` + `horizon-store-compliance`. **Agent teams require activation:** set
`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1` in your shell profile, or commit a `.claude/settings.json`
with an `env` block to enable it repo-wide. Without it, `masterclass-author` runs solo.

---

## 3. The Unity MCP transport (the "bridge")

There is **one** relay (`unity-mcp`) exposing **two tool families**: `Unity_*` (Unity native) and
`meta_*` (Meta XR Building Blocks). It is **not** committed (`.mcp.json` is per-user-per-OS); each
dev registers it locally (see `setup-unity6-claude-integration.md`).

- The bridge **only works while the Unity Editor is open.** If `Unity_*`/`meta_*` calls fail,
  check the Editor is running and the client is approved in *Project Settings → AI*.
- **Always call `meta_get_config_information` once before any `meta_*` tool** — it returns the live
  config you need to set correct values.
- Sanity check both families respond: `Unity_GetProjectData` + `meta_get_config_information`.
- We deliberately run **no second Unity bridge** (no CoplayDev/Besty HTTP server) — one transport,
  no contention.
- Other MCP servers (context7, github, etc.) are always-on; prefer **context7** for any
  library/SDK API question.

---

## 4. CLAUDE.md & memory discipline

- `CLAUDE.md` is a **living document**: when a durable decision or constraint is learned, Claude
  updates it inline (or adds an ADR for architectural calls). Keep it lean — if a rule keeps being
  ignored, the file is too long; cut, don't add.
- Add a quick line to your **personal** global memory mid-session with the `#` shortcut.
- Significant decisions → an **ADR** via `/write-adr` (the `claude-review` action reads
  `docs/02-architecture/adr/`).
- `.claude/` files stay **thin** and point back to `CLAUDE.md` so Cursor and Claude never drift.

---

## 5. Context management (keep the window clean)

- **`/clear`** between unrelated tasks — the highest-leverage habit. A fresh window reasons better.
- **`/compact [instructions]`** when a long single task fills up (`/compact focus on the replay
  timeline changes`). Auto-compaction also kicks in near the limit.
- **`/rewind`** (or Esc+Esc) to a checkpoint to summarise/restore part of the conversation.
- For a quick aside that shouldn't pollute history, use **`/btw`**.
- Big outputs are expensive: read only the slice you need; use subagents (Explore, our agents) to
  process large material and return just the conclusion.

**Avoid the failure patterns:** the *kitchen-sink session* (mixed unrelated tasks → `/clear`), the
*trust-then-verify gap* (claiming done without running the check), and *infinite exploration* (scope
it or hand it to a subagent).

---

## 6. Permissions & modes

- **Plan mode** — research/propose, no edits. Default for any non-trivial change.
- **Accept-edits** — auto-applies edits but still prompts for risky commands. Good for a tight
  TDD loop you're watching.
- **Auto mode** (`claude --permission-mode auto`) — a classifier reviews each command, blocking
  scope escalation while letting routine work run prompt-free. Useful for longer unattended runs.
- **Bypass** — no prompts; only for sandboxes you trust.
- Reduce prompt fatigue safely with the **`/fewer-permission-prompts`** skill (allowlists common
  read-only commands in `.claude/settings.json`). Never blanket-allow destructive commands.
- Our hard guardrails still apply: no `git push`/merge/force-push without explicit OK, never commit
  secrets, never bypass hooks.

---

## 7. Verification — the non-negotiable

For every "done":
1. **Show evidence** — the command run and its output, the test result, or a scene capture.
2. **EditMode tests** for pure logic (run in CI too).
3. **On-device / Simulator** for anything touching hand tracking, passthrough, anchors, or audio.
   Mac Editor Play Mode cannot do hand tracking — say so explicitly when you couldn't fully verify.
4. **Adversarial review** for anything important: `/code-review` or a verification subagent that
   tries to *refute* the result, so the agent that did the work isn't the one grading it.

For longer autonomous runs you can make verification structural: a Stop hook that blocks the turn
until a check passes, or a `/goal` condition re-checked every turn. Use these when you're not watching.

---

## 8. Ankhora-specific gotchas

- **Unity version is pinned** to `6000.4.10f1` on all three machines — mismatches cause scene/prefab
  diffs. Don't "upgrade to try something".
- **Asset Serialization = Force Text** (diffable scenes/prefabs). Don't change it.
- **`.meta` files:** every asset under `Assets/` needs a committed `.meta` with a unique GUID. Missing
  test metas are being fixed on `fix/unity-test-metas`; the `unity-test-author` agent handles metas.
- **No Quest Link on Mac.** Iteration is Build & Run / Meta XR Simulator. Hand-tracking iteration
  lives on the Windows stations.
- **CI does not build the APK** (Meta SDK `OVRProjectConfig` throws on headless Linux). CI runs
  EditMode tests. The APK is built locally on device (`/build-android`). If a build breaks, dispatch
  `xr-build-doctor`.
- **Dual-repo:** PRs land on `LenySauzet/Ankhora`; `main` is mirrored to the Epitech repo. Never push
  to the Epitech repo by hand.
- **MVP is ultra-thin** (one ≤2-min masterclass). Anything in `CLAUDE.md` § *Out of scope* (RAG,
  marketplace, multi-user, QR launcher…) triggers a re-scoping discussion before any code.

---

## 9. Scaling up (when one session isn't enough)

- **Worktrees** — run isolated Claude sessions in separate git checkouts so parallel edits don't
  collide. Good for working two features at once.
- **Headless** — `claude -p "..."` for scripts/CI; `--output-format json` / `stream-json --verbose`
  for parseable output. This is how `claude-review.yml` runs.
- **Parallel agents** — for 2+ independent tasks, dispatch them concurrently (our agents, or — with the
  superpowers plugin — its `dispatching-parallel-agents` skill).

---

## 10. Quick reference

```
Shift+Tab        cycle permission mode (incl. plan mode)
/clear           reset context between tasks
/compact [hint]  summarise a long session, optionally focused
/rewind          go back to a checkpoint (restore or summarise)
/code-review     fresh-subagent review of the current diff
/build-android   build the Quest APK (local, on device)
/write-adr       scaffold the next ADR
/add-feature     issue + branch + spec for a new feature
/mirror-epitech  manual mirror of main to the Epitech repo
#<text>          append a line to personal global memory
```

**The five habits that matter most:** plan before coding · keep logic testable and test it ·
verify on device, show evidence · `/clear` between tasks · let a fresh subagent review the diff.

---

## References (verified 2026-06-05)

- Anthropic — *Best practices for Claude Code*: https://www.anthropic.com/engineering/claude-code-best-practices
- Claude Code docs — *Common workflows*: https://docs.claude.com/en/docs/claude-code/common-workflows
- Canonical docs index: https://docs.claude.com/en/docs/claude-code (also mirrored at code.claude.com/docs).
  Feature pages referenced above: sub-agents, skills, slash-commands, hooks, mcp, permission-modes,
  headless, worktrees, memory, checkpointing.
- Project: `CLAUDE.md`, `.claude/README.md`, `docs/05-operations/setup-unity6-claude-integration.md`.

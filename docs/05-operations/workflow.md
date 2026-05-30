# Team workflow — Idea → Production

Authoritative reference for the Ankhora team. Walks through every step from "we should build X" to "X is on `main` and mirrored to the Epitech submission repo", with the exact tool and command for each.

Read [CLAUDE.md](../../CLAUDE.md) first if you have not already — it sets the constraints this workflow operates under (3-person Epitech project, Thu/Fri only, dual-repo, ultra-thin MVP).

---

## TL;DR — One-glance flow

```
IDEA
  ↓
ISSUE  (GitHub issue, picks a template, auto-added to the Kanban)
  ↓
SPEC?  (optional — only for non-trivial features, brainstorm in Claude Code)
  ↓
BRANCH  (feat/<scope>-<slug>, branched from up-to-date main)
  ↓
CODE   (Unity Editor + Cursor + Blender)
  ↓
BUILD & TEST  (APK + adb on Mac, Quest Link in Editor on Windows)
  ↓
COMMIT  (/git-commit in Claude Code → Conventional Commits)
  ↓
PUSH + OPEN PR  (gh pr create — template auto-fills)
  ↓
3 REVIEWERS RUN  (CodeRabbit + Claude review + GameCI Unity build)
  ↓
HUMAN REVIEW  (when teammates available — best-effort, not required)
  ↓
SQUASH & MERGE  (one Conventional Commits line lands on main)
  ↓
SYNC LOCAL  (git pull --ff-only)
  ↓
MIRROR  (/mirror-epitech — pushes main to the Epitech repo)
```

Total time per feature: typically 1 working day end-to-end. Faster for fixes, slower for things that need a real spec.

---

## Quick reference: which tool for what

| Need | Tool | Where |
|---|---|---|
| Plan / brainstorm / explore the codebase | **Claude Code** | Terminal (Warp) |
| Edit code in the IDE | **Cursor** | IDE window |
| Run multi-file refactors | **Claude Code** | Terminal |
| Quick slash command (`/add-feature`, `/build-android`, `/mirror-epitech`) | **Cursor** | Cursor chat |
| Conventional-commit message generation | **Claude Code** (`/git-commit` skill) | Terminal |
| Multi-line PR / issue editing | **gh CLI** or **GitHub UI** | Terminal or browser |
| Scene / prefab editing | **Unity Editor** | Native app |
| 3D modelling | **Blender** | Native app |
| Sideload APK on Quest 3 | **Meta Quest Developer Hub** or **adb** | Terminal |
| Performance monitoring on device | **OVR Metrics Tool** (Quest app) | On the headset |

### Claude Code vs Cursor — two distinct extension systems

Both Claude Code and Cursor are AI-assisted, but they run different processes and read different files:

|                   | **Claude Code**                    | **Cursor**                         |
|-------------------|------------------------------------|------------------------------------|
| Type              | CLI in your terminal               | Standalone IDE                     |
| Extension files   | **Skills** (`~/.claude/skills/*` and `.claude/skills/*` per project) | **Commands** (`.cursor/commands/*.md`) |
| Invocation        | `/skill-name` in the Claude Code session | `/command-name` in the Cursor chat panel |
| Examples in this repo | `/git-commit`, `/brainstorming`, `/writing-plans`, `/code-review` | `/add-feature`, `/build-android`, `/mirror-epitech` |
| Reads CLAUDE.md   | ✅ Yes, automatically               | ✅ Yes, via `.cursor/rules/general.mdc` → `AGENTS.md` → `CLAUDE.md` |

**A skill in Claude Code is not available in Cursor**, and vice versa — they live in separate folders and are invoked by separate processes. When in doubt, look at the file path: `.cursor/commands/x.md` is a Cursor command; anything under `~/.claude/` or `.claude/` is a Claude Code skill.

---

## Phase 1 — Idea → Issue

### When to create an issue vs go PR-only

**Default: always create an issue first.** Issues are the unit of planning on the Kanban. The PR comes later as the *delivery* of the code.

Skip the issue only in three cases:

| Case | Why | Example |
|---|---|---|
| Trivial change, not worth tracking | Kanban clutter | Fix typo in `README.md` |
| Bot-generated | Already auto-tracked | Dependabot bumps |
| Hotfix during incident | Speed | Production passthrough is black, ship a fix now |

For everything else: **issue first**, PR later, linked via `Closes #NN` in the PR body. When the PR merges, the issue closes automatically and the card moves to `Done` on the board (via the `Pull request merged` Project workflow).

### Create the issue

`github.com/LenySauzet/Ankhora/issues/new/choose` → pick the right template:

| Template | When |
|---|---|
| **Feature** | New user-facing or system capability |
| **Bug** | Something is broken / behaves incorrectly |
| **Task** | Chore, refactor, doc, CI — any change that's neither feature nor bug |
| **Research / spike** | Time-boxed investigation that must conclude with a decision |

Fill in the YAML form. The title automatically follows the Conventional Commits format because the templates pre-fill `feat(<scope>): ...`, `fix(<scope>): ...`, etc.

### What happens automatically

- Issue auto-added to the **Ankhora** GitHub Project board (workflow `Item added to project`).
- Issue lands in the `Backlog` column.

### Move the issue through the board

| Column | Meaning |
|---|---|
| `Backlog` | Not started, may not be in this sprint |
| `Spec / Research` | Needs spec or investigation before it's codable |
| `Ready` | Spec done, anyone can pick it up |
| `In progress` | Someone is working on it (PR not open yet) |
| `In review` | PR is open and waiting for review |
| `Done` | Merged |

Drag manually between `Backlog` → `Spec / Research` → `Ready` as you plan. The other transitions are automated by Project workflows.

---

## Phase 2 — Spec (optional, ~15–60 min)

Only do this phase for **non-trivial** features. Trivial fixes can skip straight to Phase 3.

### Brainstorm

Open Claude Code (in Warp) and invoke:

```
/brainstorming
```

This is a Superpowers skill that walks you through user intent, requirements, design, and trade-offs before any code is written. It's the cheapest place to catch a wrong-headed feature.

For multi-step plans (the feature has several files / interactions / decisions), follow up with:

```
/writing-plans
```

### Write the spec file

Create `docs/03-xr/<scope>-<slug>.md` (or under the right folder if the scope is not `xr`):

```markdown
# <Scope> · <Title>

- Status: draft | in-progress | shipped
- Owner: @<github-handle>
- Tracking issue: #<NN>
- Last updated: YYYY-MM-DD

## Why
What problem this solves. Tie back to the MVP (see `docs/01-product/mvp-scope.md`) or explain why it's in scope.

## What
User-facing behaviour described from the expert / learner perspective.

## How
Architecture sketch. Reference files / MonoBehaviours / scenes that will be touched. Cite Meta SDK APIs explicitly with doc links.

## Data model
If the feature touches the masterclass JSON schema, show the diff here.

## Acceptance criteria
- [ ] …
- [ ] Runs at 90 FPS on Quest 3 with passthrough enabled.
- [ ] No allocation in hot loop (verify via Profiler).

## Out of scope
List nearby features intentionally left out — link follow-up issues.

## Open questions
- …
```

Update the linked issue to reference the spec: `Spec: docs/03-xr/<scope>-<slug>.md`.

---

## Phase 3 — Branch (1 min)

### Sync `main` first

Always start from an up-to-date `main`:

```bash
git checkout main
git pull --ff-only
```

If `git pull --ff-only` refuses because your local has commits that origin doesn't, deal with those first (push them, or move them to a branch). Never start a new feature on a stale `main`.

> **What `--ff-only` does**: `git pull` is `git fetch + git merge` by default. `--ff-only` tells git to only fast-forward — if your local main has diverged from origin/main, it refuses to merge automatically. This prevents accidental merge commits on `main`.

### Branch naming

Format: `<type>/<scope>-<slug>`, kebab-case throughout.

| Prefix | Use |
|---|---|
| `feat/` | New feature |
| `fix/` | Bug fix |
| `chore/` | Maintenance, infra |
| `docs/` | Docs only |
| `ci/` | CI / workflows |
| `refactor/` | Refactor with no behaviour change |

Allowed scopes: `recorder`, `playback`, `xr`, `ui`, `domain`, `infra`, `ci`, `docs`, `tooling`.

Examples:
- `feat/recorder-hand-capture`
- `fix/passthrough-flicker`
- `chore/manual-mirror-epitech`
- `docs/team-workflow`

### Two ways to create the branch

**Manual (always works):**

```bash
git checkout -b feat/recorder-hand-capture
```

**Automated (Cursor slash command):**

In Cursor's chat panel:

```
/add-feature recorder hand-capture "Capture hand joints to JSON timeline" @teammate1
```

This is a Cursor command at `.cursor/commands/add-feature.md`. It scaffolds Phases 1–3 in one shot:

1. Validates the scope is allowed and the slug is kebab-case.
2. Creates the GitHub issue (`feat(recorder): Capture hand joints to JSON timeline`) labelled `feature` + `scope/recorder`, assigned to the owner you passed.
3. Creates `docs/03-xr/recorder-hand-capture.md` pre-filled with the spec template.
4. Creates the branch `feat/recorder-hand-capture` from up-to-date main.
5. Commits the spec doc with `docs(recorder): add spec for ... (#<issue>)`.
6. Pushes the branch and (optionally) opens a draft PR.

Use it when you have a clear, well-scoped feature to start. Skip it for research spikes (no spec yet) or trivial fixes.

---

## Phase 4 — Develop

### Read the project rules before touching the code

The first time you open Cursor on this repo, it auto-loads `.cursor/rules/*.mdc`:

| Rule file | What it enforces |
|---|---|
| `001-unity-conventions.mdc` | C# style, Unity asset workflow, scripts folder layout |
| `002-meta-xr-sdk.mdc` | Building Blocks first, Interaction SDK for hand tracking, passthrough setup, anchors, Voice SDK |
| `003-team-workflow.mdc` | Branches, Conventional Commits, PRs, mirror-to-Epitech |
| `004-vr-performance.mdc` | 90 Hz budgets, URP only, Vulkan, MSAA 4x, no SSAO, scripting allocations |

Claude Code reads `CLAUDE.md` automatically via the global instructions in `~/.claude/CLAUDE.md`.

### Iteration loop

The loop depends on your machine.

**Mac (Lény)** — no Quest Link:

```
modify code in Cursor
  ↓
build APK (Unity Editor or /build-android command)
  ↓
adb install (Cursor command or manual)
  ↓
test on Quest 3
  ↓
back to top
```

Optionally use **Meta XR Simulator** for fast-iteration on UI / logic that does not need real hand tracking. Build APK only for the things the Simulator cannot represent.

**Windows (teammates)** — with Quest Link:

```
modify code in Cursor
  ↓
Play Mode in Unity Editor via Quest Link
  ↓
test live (no APK build needed)
  ↓
back to top
```

Windows machines are the natural place for tight hand-tracking iteration. Lény (Mac) sticks to architectural / non-XR-heavy work and burst-tests on device.

### Build the APK

When ready to ship to device, the canonical entry point is the Cursor command:

```
/build-android         # dev build, installs to connected device
/build-android release # release build, installs to connected device
/build-android dev no  # dev build, no install
```

The command (`.cursor/commands/build-android.md`) validates `adb` is on PATH, that a Quest 3 is connected and authorised, then runs Unity in batch mode and `adb install -r`.

> This command currently expects `Assets/Editor/BuildScript.cs` to exist with a `Ankhora.Editor.BuildScript.BuildQuestApk` method. We have not added that script yet — the first Meta XR SDK integration PR will introduce it. Until then, build manually from `File > Build Settings > Build And Run` in Unity.

### Check performance on device

Install **OVR Metrics Tool** on the Quest 3 once (from the Meta Quest Developer Hub). Enable the overlay on the headset to see live FPS / GPU / CPU / thermals while you test.

Acceptance criterion for any merge: **90 FPS on Quest 3 with passthrough enabled**. See `.cursor/rules/004-vr-performance.mdc` for the full budget.

### Run the Unity tests

In Unity Editor: `Window > General > Test Runner > Run All`. Tests live under `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/`. We currently have no tests; add them as the architecture solidifies.

---

## Phase 5 — Commit + Push

### Review what's about to be committed

```bash
git status
git diff           # unstaged changes
git diff --staged  # staged changes
```

### Generate a Conventional Commits message

In Claude Code (Warp), invoke the global skill:

```
/git-commit
```

The skill analyses the diff, picks the right type and scope, generates a description in the imperative mood, and either commits directly or shows you the message for confirmation. This is the easy way to stay compliant with the team convention.

**Manual fallback:**

```bash
git add <specific files>
git commit -m "feat(recorder): persist hand joints to JSON timeline"
```

### Conventional Commits format — required on every commit

```
<type>(<scope>): <description in the imperative, ≤72 chars>

<optional body>

<optional footer>
```

Allowed `type`s: `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `perf`, `build`, `ci`, `style`.

Allowed `scope`s for Ankhora: `recorder`, `playback`, `xr`, `ui`, `domain`, `infra`, `ci`, `docs`, `tooling`.

Why this matters: squash-merge uses the PR title (which must also be a Conventional Commit) as the commit message on `main`. That keeps `main` history readable as a changelog and lets us auto-generate releases later.

### Push the branch

First push of a branch (sets the upstream):

```bash
git push -u origin <branch-name>
```

Subsequent pushes:

```bash
git push
```

---

## Phase 6 — Open Pull Request

### Open the PR

From Warp (easiest):

```bash
gh pr create --fill
```

`--fill` pre-populates the title and body from the branch's commits (which are Conventional Commits-formatted, so the title comes out right) and applies the PR template at `.github/pull_request_template.md`.

You can also open it from the GitHub UI — same template applies.

### Fill in the template

The template asks for:

- **What** — one paragraph, user-facing or system-facing.
- **Why** — one paragraph, link the issue with `Closes #NN`.
- **How** — bullet points, files / scenes / packages touched.
- **Screens / video** — required for any UI / passthrough / hand-tracking / animation change. Use Meta Quest Developer Hub's recording feature.
- **Verification checklist** — tick what you actually did (Editor test, on-device test, OVR Metrics, no regression).
- **Architectural impact** — link an ADR if you introduced one.

### What auto-triggers

Three required checks fire on every PR:

| Check | What it does | Typical duration | Bot reviewer |
|---|---|---|---|
| **GameCI Unity build** | Builds the Android APK on a fresh Ubuntu runner using the Student licence. Catches "works on my machine" issues. | 5–15 min first time per runner (Unity Docker image), then ~5 min with cache. | n/a |
| **Project-aware review** (`anthropics/claude-code-action`) | Posts a review reading CLAUDE.md + Cursor rules + ADRs + research dossier. Comments inline. Skipped on Dependabot PRs and on PRs that modify `.github/workflows/claude-review.yml` itself. | ~1 min | Claude |
| **CodeRabbit** | Generalist review: bugs, style, perf, security. Posts a summary + walkthrough + inline nitpicks. Free for public OSS repos. | ~2 min | CodeRabbit |

If you need to ask the Claude reviewer a follow-up question, post `@claude <question>` as a PR comment — it re-runs and answers in the same thread.

### What the board does automatically

The linked issue moves to `In review` on the Ankhora board (Project workflow `Pull request linked to issue`). No manual drag needed.

---

## Phase 7 — Review

### Address bot comments

CodeRabbit and Claude post inline review comments. Treat them like a human review — accept, refute with a reply, or push a fixup commit on the same branch (it re-triggers all checks). The bots run again automatically when you push.

### Request a human review

When a teammate is available (Thu/Fri standup is the natural slot), add them as a reviewer from the PR sidebar. Branch protection is currently set to **0 required approvals** — humans review when they can, but the PR is not blocked waiting for them. The 3 bot checks plus the Unity build are the actual gate.

Keep this in mind: 0 approvals is a deliberate trade-off for a 3-person / 2-month project. If we slip and merge something risky, it's because *we* shipped fast, not because the rule was wrong. Raise back to 1 if the team grows or the rhythm allows.

### Iterate

If feedback requires changes:

1. Make the change locally on the same branch.
2. `/git-commit` (or manual) to commit it.
3. `git push` — the PR updates automatically and all checks re-run.
4. Reply "addressed" to each comment with the new commit SHA.

---

## Phase 8 — Merge

### Squash and merge

When the 3 required checks are green and you (or a teammate) approves, click **Squash and merge** on GitHub.

| Setting we have | Effect |
|---|---|
| **Default commit message: Pull request title** | The PR title (a Conventional Commit) becomes the single commit on `main` |
| **Linear history required** | No merge commits on `main` ever |
| **Automatically delete head branches** | The feature branch disappears the moment the merge lands |
| **Allow auto-merge** | If a check is slow, click "Enable auto-merge" and walk away — GitHub squash-merges as soon as everything is green |

### What auto-fires after merge

- Feature branch deleted on GitHub.
- Linked issue closed (because of `Closes #NN`).
- Project board card moves to `Done`.
- `Mirror to Epitech` workflow would have run on push to main — currently disabled until the EPITECH_PAT lands (see Phase 9).

---

## Phase 9 — Sync local + Mirror to Epitech

### Sync local main

```bash
git checkout main
git pull --ff-only
```

Then optionally delete the local branch (it's already gone on origin):

```bash
git branch -d <branch-name>
```

### Mirror to the Epitech repo

While `EPITECH_PAT` is pending Epitech-org approval, the workflow is **disabled**. Each member of the team mirrors manually after a merge, using their own GitHub credentials:

In Cursor's chat:

```
/mirror-epitech
```

This command (`.cursor/commands/mirror-epitech.md`) is **branch-agnostic and working-tree-agnostic** — it fetches `origin/main` fresh and pushes that ref directly onto the Epitech remote, no matter what branch you happen to be on locally. You can invoke it from any state.

What it does under the hood:

```bash
git fetch origin main
git remote get-url epitech 2>/dev/null \
  || git remote add epitech https://github.com/EpitechMscProPromo2026/T-VIR-902-MPL_2.git
git lfs fetch origin main
git lfs push --all epitech
git push epitech +refs/remotes/origin/main:refs/heads/main
```

Force-pushing is intentional — the Epitech repo is mirror-only, never edited by hand.

### When the PAT is approved

Re-enable the workflow and retire the manual fallback:

```bash
gh workflow enable mirror-epitech.yml
gh workflow run mirror-epitech.yml   # one-shot trigger to backfill
```

Then delete `.cursor/commands/mirror-epitech.md` and the manual subsection in `SETUP-NEXT-STEPS.md`.

---

## Phase 10 — Cleanup (weekly, 5–10 min)

Do this at the end of every Friday session, or at the start of every Thursday.

### Triage Dependabot PRs

Dependabot opens PRs for action-version bumps on a weekly schedule (configured in `.github/dependabot.yml`).

For each open Dependabot PR:

1. Read the changelog linked in the PR description.
2. Decide:
   - Low-risk patch (`@v4 → @v4.0.5`): merge as soon as CI is green.
   - Minor bump (`@v4 → @v4.1`): probably safe; check the changelog briefly, then merge.
   - Major bump (`@v4 → @v5`): test on the PR first; do **not** merge without checking the release notes for breaking changes. If you decide to skip a major version, comment `@dependabot ignore this major version` on the PR.
3. Always make sure CI is green before merge. If a bump breaks CI, comment `@dependabot recreate` after fixing main, or `@dependabot close` if the bump is genuinely incompatible.

### Reconcile the board

Drag any orphan cards (issues created but never assigned to a column) into the right place. Drop stale `Backlog` items by closing the issue with a reason.

### Sync everyone's local

Each teammate runs `git checkout main && git pull --ff-only` to land all merges of the week.

---

## Phase 11 — Resolve a merge conflict on a PR

Happens when `main` has moved in the same region of code as your PR since you branched. GitHub then blocks the merge button and surfaces "This branch has conflicts that must be resolved".

### When conflicts are expected

| Scenario | Likelihood |
|---|---|
| Two PRs touch the same file in parallel | High on a 3-person team with a small file set |
| Squash-merge of a PR, then continuation of work on the same branch | Guaranteed conflict (see "The squash-merge artifact" below) |
| Dependabot bump touches a workflow you're also editing | Common around weekly bumps |
| Unity scene edited from two branches | Common — and Unity's YAML merge driver handles most of it |

### The standard procedure (always works)

#### Step 1 — Sync your local main

```bash
git checkout main
git pull --ff-only
```

#### Step 2 — Rebase your PR branch on the fresh main

```bash
git checkout <pr-branch>
git rebase origin/main
```

Three possible outcomes:

| Outcome | What it looks like | Next step |
|---|---|---|
| **A. Clean rebase** | `Rebasage et mise à jour de refs/heads/X avec succès.` | Go to Step 4 |
| **B. Cherry-pick skipped** | `avertissement : le commit XXXXXX appliqué précédemment a été sauté` | Normal — Git detected duplicate content already on main. Go to Step 4 |
| **C. Real conflict** | `CONFLICT (content): Merge conflict in <file>` and the rebase pauses | Go to Step 3 |

#### Step 3 — Resolve conflicts (only if outcome C)

For each paused commit, open the conflicted files. You'll see markers:

```
<<<<<<< HEAD
content currently on main
=======
content from your branch
>>>>>>> <your-commit-sha>
```

Edit each file to keep the right content (often a mix of both sides), then strip the `<<<<<<<`, `=======`, `>>>>>>>` markers. Save. Then:

```bash
git add <resolved file>
git rebase --continue
```

Git moves to the next paused commit (or finishes the rebase if that was the last one). If you get lost or want to back out:

```bash
git rebase --abort
```

This restores the branch to its pre-rebase state.

**For Unity scenes / prefabs / `.asset` files**: the `unityyamlmerge` driver is registered in `.gitattributes` and runs automatically — it resolves ~90% of YAML conflicts without you touching them. If it can't, open the scene in Unity Editor and resolve visually before staging and continuing.

#### Step 4 — Force-push the rebased branch

Rebase **rewrites** the commit SHAs on your branch, so a plain `git push` is refused. Use:

```bash
git push --force-with-lease
```

- `--force` overwrites the remote blindly — if a teammate pushed to your branch between your fetch and your push, their commits are silently dropped.
- `--force-with-lease` only force-pushes if the remote is in the state you last fetched. If it has moved, the push is refused so you can investigate.

**Golden rule**: `--force-with-lease` on your own feature branch is fine. Force-push to `main` is blocked by branch protection — and that's the correct behaviour.

### Alternative — GitHub's "Resolve conflicts" web editor

For trivial conflicts on small text files (markdown, YAML, short code), the **Resolve conflicts** button on the PR page opens an in-browser editor where you edit and commit directly.

**Avoid it for**:

- Unity scenes (`*.unity`)
- Prefabs (`*.prefab`)
- Files longer than ~100 lines
- Anything where you'd want to run tests after resolving

For those, do the local rebase — you get your IDE, the Unity merge driver, and the ability to test before pushing.

### The squash-merge artifact (a recurring gotcha)

When you squash-merge a PR, GitHub:

1. Takes all the commits on the PR branch.
2. Squashes them into **one new commit on `main`** with a brand new SHA.
3. Closes the PR.

The original commits on the PR branch still exist. If you (or anyone) continues working on **the same branch** afterwards, every follow-up commit appears to "duplicate" content that's already on `main` under a different SHA. Git can sometimes auto-detect this via patch-id and skip the duplicate during rebase (outcome B above), but the cleaner fix is to never put yourself in that situation:

**Rule**: after your PR is squash-merged, **do not continue working on that branch**. Sync `main`, create a fresh branch for follow-up work.

```bash
# After your PR was squash-merged:
git checkout main
git pull --ff-only
git branch -d <merged-branch>          # delete the old branch locally
git checkout -b <new-branch-for-followup>
```

If you forget and end up with a confusing conflict on a branch that "should" be clean, the rebase procedure above resolves it — but it's friction you don't need.

---

## Tooling reference — Slash commands inventory

### Cursor commands (in `.cursor/commands/*.md`)

| Command | Phase | What it does |
|---|---|---|
| `/add-feature` | 1, 2, 3 | Scaffold issue + spec + branch + draft PR |
| `/build-android` | 4 | Build the Quest 3 APK and (optionally) `adb install` |
| `/mirror-epitech` | 9 | Mirror `origin/main` to the Epitech submission repo |

### Claude Code skills (global, in `~/.claude/skills/*`)

| Skill | Phase | What it does |
|---|---|---|
| `/brainstorming` | 2 | Explore user intent and requirements before code |
| `/writing-plans` | 2 | Decompose a multi-step task into ordered steps |
| `/test-driven-development` | 4 | Drive a feature with tests first |
| `/systematic-debugging` | 4 | Methodical bug hunt instead of guess-and-check |
| `/git-commit` | 5 | Generate a Conventional Commits message from the diff |
| `/code-review` | 6, 7 | Review the current diff for bugs and cleanups |
| `/verification-before-completion` | 4, 5 | Run checks before claiming "done" |

### Adding your own commands and skills

| Want a new… | Where | Format |
|---|---|---|
| Cursor slash command (project-wide) | `.cursor/commands/<name>.md` | Plain Markdown, no YAML frontmatter, English only. Filename = command name. See `.cursor/commands/example.md` for the template |
| Claude Code skill (per project) | `.claude/skills/<name>.md` | YAML frontmatter + Markdown body. See [Claude Code skills docs](https://docs.claude.com/en/docs/claude-code) |

When in doubt, write a Cursor command (cheaper, more focused). Skills are heavier and worth it when the workflow is genuinely reusable across projects.

---

## Cheat sheet — print this

```
PLANNING
  /brainstorming                       (Claude Code)
  Create an Issue                      (GitHub)

START FEATURE
  git checkout main && git pull --ff-only
  /add-feature <scope> <slug> "<title>" @<owner>      (Cursor)
    — or —
  git checkout -b feat/<scope>-<slug>                  (manual)

DEVELOP
  Code in Cursor (rules auto-loaded)
  /build-android                                       (Cursor)
  Test on Quest 3, OVR Metrics overlay = 90 FPS

COMMIT
  /git-commit                                          (Claude Code)
  git push -u origin feat/<scope>-<slug>               (first push)

OPEN PR
  gh pr create --fill
  Fill template, link `Closes #NN`

REVIEW
  Address CodeRabbit + Claude comments
  Reply to @claude with questions if needed
  Push fixup commits — checks re-run automatically

MERGE
  Squash and merge (or Enable auto-merge)

POST-MERGE
  git checkout main && git pull --ff-only
  /mirror-epitech                                      (Cursor)
  git branch -d feat/<scope>-<slug>

CONFLICT RESOLUTION (when GitHub says "conflicts must be resolved")
  git checkout main && git pull --ff-only
  git checkout <pr-branch>
  git rebase origin/main
    — clean ............. → push
    — cherry-pick skipped → push  (normal, post squash-merge artifact)
    — CONFLICT .......... → edit conflicted files, strip <<<<<<< markers,
                            git add <files>, git rebase --continue
  git push --force-with-lease
```

---

## Common pitfalls

| Symptom | Probable cause | Fix |
|---|---|---|
| `git pull --ff-only` refuses | Local main has commits not on origin | Push them on a branch, then reset main |
| Claude review fails with "internal error" | Transient bug in `anthropics/claude-code-action` | Re-run the workflow once |
| Claude review fails on a workflow-file PR | Security guard — action refuses to validate itself | Bypass merge as admin; this is expected |
| Claude review fails on a Dependabot PR | Action refuses bot-initiated workflows | Already handled — the workflow skips Claude on Dependabot |
| CodeRabbit reports "Review skipped" | No substantive code change to review (e.g. docs-only PR) | Fine, this counts as `pass` |
| Unity build fails with `Missing Unity License` | The secret silo is mismatched (Actions vs Dependabot) | Add the 3 Unity secrets to **both** silos |
| `mirror-epitech.yml` workflow shows red | EPITECH_PAT is `Pending` — workflow is disabled by design | Use `/mirror-epitech` manually until PAT lands |
| `/mirror-epitech` reports `LFS budget exceeded` | The Epitech org's LFS quota is at 0 — does not affect our working repo | Already handled — the command passes `--no-verify` to push refs without LFS blobs |
| Conventional Commits lint complains | PR title doesn't follow the format | Edit the title — never the commits, since we squash-merge |
| PR shows "This branch has conflicts" right after a related PR squash-merged | Squash-merge artifact — the original PR's commits still live on the branch | See [Phase 11](#phase-11--resolve-a-merge-conflict-on-a-pr). Quick fix: `git rebase origin/main && git push --force-with-lease` |
| `git push` refused because rebase rewrote history | Expected — push needs `--force-with-lease` after a rebase | `git push --force-with-lease` (never `--force` on a shared branch) |
| Rebase pauses with conflicts you can't figure out | Complex overlap — best resolved with full context | `git rebase --abort`, ask a teammate or open the file in your IDE with the inline merge UI |

If something is broken and not listed here, add a row.

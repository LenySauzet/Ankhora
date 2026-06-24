# Tooling · Project insights report skill

- Status: in-progress
- Owner: @LenySauzet
- Tracking issue: #22
- Last updated: 2026-06-25

## Why

The team needs a repeatable way to **prove and analyse its work** for the recurring
Epitech follow-ups: KPIs, what worked, what didn't, discoveries, and progress. Today this
evidence is scattered across the repo (docs, ADRs, commits), the PRs (each carries a
CodeRabbit + a Claude Code summary), and the GitHub Project board. A single command should
turn all of it into a presentation-grade, self-contained report — a **source of truth** to
estimate and defend the team's involvement and advances.

Not Ankhora product scope; this is **dev tooling** (a Claude Code skill).

## What

A project skill, `insights-report`, that on each invocation produces a **dated,
self-contained `.html` report** (`reports/insights-YYYY-MM-DD.html`) with charts, covering:

- **Executive summary** — project, period, team, one-paragraph TL;DR.
- **KPIs at a glance** — PRs merged, time-to-merge, commits, lines ±, ADRs, spikes, board
  items by status, % MVP touched, active working days (Thu/Fri reality).
- **Velocity & activity** — PRs/commits over time; contribution by author (broadened to all
  branches, with an honest bootstrap-phase context note — see Open questions).
- **Delivery / board** — Projects v2 status distribution and milestone mapping (light:
  the board is sparse; PRs are the real work unit).
- **Quality** — CodeRabbit findings raised/resolved, CI pass rate, review turnaround.
- **Decisions & discoveries** — ADRs (Unity 6, MRTK), spikes, research dossier.
- **What worked / what didn't** + **improvement ideas / next steps**.
- **Provenance appendix** — data sources + generation timestamp.

## How

The skill is a documented procedure Claude follows at runtime:

1. **Preconditions** — in the repo; `gh` has `read:project` scope (else instruct
   `gh auth refresh -s read:project --hostname github.com`).
2. **Gather** (commands embedded in the skill):
   - Git: commits per author + over time, lines ±, branches, active days — across all branches.
   - PRs: `gh pr list --state all --json …`; extract CodeRabbit (`coderabbitai`) and Claude
     (`claude`) summaries + review/finding counts from `gh pr view <n> --json comments,reviews`.
   - Board: Projects v2 via `gh api graphql` on project id `PVT_kwHOBk5vnc4BZNux`
     (status field options: Backlog, Spec / Research, Ready, In progress, In review, Done).
   - Docs/decisions: ADRs under `docs/02-architecture/adr/`, spikes under
     `docs/superpowers/`, milestones/roadmap under `docs/01-product/` & `docs/07-milestones.md`.
3. **Compute KPIs** → **synthesise** the qualitative analysis (Claude reasons over the
   gathered artifacts) → **render** a single self-contained HTML (vendored Chart.js inlined,
   data inlined) → **verify** the file (exists, well-formed, charts present) → report path.

### Data-source reality (verified 2026-06-25)

- The board (`projects/3`) currently holds **2 items**; **archived items are not retrievable
  via the GitHub API** (Projects v2 limitation). The report therefore treats **merged PRs as
  the primary work unit** (21 PRs: 20 merged + 1 open at authoring time); the board is a
  light supplement. There are **0 issues** on the repo.
- Contribution on `main` is currently bootstrap-phase / single-committer; the report
  broadens the lens to all branches and adds a factual context note (decided per Open
  questions below).

## Data model

No Ankhora masterclass-schema impact. The report inlines a JSON data blob (gathered KPIs +
series) into the HTML; no persisted schema.

## Acceptance criteria

- [ ] `insights-report` skill exists at `.claude/skills/insights-report/SKILL.md` and fires
      on "insights/progress/follow-up report" intents.
- [ ] Running it produces `reports/insights-YYYY-MM-DD.html`, a **single self-contained file**
      that opens offline (no network) and renders all charts.
- [ ] Report covers every section listed in *What*, with charts for velocity, contribution,
      board status, and quality.
- [ ] Board archived-item limitation and the contribution context note are stated in-report
      (honest provenance).
- [ ] A first report for the 2026-06-25 follow-up is generated, verified, and committed.

## Out of scope

- Pulling archived Projects v2 items (GitHub API limitation — documented, not worked around).
- Auto-publishing/hosting the report (it is a committed local artifact).
- Per-PR deep code analysis beyond the CodeRabbit/Claude summaries already present.

## Open questions

- Contribution framing — **resolved**: honest + broadened to all branches with a
  bootstrap-phase context note (team iterates device-side on Windows/Link; not yet merged).

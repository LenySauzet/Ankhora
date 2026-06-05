---
description: Mirror origin/main to the Epitech submission repo (manual fallback)
argument-hint: "[dry-run]"
---

# Mirror origin/main to the Epitech submission repo

Push whatever is on `origin/main` of `LenySauzet/Ankhora` to the Epitech mirror
`EpitechMscProPromo2026/T-VIR-902-MPL_2`. Manual fallback while the `mirror-epitech.yml`
workflow is disabled (PAT pending approval).

> **Canonical procedure:** follow `@.cursor/commands/mirror-epitech.md` — the exact commands,
> the LFS-budget limitation, the branch-agnostic behaviour, and the deprecation steps all live
> there. This Claude command is a thin pointer so Cursor and Claude stay in sync.

## Critical guardrails (restated)

- Mirrors **`origin/main` only** — never feature branches, tags, or local state. The command
  is branch-agnostic and ignores your working tree.
- Never `git push --mirror`. The `+refs/remotes/origin/main:refs/heads/main` force-push to the
  Epitech remote is intentional (mirror-only repo).
- `--no-verify` is used **only** to skip the LFS pre-push hook against the Epitech remote
  (its LFS budget is exhausted) — not a general license to skip hooks.
- The Epitech repo is write-only; never edit it by hand.

## Arguments

- `/mirror-epitech` — sync once.
- `/mirror-epitech dry-run` — confirm the reachable remote + SHA without pushing.

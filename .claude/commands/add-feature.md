---
description: Scaffold a new Ankhora feature (tracking issue + branch + spec doc)
argument-hint: "[scope] [slug] \"<title>\" @owner"
---

# Scaffold a new feature (issue + branch + spec)

Create the standard artefacts to start a feature on Ankhora: a tracking issue, a
`feat/<scope>-<slug>` branch, and a spec doc — following the team workflow.

> **Canonical workflow:** follow `@.cursor/commands/add-feature.md` (the full step list,
> spec template, scope list, and arguments live there) and `@.cursor/rules/003-team-workflow.mdc`.
> This Claude command is a thin pointer so Cursor and Claude stay in sync from one source.

## Critical guardrails (restated so they hold even standalone)

- Branch name `feat/<scope>-<slug>`; **refuse** if it already exists locally or on `origin`.
- Create the spec file and stage it **before** creating the GitHub issue (no orphan issue
  with no doc). Branch ↔ spec ↔ issue must cross-reference each other.
- Never push to a branch other than the one just created. Never push to `main`.
- Use the `git-commit` skill / Conventional Commits for the placeholder commit
  (`docs(<scope>): add spec for <title> (#<issue>)`).

## Arguments

- `/add-feature` — ask for scope, slug, title, owner.
- `/add-feature recorder hand-capture "Capture hand joints to JSON timeline" @teammate` — fully specified.

## Expected output

Issue URL · spec path · branch name (checked out) · suggested next step.

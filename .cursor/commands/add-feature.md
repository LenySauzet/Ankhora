# Scaffold a new feature (issue + branch + spec)

Create the standard artefacts to start a new feature on Ankhora: a tracking issue, a feature branch, and a feature spec doc. Follows team workflow conventions (`@.cursor/rules/003-team-workflow.mdc`).

## Required inputs

1. **Scope** — one of `recorder`, `playback`, `xr`, `ui`, `domain`, `infra`, `ci`, `docs`, `tooling`.
2. **Slug** — kebab-case short identifier (e.g. `hand-capture`, `ghost-rendering`).
3. **Title** — one sentence, imperative (e.g. "Capture hand joints to JSON timeline").
4. **Owner** — GitHub handle of the dev who will pick it up.

Ask only what the user did not provide after `/add-feature`.

## Instructions

When the user invokes `/add-feature`:

1. Validate scope is in the allowed list. Validate slug is kebab-case.
2. Compute the branch name: `feat/<scope>-<slug>`. Refuse if a branch with that name already exists locally or on `origin`.
3. Create the spec file at `docs/03-xr/<scope>-<slug>.md` (or under the right area if scope is not `xr`) with the template below.
4. Create a GitHub issue on `LenySauzet/Ankhora` (use `gh issue create`) with title `feat(<scope>): <title>`, body pointing at the spec file, labels `feature`, `scope/<scope>`, and assignee = owner. Capture the issue number.
5. Create and check out the branch:
   ```
   git checkout main && git pull --ff-only
   git checkout -b feat/<scope>-<slug>
   ```
6. Add a placeholder commit so the branch can be pushed immediately:
   ```
   git add docs/03-xr/<scope>-<slug>.md
   git commit -m "docs(<scope>): add spec for <title> (#<issueNumber>)"
   git push -u origin feat/<scope>-<slug>
   ```
7. Optionally open a draft PR with title `feat(<scope>): <title>` referencing the issue (`Closes #<issueNumber>`).

## Spec template

```markdown
# <Scope> · <Title>

- Status: draft | in-progress | shipped
- Owner: @<github-handle>
- Tracking issue: #<NN>
- Last updated: YYYY-MM-DD

## Why
What problem this solves. Tie back to the MVP (see `docs/01-product/mvp-scope.md`) or explain why it is in scope.

## What
User-facing behaviour described from the expert / learner perspective.

## How
Architecture sketch. Reference files / MonoBehaviours / scenes that will be touched. Reference Meta SDK APIs explicitly with doc links.

## Data model
If the feature touches the masterclass JSON schema, show the diff here.

## Acceptance criteria
- [ ] …
- [ ] Runs at 90 FPS on Quest 3 with passthrough enabled.
- [ ] No allocation in hot loop (verify via Profiler).

## Out of scope
List nearby features intentionally left out — link to follow-up issues.

## Open questions
- …
```

## Arguments

- `/add-feature` — ask for all inputs.
- `/add-feature recorder hand-capture "Capture hand joints to JSON timeline" @teammate1` — fully specified.

## Key rules

- Never push to a branch other than the one just created.
- Never create the issue before the spec file is staged — if the issue creation succeeds but the spec write fails, you have an orphan issue with no doc.
- Branch + spec + issue must reference each other (issue cites spec, spec cites issue, PR cites both).

## Expected output

Respond with:

1. Issue URL.
2. Spec file path.
3. Branch name (and confirmation it is checked out).
4. Next-step suggestion (e.g. `/build-android dev no` after first commit).

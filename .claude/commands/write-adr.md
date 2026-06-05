---
description: Scaffold the next numbered Architecture Decision Record from the template
argument-hint: "<short decision title>"
---

# Write an ADR

Create the next Architecture Decision Record under `docs/02-architecture/adr/`, using
the project template. ADRs are how Ankhora records decisions of consequence
(`@CLAUDE.md` § *Conventions*); the `claude-review.yml` action reads this folder.

## Instructions

1. The decision title comes from `$ARGUMENTS`. If empty, ask for a one-line title.
2. Determine the next number: list `docs/02-architecture/adr/`, take the highest
   `NNNN-` prefix (ignoring `0000-adr-template.md`), add 1, zero-pad to 4 digits.
3. Derive the slug: lowercase the title, spaces → `-`, strip punctuation.
4. Copy `docs/02-architecture/adr/0000-adr-template.md` to
   `docs/02-architecture/adr/<NNNN>-<slug>.md`.
5. Fill what you can confidently from the conversation and the repo:
   - **Status:** `Proposed` unless the user says the decision is already made (`Accepted`).
   - **Date:** today (`date +%F`).
   - **Context, drivers, options, decision, consequences:** draft from the discussion.
     Leave a clearly-marked `<TODO>` for anything you cannot source — never invent
     versions, file paths, or trade-offs. Pull real facts from `@CLAUDE.md` and
     `@research/xr-platform-master-research.md` when relevant.
6. Keep it short — an ADR is a decision record, not a design doc.

## Key rules

- One decision per ADR. If the discussion covers several, propose splitting.
- Do not renumber or edit existing ADRs (supersede them instead: set the old one's
  status to `Superseded by [ADR-XXXX]` and link forward).
- Do not commit — leave the file staged for the user to review (`@CLAUDE.md`: PRs need
  a human review).

## Verification

- [ ] New file at `docs/02-architecture/adr/<NNNN>-<slug>.md`, number is unique and sequential.
- [ ] No invented facts; every unknown is a visible `<TODO>`.
- [ ] Reported: the path created and a 2-line summary of the decision captured.

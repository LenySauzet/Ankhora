<!--
PR title must be a Conventional Commit (squash-merge uses the PR title as the commit message).
Examples:
  feat(recorder): persist hand joints to JSON timeline
  fix(passthrough): suppress flicker on scene reload
  docs(architecture): add ADR 0004 on hand tracking strategy
-->

## What

<!-- One paragraph. What does this PR change, from a user-facing or system-facing point of view? -->

## Why

<!-- One paragraph. What problem does this solve? Link the tracking issue. -->

Closes #

## How

<!-- Bullet points are fine. What changed at the code level? Files / scenes / packages touched. -->

## Screens / video (XR work)

<!-- If this PR touches anything visible in the headset, attach a short capture (MQDH > Recording).
     Required for UI / passthrough / hand tracking / animation changes. -->

## Verification

<!-- How did you verify this works? Check all that apply. -->

- [ ] `git lfs install` was already run on my clone before staging binaries.
- [ ] Tested in Unity Editor (Mac: Meta XR Simulator / Windows: Quest Link).
- [ ] Built and ran on Quest 3 device (APK + adb install).
- [ ] OVR Metrics Tool shows ≥ 90 FPS with passthrough enabled.
- [ ] No regression on existing scenes I touched.
- [ ] EditMode + PlayMode tests pass (`Window > General > Test Runner`).

## Architectural impact

<!-- Did this PR introduce or change an architectural decision? If yes, link the ADR. -->

- [ ] No architectural change.
- [ ] ADR added / updated: `docs/02-architecture/adr/NNNN-<slug>.md`.

## Notes for reviewers

<!-- Anything specific to look at? Risky area? Known limitation deferred to a follow-up? -->

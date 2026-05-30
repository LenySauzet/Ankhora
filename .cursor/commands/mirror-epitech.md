# Mirror origin/main to the Epitech submission repo

Push whatever is currently on `origin/main` of `LenySauzet/Ankhora` to the Epitech submission repo `EpitechMscProPromo2026/T-VIR-902-MPL_2`. Used as a manual fallback while the `mirror-epitech.yml` workflow is disabled (because the fine-grained PAT `Ankhora mirror to Epitech` is still in `Pending` status awaiting Epitech org admin approval).

**The command does not care about your local branch or your working tree state.** It mirrors what is on `origin/main` at the moment of invocation, full stop. You can run it from a feature branch with a dirty working tree — that local state is never read or pushed.

## Required inputs

None.

## Instructions

When the user invokes `/mirror-epitech`:

1. Fetch the current `origin/main` ref without touching the local branches:
   ```bash
   git fetch origin main
   ```
   If the fetch fails (network error, auth issue with `origin`), **abort** with the error verbatim.

2. Capture the SHA that is about to be mirrored, for the report at the end:
   ```bash
   ORIGIN_SHA="$(git rev-parse origin/main)"
   ```

3. Ensure the `epitech` remote exists; add it if not:
   ```bash
   git remote get-url epitech 2>/dev/null \
     || git remote add epitech https://github.com/EpitechMscProPromo2026/T-VIR-902-MPL_2.git
   ```

4. Push the `origin/main` ref straight onto `epitech/main` with force, **skipping the LFS upload step** (the Epitech org exceeded its Git LFS budget and no Owner is currently reachable to raise it — see "Known limitation" below):
   ```bash
   git push --no-verify epitech "+refs/remotes/origin/main:refs/heads/main"
   ```
   - The `+` prefix performs a force-update of remote `main`. This is intentional — the Epitech repo is mirror-only, never edited by hand.
   - Pushing `refs/remotes/origin/main` (not `refs/heads/main`) means the command works from any local branch, in any local state.
   - `--no-verify` skips the LFS pre-push hook so the push goes through even though the Epitech LFS endpoint refuses uploads. The repo lands on Epitech with the same git objects and history; only the LFS-tracked binary blobs (currently `Cover.png` and `Assets/TutorialInfo/Icons/URP.png`) are absent — they remain as LFS pointer files. This is acceptable for a submission repo whose role is to prove the work, not to be cloned and built downstream.

5. Verify the Epitech remote now reflects the same SHA:
   ```bash
   EPITECH_SHA="$(git ls-remote epitech main | awk '{print $1}')"
   [ "$EPITECH_SHA" = "$ORIGIN_SHA" ]   # must be true
   ```

## Known limitation — LFS budget exhausted on the Epitech org

The Epitech org `EpitechMscProPromo2026` has 0 GB of Git LFS budget remaining (`This repository exceeded its LFS budget`). Until an Epitech Owner raises it, the mirror **cannot** ship LFS objects. The workaround above pushes git refs + LFS pointer files only — the binary blobs themselves stay on `LenySauzet/Ankhora`.

This is normally fine because the Epitech repo's job is to expose the work to the school's grading process, not to be cloned and run. But: keep new LFS-tracked binaries minimal. If a binary asset is genuinely needed downstream, host it externally (a Release on the working repo, an S3 bucket, etc.) and reference it from code rather than committing it as LFS.

To restore full mirroring once the budget is raised:

1. Remove the `--no-verify` flag from step 4 and reinstate the `git lfs fetch origin main` + `git lfs push --all epitech` step from before.
2. Run `/mirror-epitech` once to backfill the missing LFS objects on Epitech.

## Arguments

- `/mirror-epitech` — sync `origin/main` once (the default).
- `/mirror-epitech dry-run` — run steps 1–3 without pushing; useful to confirm the remote is reachable and to see which SHA would be mirrored.

## Key rules

- **Do not push** anything other than `origin/main`. No feature branches, no tags.
- **Never** use `git push --mirror` — it would push every local branch including throwaway work.
- `--no-verify` is used here **specifically and only** to bypass the LFS pre-push hook against the Epitech remote. It is not a general license to skip hooks — see the rationale in the "Known limitation" section above.
- The user pushes with their own GitHub credentials (HTTPS via `gh auth` or SSH key). If they see a 403, the issue is on Lény's side (token expired, gh CLI logged out), not the Epitech repo's side — surface the raw error clearly.
- Do **not** check or warn about the local branch the user is on. The command is intentionally branch-agnostic.
- Do **not** check the local working tree cleanliness. Uncommitted local changes are unrelated to what is being mirrored.

## Expected output

Respond with:

1. The SHA mirrored (from step 2), with its commit subject for context (`git log -1 --format='%h %s' origin/main`).
2. The `git push` command run and its result.
3. The Epitech-side `main` SHA after the push, confirmed equal to `origin/main`.
4. A reminder to invoke this command again after any further merge to `main`.

## When to deprecate this command

Once Lény's PAT `Ankhora mirror to Epitech` is approved by an Epitech org Owner:

1. Re-enable the workflow:
   ```bash
   gh workflow enable mirror-epitech.yml
   ```
2. Delete this command file and remove its mention from `SETUP-NEXT-STEPS.md`.
3. Trigger a one-shot run to confirm: `gh workflow run mirror-epitech.yml`.

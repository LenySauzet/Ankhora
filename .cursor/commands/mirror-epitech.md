# Mirror main to the Epitech submission repo

Push the current `main` of `LenySauzet/Ankhora` to the Epitech submission repo `EpitechMscProPromo2026/T-VIR-902-MPL_2`. Used as a manual fallback while the `mirror-epitech.yml` workflow is disabled (because the fine-grained PAT `Ankhora mirror to Epitech` is still in `Pending` status awaiting Epitech org admin approval).

## Required inputs

None. The command operates on the current working tree.

## Instructions

When the user invokes `/mirror-epitech`:

1. Verify the working tree is clean and on `main`:
   ```bash
   git rev-parse --abbrev-ref HEAD   # must print "main"
   git status --porcelain            # must be empty
   ```
   If the tree is dirty or the branch is not `main`, **abort** with a clear message telling the user to commit/stash and `git checkout main`.

2. Verify `main` is up-to-date with `origin`:
   ```bash
   git fetch origin main
   [ "$(git rev-parse HEAD)" = "$(git rev-parse origin/main)" ] || abort
   ```
   If local is behind, run `git pull --ff-only origin main` first.

3. Make sure the `epitech` remote exists; add it if not:
   ```bash
   git remote get-url epitech 2>/dev/null \
     || git remote add epitech https://github.com/EpitechMscProPromo2026/T-VIR-902-MPL_2.git
   ```

4. Push LFS objects then refs to the Epitech remote:
   ```bash
   git lfs push --all epitech
   git push epitech +refs/heads/main:refs/heads/main
   ```
   The `+` prefix performs a force-update of remote `main`. This is intentional — the Epitech repo is mirror-only, never edited by hand.

5. Report the resulting commit SHA on the Epitech remote:
   ```bash
   git ls-remote epitech main | awk '{print $1}'
   ```

## Arguments

- `/mirror-epitech` — sync `main` once (the default).
- `/mirror-epitech dry-run` — run steps 1–3 without pushing; useful to confirm the local tree is clean and the remote is reachable.

## Key rules

- **Do not run** if `main` is dirty or behind `origin/main`. The Epitech repo must always reflect what's been merged on the working repo.
- **Never push** any branch other than `main` to the Epitech remote.
- **Never** use `git push --mirror` — it would push every local branch including throwaway work.
- Surface authentication errors clearly: the user pushes with their own GitHub credentials (HTTPS via `gh auth` or SSH key). If they see a 403, the issue is on Lény's side (token expired, gh CLI logged out), not the Epitech repo's side.

## Expected output

Respond with:

1. Local state check summary (branch, dirty status, ahead/behind origin).
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

# SETUP-NEXT-STEPS

> Browser-only actions Lény (or a teammate) must do once before CI workflows can run green.
> Time budget: ~30–45 min total. Delete this file once every checkbox is done.

## 1. GitHub repo configuration

### 1.1 Push the initial commit

The local repo has been `git init`-ed with a first commit on `main`. From this folder:

```bash
git remote add origin https://github.com/LenySauzet/Ankhora.git
git push -u origin main
```

### 1.2 Branch protection on `main`

`Settings > Branches > Add rule` → pattern `main`:

- ☑️ Require a pull request before merging
  - ☑️ Require approvals: **1**
  - ☑️ Dismiss stale pull request approvals when new commits are pushed
  - ☑️ Require review from Code Owners
- ☑️ Require status checks to pass before merging
  - Add these required checks (they appear once the workflows have run once):
    - `CI — Unity build verify / Build Quest APK (Android, IL2CPP, ARM64)`
    - `Claude Code review / Project-aware review`
    - `CodeRabbit` (added after step 4 below)
- ☑️ Require conversation resolution before merging
- ☑️ Require linear history
- ☑️ Do not allow bypassing the above settings

### 1.3 Allowed merge strategies

`Settings > General > Pull Requests`:

- ☐ **Allow merge commits** — OFF (no merge bubbles, keep `main` linear)
- ☑️ **Allow squash merging** — ON. Click the "Default commit message" dropdown and pick **"Pull request title"** (NOT "Default message"). This is what makes squash-merge + Conventional Commits work — every commit on `main` becomes a clean `feat(...)` / `fix(...)` line, suitable for an auto-generated `CHANGELOG.md`.
- ☐ **Allow rebase merging** — OFF (would replay each WIP commit onto `main` and pollute history)
- ☐ **Always suggest updating PR branches** — OFF (manual rebase keeps merge commits off feature branches)
- ☑️ **Allow auto-merge** — ON. Lets a reviewer hit "Auto-merge" and have GitHub squash-merge as soon as CI + reviews are green. Saves time when GameCI takes ~5-10 min.
- ☑️ **Automatically delete head branches** — ON (feature branches disappear after merge)

### 1.4 GitHub Project (kanban)

`Projects > New project > Board template`:

- Name: **Ankhora MVP**
- Columns: `Backlog`, `Spec / Research`, `Ready`, `In progress`, `In review`, `Done`
- Workflow: enable "Auto-add to project" for `LenySauzet/Ankhora` issues + PRs
- Add the 3 teammates as project collaborators

## 2. Secrets — required for CI

Add each as a **repository secret** in `Settings > Secrets and variables > Actions > New repository secret`.

### 2.1 Unity license — depends on your license type

#### Path A — Student / Plus / Pro license with a visible serial — Ankhora is on this path ✅

`ci.yml` is wired for this path. Add three repository secrets at `Settings > Secrets and variables > Actions > New repository secret`:

| Secret name | Value | Where to find it |
|---|---|---|
| `UNITY_EMAIL` | Unity ID email | The one you log into Unity Hub with |
| `UNITY_PASSWORD` | Unity ID password | Same login |
| `UNITY_SERIAL` | Unity serial number (`Sx-XXXX-XXXX-XXXX-XXXX-XXXX`) | `id.unity.com` → Account → Subscriptions, or the email Unity sent when issuing the Student license |

> Treat the serial like a password — do not commit it, do not paste it in chat. Once it is in GitHub Secrets it is write-only (you can replace it but not read it back). Keep a copy in your password manager too.

After saving the three secrets, that is it — open a test PR to confirm the build job authenticates.

#### Path B — Personal license (no visible serial)

Only if you have a Personal license. Generate a `.ulf` via the manual activation flow:

1. In Ankhora, create a one-shot workflow `.github/workflows/_unity-activation.yml` on a throwaway branch:
   ```yaml
   name: Unity — request activation file (one-shot)
   on:
     workflow_dispatch: {}
   jobs:
     request:
       runs-on: ubuntu-latest
       steps:
         - uses: game-ci/unity-request-activation-file@v2
           id: getManualLicenseFile
           with:
             unityVersion: 2022.3.62f3
         - uses: actions/upload-artifact@v4
           with:
             name: Unity_v2022.3.62f3.alf
             path: ${{ steps.getManualLicenseFile.outputs.filePath }}
   ```
2. Push the branch, run the workflow manually from `Actions`, download the `.alf` artifact.
3. Upload `.alf` to <https://license.unity3d.com/manual>, sign in, pick **Personal**, download the `Unity_v2022.x.ulf`.
4. Paste the full XML content of the `.ulf` as the value of secret `UNITY_LICENSE`.
5. Delete the throwaway workflow + branch. Ping me to swap `ci.yml` to consume `UNITY_LICENSE`.

### 2.2 `CLAUDE_CODE_OAUTH_TOKEN`

For the Claude Code GitHub Action — uses your Claude Pro / Max subscription, no extra cost.

```bash
# Locally:
claude setup-token
```

Copy the printed token into the secret `CLAUDE_CODE_OAUTH_TOKEN`.

Doc: <https://github.com/anthropics/claude-code-action#authentication>

### 2.3 `EPITECH_PAT`

PAT used by `mirror-epitech.yml` to push `main` to `EpitechMscProPromo2026/T-VIR-902-MPL_2`.

1. `github.com > Settings > Developer settings > Personal access tokens > Fine-grained tokens > Generate new token`.
2. Token name: `Ankhora mirror`.
3. Expiration: pick a date past the project deadline (e.g. 90 days).
4. Resource owner: `EpitechMscProPromo2026`.
5. Repository access: only `T-VIR-902-MPL_2`.
6. Permissions:
   - **Contents**: Read and write
   - **Metadata**: Read-only (auto-set)
7. Generate, copy the value, save it as secret `EPITECH_PAT`.

If the Epitech org requires the token to be approved, an Epitech admin must approve it (one-shot).

#### Current status — Mirror disabled, manual fallback active

As of 2026-05-30, the PAT is in `Pending` status with no Epitech Owner currently reachable. The `mirror-epitech.yml` workflow has been **disabled** to avoid red checks on every push:

```bash
gh workflow disable mirror-epitech.yml   # already done
```

Until an Epitech Owner approves the PAT, sync `main` to the Epitech repo **by hand** after each merge using Lény's personal GitHub credentials (which already have push access to `T-VIR-902-MPL_2`). The commands below mirror `origin/main` from any branch with any working tree state — they never read your local branch and they skip LFS uploads because the Epitech org's LFS budget is exhausted:

```bash
git fetch origin main
git remote get-url epitech 2>/dev/null \
  || git remote add epitech https://github.com/EpitechMscProPromo2026/T-VIR-902-MPL_2.git
git push --no-verify epitech +refs/remotes/origin/main:refs/heads/main
```

Easier: run **`/mirror-epitech`** in Cursor — the command at `.cursor/commands/mirror-epitech.md` runs the same sequence.

> **LFS limitation (downstream only)**: the Epitech org reports `This repository exceeded its LFS budget`. The push above skips the LFS pre-push hook (`--no-verify`) so git refs and pointer files still land on Epitech, but the binary blobs themselves stay only on `LenySauzet/Ankhora`. **This does not constrain the team's workflow on the working repo** — keep adding LFS-tracked assets freely. The Epitech repo is a grading-visibility view, not a clone-and-build target; graders see file existence, history, and commits — that's enough for evaluation. If the budget is ever raised, remove `--no-verify`, re-add `git lfs fetch origin main && git lfs push --all epitech` before the ref push, and run `/mirror-epitech` once to backfill.

When the PAT is eventually approved:

```bash
gh workflow enable mirror-epitech.yml
gh workflow run mirror-epitech.yml
# Delete .cursor/commands/mirror-epitech.md and this subsection
```

## 3. CodeRabbit (free for public OSS repos)

1. Go to <https://app.coderabbit.ai>, sign in with GitHub.
2. Install the CodeRabbit GitHub app on `LenySauzet/Ankhora`.
3. Plan: select **Open Source** (free) — Ankhora is a public repo, eligible.
4. Default config is fine; CodeRabbit picks up `.github/.coderabbit.yaml` if you want to tune it later.
5. Open a test PR after step 5 below to confirm CodeRabbit posts a summary + walkthrough.

Doc: <https://docs.coderabbit.ai/getting-started/quickstart>

## 4. Local — each teammate, once per clone

After cloning the repo:

```bash
# 1. Install Git LFS hooks (binary assets pass through LFS)
git lfs install
git lfs pull

# 2. Register the Unity YAML merge driver (scene / prefab merges)
# macOS:
git config merge.unityyamlmerge.name "Unity YAML merge"
git config merge.unityyamlmerge.driver \
  "/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/Tools/UnityYAMLMerge merge -p %O %B %A %A"
# Windows:
git config merge.unityyamlmerge.name "Unity YAML merge"
git config merge.unityyamlmerge.driver \
  "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/Tools/UnityYAMLMerge.exe merge -p %O %B %A %A"
```

## 5. Verify everything is wired

Open a throwaway PR that edits a single file (e.g. fix a typo in `README.md`):

- [ ] GameCI workflow runs and either passes or fails on a real Unity error (not on missing secret).
- [ ] CodeRabbit posts a summary within ~2 min.
- [ ] Claude Code action posts a review within ~2 min.
- [ ] Required checks block the merge until they go green.
- [ ] After merging, `mirror-epitech` runs and `main` shows up on `EpitechMscProPromo2026/T-VIR-902-MPL_2`.

Once every box is checked, delete this `SETUP-NEXT-STEPS.md` file.

## Reference

- Cursor rules: [`.cursor/rules/`](.cursor/rules/)
- Project context: [`CLAUDE.md`](CLAUDE.md)
- Research dossier: [`research/xr-platform-master-research.md`](research/xr-platform-master-research.md)

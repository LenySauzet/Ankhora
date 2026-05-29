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

- ☑️ Allow squash merging (default message: **Pull request title**)
- ☐ Allow merge commits (off)
- ☐ Allow rebase merging (off)
- ☑️ Automatically delete head branches

### 1.4 GitHub Project (kanban)

`Projects > New project > Board template`:

- Name: **Ankhora MVP**
- Columns: `Backlog`, `Spec / Research`, `Ready`, `In progress`, `In review`, `Done`
- Workflow: enable "Auto-add to project" for `LenySauzet/Ankhora` issues + PRs
- Add the 3 teammates as project collaborators

## 2. Secrets — required for CI

Add each as a **repository secret** in `Settings > Secrets and variables > Actions > New repository secret`.

### 2.1 `UNITY_LICENSE` (+ email + password)

GameCI needs a Unity Personal licence file. Generate it once:

1. Locally, install Unity Hub if not already done.
2. Run the GameCI activation workflow once: clone <https://github.com/game-ci/unity-actions> and follow `unity-request-activation-file/README.md`. The 5-min flow yields a `.alf` file.
3. Upload that `.alf` to <https://license.unity3d.com/manual>, sign in, download the resulting `Unity_v2022.x.ulf`.
4. Copy the file's content as the value of secret `UNITY_LICENSE`.
5. Also add `UNITY_EMAIL` and `UNITY_PASSWORD` (the Unity ID credentials).

> Personal licence is fine for student / non-commercial use. Re-issue annually if it expires.

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

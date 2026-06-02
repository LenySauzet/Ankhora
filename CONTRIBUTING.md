# Contributing to Ankhora

Thanks for your interest in Ankhora — a Meta Quest XR platform for recording and
replaying immersive spatial masterclasses.

Ankhora is built as a 3-person Epitech MSc Pro group project, but the repository
is public and open source under the [Apache License 2.0](LICENSE). Contributions
are welcome. By contributing, you agree that your contributions are licensed
under Apache-2.0 and that you have the right to submit them.

Please also read our [Code of Conduct](CODE_OF_CONDUCT.md).

## Project status

The project is in **spec / planning phase**. The build pipeline is not yet wired
(no XR plugin, no Quest target configured). Until that lands, most contributions
are documentation, specs, and tooling rather than runtime feature code.

## Development environment

- **Unity Editor**: `2022.3.62f3` LTS — **all contributors must use this exact
  version**. Editor version mismatches are a frequent cause of avoidable
  scene/prefab diffs.
- **Render pipeline**: URP `14.0.12`.
- **Target device**: Meta Quest 3.
- **Asset Serialization Mode**: `Force Text` (required for diffable scenes and
  prefabs). Verify in `Edit > Project Settings > Editor`.
- **Iteration**:
  - macOS: `Build APK → adb install` on device, or the Meta XR Simulator. Quest
    Link / Air Link are not available on macOS.
  - Windows: Quest Link is available for fast in-Editor iteration.

## Workflow

1. **Branch** off `main` with a short-lived branch:
   - `feat/<scope>-<slug>` for features
   - `fix/<slug>` for bug fixes
   - `docs/<slug>`, `chore/<slug>`, `refactor/<slug>` as appropriate

   Never push directly to `main`.

2. **Commit** using [Conventional Commits](https://www.conventionalcommits.org/)
   (`feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`, …). One logical
   change per commit, imperative mood ("add", not "added").

3. **Open a Pull Request** against `main`. Every PR requires:
   - at least one human review,
   - green CI (build + AI code review).

   PRs are merged with **squash and merge** to keep `main` history linear.

> **Dual-repo note.** The working repository where PRs land is
> [`LenySauzet/Ankhora`](https://github.com/LenySauzet/Ankhora). The Epitech
> submission repository (`EpitechMscProPromo2026/T-VIR-902-MPL_2`) is a mirror
> kept in sync automatically by CI on every merge to `main`. **Do not push to
> the Epitech repository by hand.**

## Code style

- **Language**: all code, comments, file content, and documentation in
  **English**. (The team communicates in French; deliverables stay in English.)
- **C#**: standard Unity conventions — `PascalCase` for types, `camelCase` for
  fields, prefer `[SerializeField] private` over public fields.
- **XR features**: once the Meta XR SDK is installed, prefer **Meta Building
  Blocks + the Project Setup Tool** over manual rigging. Hand-tracking
  interactions must go through Meta's **Interaction SDK** (bypassing it risks
  Meta Horizon Store rejection).

## Reporting issues

Use GitHub Issues for bugs and feature requests. Please include your Editor
version, target device, and clear reproduction steps where relevant.

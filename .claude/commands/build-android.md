---
description: Build the Quest 3 APK (Unity 6, Android/ARM64/IL2CPP) and optionally adb install
argument-hint: "[dev|release] [install|no-install]"
---

# Build Android APK for Quest 3

Build the Unity project for Meta Quest 3 (Android, ARM64, IL2CPP, OpenXR + Meta
feature group) and optionally install it on a connected device.

> Read `@CLAUDE.md` § *What "build" / "run" / "test" mean today* first — it is the
> authority on the current build state. **The Quest APK is built locally on device,
> never in CI** (Meta XR SDK 201.0.0's `OVRProjectConfig` cctor throws on headless
> Linux). On the **Mac station there is no Quest Link** — iteration is Build & Run on
> device or the Meta XR Simulator; do not suggest Link-based flows on Mac.

## Arguments

`$ARGUMENTS` may contain two tokens, in any order:
- `dev` (default) or `release` — `dev` keeps Development Build + Script Debugging ON.
- `install` (default) or `no-install` — whether to `adb install -r` after the build.

Ask only for tokens the user did not provide.

## Instructions

1. **Preconditions** (abort with a clear one-line error on the first failure):
   - `Packages/manifest.json` contains `com.unity.xr.openxr` and `com.meta.xr.sdk.all`
     (the XR stack must be wired).
   - `adb` is on PATH (`which adb`).
   - If installing: `adb devices` lists ≥1 device in state `device` (not `unauthorized`/`offline`).
2. **Resolve the Unity 6 editor path** (pinned `6000.4.10f1` — all machines must match):
   - macOS: `/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity`
   - Windows: `C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe`
   - Fall back to `~/Applications/...` on macOS if the system path is missing.
   - If the resolved editor is missing, stop and tell the user to install `6000.4.10f1` via Unity Hub.
3. **Build** in batchmode:
   ```bash
   "<UnityPath>" -batchmode -quit -nographics \
     -projectPath . \
     -buildTarget Android \
     -executeMethod Ankhora.Editor.BuildScript.BuildQuestApk \
     -logFile - \
     -outputPath Build/Android/Ankhora-<mode>-<shortSha>.apk
   ```
   If `Ankhora.Editor.BuildScript.BuildQuestApk` does not exist yet, stop and say:
   "no build script yet — add one under `Assets/Editor/BuildScript.cs` (must set
   IL2CPP + ARM64 + the OpenXR/Meta feature group for Android)." Do not invent a fallback.
4. Report the build summary: APK path, size, duration, warning count, error count.
5. **If installing:** `adb install -r <apk-path>`, report the exit code, then *suggest*
   (do not run) the launch command:
   `adb shell am start -n com.tolkai.ankhora/com.unity3d.player.UnityPlayerActivity`.

## Key rules

- Never push to a device labelled `unauthorized` — ask the user to accept the USB-debug
  prompt in the headset first.
- Never auto-launch the app — only suggest the command. The user owns device focus.
- If the only change is docs (`*.md`, `docs/`), do not run a build — explain and bail.
- Never commit the APK (`*.apk` is gitignored).

## Verification

- [ ] Editor `6000.4.10f1` resolved.
- [ ] Build reports 0 errors and an APK path under `Build/Android/`.
- [ ] If installed: `adb install -r` returned `Success`.

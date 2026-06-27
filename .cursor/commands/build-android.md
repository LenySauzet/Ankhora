# Build Android APK for Quest 3

Build the Unity project for Meta Quest 3 (Android, ARM64, IL2CPP, Vulkan) and optionally install it on a connected device.

## Required inputs

1. **Mode** — `dev` (default) or `release`. `dev` keeps Development Build + Script Debugging ON.
2. **Install** — `yes` (default) or `no`. If `yes`, run `adb install -r` after the build.

Ask only if the user did not specify these after `/build-android`.

## Instructions

When the user invokes `/build-android`:

1. Verify prerequisites in this order, abort with a clear error if any fails:
   - `Packages/manifest.json` contains `com.unity.xr.openxr` and `com.meta.xr.sdk.all` (the XR stack is wired since the Unity 6 migration). The Quest APK is built locally on device, never in CI; on Mac there is no Quest Link (Build & Run / Meta XR Simulator).
   - `adb` is on PATH (`which adb`).
   - If `install=yes`, `adb devices` lists at least one device whose state is `device` (not `unauthorized` or `offline`).
2. Resolve Unity's command-line path on the current machine:
   - macOS: `/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity`
   - Windows: `C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe`
   - Use `~/Applications/...` fallback on macOS if the system path is missing.
3. Build via Unity in batchmode using the project's build script:
   ```
   "<UnityPath>" -batchmode -quit -nographics \
     -projectPath . \
     -buildTarget Android \
     -executeMethod Ankhora.Editor.BuildScript.BuildQuestApk \
     -logFile - \
     -outputPath Build/Android/Ankhora-{mode}-{shortSha}.apk
   ```
   If `Ankhora.Editor.BuildScript.BuildQuestApk` does not exist yet, tell the user "no build script yet, add one under `Assets/Editor/BuildScript.cs`" and stop — do not invent a fallback.
4. Surface the build summary: APK path, size, duration, warning count, error count.
5. If `install=yes`:
   - `adb install -r <apk-path>` and report exit code.
   - Suggest `adb shell am start -n com.ankhora.app/com.unity3d.player.UnityPlayerActivity` to launch.

## Arguments

- `/build-android` — dev build, install yes (defaults).
- `/build-android release` — release build, install yes.
- `/build-android dev no` — dev build, no install.

## Key rules

- Never push to a device labelled `unauthorized` — first ask the user to accept the USB-debug prompt in the headset.
- Never auto-launch the app — only suggest the command. The user owns the device focus.
- Do not run a fresh Unity build when the only change is a doc file (`*.md`, `docs/`) — explain and bail.
- Do not commit the resulting APK. `*.apk` is already in `.gitignore`.

## Expected output

Respond with:

1. Prerequisite check summary (one line per check).
2. Build command run.
3. Build result + APK path.
4. Install result (if applicable) + suggested launch command.

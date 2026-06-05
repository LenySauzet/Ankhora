# Ankhora Foundation & XR Shell — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the fresh URP scaffold into a Quest 3 app that builds to an APK, shows the user's tracked hands, and toggles passthrough on/off — the foundation every later Ankhora feature stands on.

**Architecture:** Install the Meta XR stack on Unity 2022.3.62f3 + URP via Package Manager, scaffold the rig with Meta **Building Blocks** (Camera Rig, Hand Tracking, Passthrough) so the Project Setup Tool auto-fixes Player/XR/Android config, then add a thin testable `PassthroughController` and a headless `BuildScript` for the build→APK→`adb install` loop. Most XR config is verified by build-and-run on device (or Meta XR Simulator on Mac); the two pieces of real C# logic get EditMode tests behind interface seams.

**Tech Stack:** Unity `2022.3.62f3` LTS · URP `14.0.12` · OpenXR Plugin + XR Plug-in Management · Meta XR All-in-One SDK · XR Hands · Quest 3 (Android, IL2CPP, ARM64, Vulkan) · Meta XR Simulator + Meta Quest Developer Hub (Mac) · Unity Test Framework (EditMode).

> **Plan 1 of 6.** This is the first slice of the MVP (see `docs/01-product/mvp-scope.md`). Subsequent plans: (2) domain model & persistence, (3) capture pipeline, (4) replay/Player, (5) Pins, (6) authoring & learner shell.

> **Version policy:** The research dossier deliberately pins **no** package versions ("Meta SDK v66 → v83+ in 18 months; always resolve the current version via Package Manager at setup time"). This plan therefore never hard-codes an XR package version — each install step resolves the latest compatible version in Package Manager. Do **not** invent a version number.

> **Editor work vs scriptable work:** Tasks 1–5 and 8 are Unity Editor / device actions a human performs at the headset station (exact menu paths + observable expected results given). Tasks 6–7 and 9 include complete C# an agent can write directly. Hand-tracking verification requires a Quest 3 or Meta XR Simulator — it cannot be confirmed in macOS Editor Play Mode.

---

## File structure

| File | Responsibility |
|---|---|
| `Assets/Ankhora/Runtime/Ankhora.Runtime.asmdef` | Runtime assembly for all Ankhora C# (keeps our code out of `Assembly-CSharp`, enables fast tests). |
| `Assets/Ankhora/Runtime/Xr/IPassthroughLayer.cs` | Seam: a 1-property interface abstracting "is passthrough on". |
| `Assets/Ankhora/Runtime/Xr/PassthroughController.cs` | Toggle logic over `IPassthroughLayer` (pure, unit-tested). |
| `Assets/Ankhora/Runtime/Xr/OvrPassthroughLayer.cs` | Production `IPassthroughLayer` wrapping Meta's `OVRManager.isInsightPassthroughEnabled`. |
| `Assets/Ankhora/Runtime/Xr/PassthroughToggleInput.cs` | `MonoBehaviour` wiring a controller button to `PassthroughController`. |
| `Assets/Ankhora/Tests/EditMode/Ankhora.Tests.EditMode.asmdef` | EditMode test assembly. |
| `Assets/Ankhora/Tests/EditMode/PassthroughControllerTests.cs` | Unit tests for toggle logic. |
| `Assets/Editor/Ankhora.Editor.asmdef` | Editor assembly (build tooling). |
| `Assets/Editor/BuildScript.cs` | Headless APK build entrypoint (`Ankhora.Editor.BuildScript.BuildAndroid`). |
| `Assets/Scenes/Ankhora.unity` | The XR scene (Camera Rig + Hand Tracking + Passthrough). |

---

## Task 0: Prerequisites (Mac station + Quest)

**Files:** none (environment).

- [ ] **Step 1: Confirm Unity + Android module**

Unity Hub → Installs → `2022.3.62f3` → ⚙ → **Add Modules** → check **Android Build Support** (with **Android SDK & NDK Tools** + **OpenJDK** sub-items). Install.
Expected: the module shows a check mark under `2022.3.62f3`.

- [ ] **Step 2: Install platform tools + Quest tooling (Mac)**

Run:
```bash
brew install --cask android-platform-tools
brew install --cask meta-quest-developer-hub   # if the cask is unavailable, download from developer.oculus.com/downloads/package/oculus-developer-hub-mac/
adb version
```
Expected: `adb` prints a version (e.g. `Android Debug Bridge version 1.0.41`).

- [ ] **Step 3: Enable Developer Mode on the Quest 3**

In the Meta Horizon mobile app → your headset → **Developer Mode → ON**. Plug the Quest into the Mac via USB-C, accept the on-headset "Allow USB debugging" prompt, then run:
```bash
adb devices
```
Expected: one device listed as `device` (not `unauthorized`).

> Cable note (dossier §1.3): the Mac M4 Pro is USB-C only; a USB-A cable (e.g. INIU) needs a USB-C adapter, or use a USB-C↔USB-C cable.

- [ ] **Step 4: Commit a doc breadcrumb**

```bash
git add docs/superpowers/plans/2026-05-31-ankhora-foundation-xr-shell.md
git commit -m "docs: add foundation & XR shell implementation plan"
```

---

## Task 1: Switch the build target to Android

**Files:** `ProjectSettings/EditorBuildSettings.asset` (Unity-managed).

- [ ] **Step 1: Switch platform**

`File → Build Settings…` → select **Android** → **Switch Platform**. Wait for the reimport.
Expected: the Unity logo badge moves to Android; Android is highlighted with the Unity icon.

- [ ] **Step 2: Verify**

In `Build Settings`, the "Platform" panel shows **Android** selected.
Expected: build target = Android, no errors in Console.

- [ ] **Step 3: Commit**

```bash
git add ProjectSettings
git commit -m "chore(xr): switch build target to Android"
```

---

## Task 2: Install the XR packages

**Files:** `Packages/manifest.json`, `Packages/packages-lock.json` (Unity-managed).

> Resolve the **current** version of each in Package Manager — do not type a version by hand. If unsure which version pairs with Unity 2022.3, check Package Manager's "See other versions" or query Context7 MCP for the package's 2022.3-compatible release.

- [ ] **Step 1: Install Unity XR packages**

`Window → Package Manager` → `+` → **Add package by name…**, adding each (leave version blank to take the latest compatible):
- `com.unity.xr.management`
- `com.unity.xr.openxr`
- `com.unity.xr.hands`
- `com.unity.xr.interaction.toolkit`

Expected: all four appear under "In Project" with no compile errors.

- [ ] **Step 2: Install the Meta XR All-in-One SDK**

Open the **Asset Store** page for **Meta XR All-in-One SDK** (free) in a browser, "Add to My Assets", then in Package Manager → **My Assets** → **Download / Import**. (Alternatively import the package from the Meta developer site.)
Expected: a `Meta` menu appears in the Unity menu bar; `Oculus`/`Meta XR` packages show under "In Project".

> Skip `com.unity.xr.arfoundation` and `com.unity.xr.meta-openxr` for the MVP — they back the V2 Room/Anchored Stages, not VR + passthrough. Adding them now is YAGNI.

- [ ] **Step 3: Resolve the Meta Project Setup Tool prompts**

If a "Meta XR — Project Setup Tool" window or Console warnings appear, click **Fix All** / **Apply All**.
Expected: the Project Setup Tool reports no outstanding required issues (warnings about optional features are fine).

- [ ] **Step 4: Commit**

```bash
git add Packages
git commit -m "chore(xr): install OpenXR, XR Hands, XRI and Meta XR All-in-One SDK"
```

---

## Task 3: Configure XR Plug-in Management + Quest Player Settings

**Files:** `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/XRPackageSettings.asset`, OpenXR settings assets (Unity-managed).

- [ ] **Step 1: Enable OpenXR for Android**

`Edit → Project Settings → XR Plug-in Management` → **Android tab** → check **OpenXR**. Then under **OpenXR → Android**, add the **Meta Quest** feature group / interaction profiles (enable "Meta Quest Support" and the "Hand Tracking Subsystem" feature). Resolve any red ⚠ via the validation **Fix** buttons.
Expected: OpenXR enabled on Android; the OpenXR validation panel shows no errors.

- [ ] **Step 2: Apply the Quest 3 Player Settings (dossier §1.4 table)**

`Edit → Project Settings → Player → Android → Other Settings`:

| Setting | Value |
|---|---|
| Scripting Backend | **IL2CPP** |
| Target Architectures | **ARM64** only (uncheck ARMv7) |
| Graphics APIs | **Vulkan** only (remove OpenGLES3) |
| Color Space (Player → Rendering) | **Linear** |
| Minimum API Level | **Android 12 (API level 32)** |
| Texture compression | **ASTC** |
| Multithreaded Rendering | **enabled** |
| Use 32-bit Display Buffer | **disabled** |
| Disable Depth and Stencil | **disabled** (required for passthrough) |
| Optimized Frame Pacing | **enabled** |

> Target/max API level: the dossier doesn't specify — leave at the Unity default (Automatic / highest installed) unless a later store-submission task requires otherwise.

Expected: no Console errors after applying; the values persist when you re-open the panel.

- [ ] **Step 3: Commit**

```bash
git add ProjectSettings
git commit -m "chore(xr): configure OpenXR + Quest 3 player settings (IL2CPP/ARM64/Vulkan/Linear)"
```

---

## Task 4: Scaffold the rig with Building Blocks (Camera Rig + Hand Tracking)

**Files:** `Assets/Scenes/Ankhora.unity` (new scene).

- [ ] **Step 1: New scene**

`File → New Scene` → Basic (URP) → save as `Assets/Scenes/Ankhora.unity`. Delete the default `Main Camera` (the Camera Rig block provides one).

- [ ] **Step 2: Add the Camera Rig + Hand Tracking blocks**

`Meta → Tools → Building Blocks`. Drag **Camera Rig** into the scene, then drag **Hand Tracking** into the scene. Accept any Project Setup Tool fixes it triggers.
Expected: a camera rig hierarchy and hand-tracking visuals are added; the Building Blocks window marks both as present; no Console errors.

- [ ] **Step 3: Set this scene as the build scene**

`File → Build Settings` → **Add Open Scenes**, and remove `SampleScene` from the build list so `Ankhora` is index 0.
Expected: only `Assets/Scenes/Ankhora.unity` is checked in the build list.

- [ ] **Step 4: Build & run — verify hands**

`File → Build Settings → Build And Run` (or build an APK and `adb install` it — see Task 9). Put the headset on.
Expected (**device verification**): you see your two **tracked hands** rendered and following your real hands. This is the first hand-tracking confirmation (not possible in macOS Editor Play Mode).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/Ankhora.unity Assets/Scenes/Ankhora.unity.meta ProjectSettings
git commit -m "feat(xr): scaffold Ankhora scene with camera rig + hand tracking (Building Blocks)"
```

---

## Task 5: Add the Passthrough Building Block

**Files:** `Assets/Scenes/Ankhora.unity` (modified).

- [ ] **Step 1: Add the Passthrough block**

`Meta → Tools → Building Blocks` → drag **Passthrough** into the scene. Accept Project Setup Tool fixes.
Expected: an `OVRPassthroughLayer` (or equivalent) is added; the scene's camera is configured for passthrough.

- [ ] **Step 2: Set passthrough support**

Select the `OVRManager` component (on the Camera Rig) → **Quest Features → General → Passthrough Support** → set to **Supported** (MR is optional in Ankhora — the Learner chooses VR vs passthrough at runtime, per `docs/06-glossary.md`).
Expected: Passthrough Support = Supported.

- [ ] **Step 3: Build & run — verify passthrough renders**

Build & run on device.
Expected (**device verification**): instead of a solid skybox, you see your **real room** (passthrough) behind any virtual content.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/Ankhora.unity Assets/Scenes/Ankhora.unity.meta ProjectSettings
git commit -m "feat(xr): enable passthrough (Supported) in the Ankhora scene"
```

---

## Task 6: PassthroughController — testable toggle logic

**Files:**
- Create: `Assets/Ankhora/Runtime/Ankhora.Runtime.asmdef`
- Create: `Assets/Ankhora/Runtime/Xr/IPassthroughLayer.cs`
- Create: `Assets/Ankhora/Runtime/Xr/PassthroughController.cs`
- Create: `Assets/Ankhora/Tests/EditMode/Ankhora.Tests.EditMode.asmdef`
- Test: `Assets/Ankhora/Tests/EditMode/PassthroughControllerTests.cs`

- [ ] **Step 1: Create the runtime assembly definition**

`Assets/Ankhora/Runtime/Ankhora.Runtime.asmdef`:
```json
{
    "name": "Ankhora.Runtime",
    "rootNamespace": "Ankhora",
    "references": [],
    "autoReferenced": true
}
```

- [ ] **Step 2: Create the EditMode test assembly definition**

`Assets/Ankhora/Tests/EditMode/Ankhora.Tests.EditMode.asmdef`:
```json
{
    "name": "Ankhora.Tests.EditMode",
    "rootNamespace": "Ankhora.Tests",
    "references": ["Ankhora.Runtime"],
    "includePlatforms": ["Editor"],
    "optionalUnityReferences": ["TestAssemblies"]
}
```

- [ ] **Step 3: Write the failing test**

`Assets/Ankhora/Tests/EditMode/PassthroughControllerTests.cs`:
```csharp
using NUnit.Framework;
using Ankhora.Xr;

namespace Ankhora.Tests
{
    public class PassthroughControllerTests
    {
        private sealed class FakePassthroughLayer : IPassthroughLayer
        {
            public bool Enabled { get; set; }
        }

        [Test]
        public void Toggle_FlipsLayerState()
        {
            var layer = new FakePassthroughLayer { Enabled = false };
            var controller = new PassthroughController(layer);

            controller.Toggle();
            Assert.IsTrue(layer.Enabled, "Toggle from off should turn passthrough on.");

            controller.Toggle();
            Assert.IsFalse(layer.Enabled, "Toggle from on should turn passthrough off.");
        }

        [Test]
        public void IsOn_ReflectsLayer()
        {
            var layer = new FakePassthroughLayer { Enabled = true };
            var controller = new PassthroughController(layer);

            Assert.IsTrue(controller.IsOn);
        }
    }
}
```

- [ ] **Step 4: Run the test — verify it fails**

`Window → General → Test Runner → EditMode → Run All`.
Expected: FAIL — `IPassthroughLayer` / `PassthroughController` do not exist (compile error).

- [ ] **Step 5: Write the seam interface**

`Assets/Ankhora/Runtime/Xr/IPassthroughLayer.cs`:
```csharp
namespace Ankhora.Xr
{
    /// <summary>Abstraction over "is passthrough currently rendering".</summary>
    public interface IPassthroughLayer
    {
        bool Enabled { get; set; }
    }
}
```

- [ ] **Step 6: Write the controller**

`Assets/Ankhora/Runtime/Xr/PassthroughController.cs`:
```csharp
namespace Ankhora.Xr
{
    /// <summary>Pure toggle logic for passthrough; UI/input layers call this.</summary>
    public sealed class PassthroughController
    {
        private readonly IPassthroughLayer _layer;

        public PassthroughController(IPassthroughLayer layer)
        {
            _layer = layer;
        }

        public bool IsOn => _layer.Enabled;

        public void Toggle() => _layer.Enabled = !_layer.Enabled;

        public void Set(bool on) => _layer.Enabled = on;
    }
}
```

- [ ] **Step 7: Run the test — verify it passes**

`Test Runner → EditMode → Run All`.
Expected: PASS (2 tests green).

- [ ] **Step 8: Commit**

```bash
git add Assets/Ankhora
git commit -m "feat(xr): add testable PassthroughController over an IPassthroughLayer seam"
```

---

## Task 7: Wire passthrough toggle to the headset

**Files:**
- Create: `Assets/Ankhora/Runtime/Xr/OvrPassthroughLayer.cs`
- Create: `Assets/Ankhora/Runtime/Xr/PassthroughToggleInput.cs`
- Modify: `Assets/Ankhora/Runtime/Ankhora.Runtime.asmdef` (reference Meta/OVR assembly)

- [ ] **Step 1: Reference the Meta runtime assembly**

In `Ankhora.Runtime.asmdef`, add the Oculus/Meta runtime assembly name to `references` so we can call `OVRManager`. Find the exact name via the OVR scripts' `.asmdef` (commonly `Oculus.VR`):
```json
{
    "name": "Ankhora.Runtime",
    "rootNamespace": "Ankhora",
    "references": ["Oculus.VR"],
    "autoReferenced": true
}
```
Expected: no compile error referencing the OVR assembly. (If the OVR scripts are not under an asmdef, set `Ankhora.Runtime`'s `autoReferenced` and instead remove the explicit reference — verify by the code in Step 2 compiling.)

- [ ] **Step 2: Production passthrough layer**

`Assets/Ankhora/Runtime/Xr/OvrPassthroughLayer.cs`:
```csharp
namespace Ankhora.Xr
{
    /// <summary>
    /// Production <see cref="IPassthroughLayer"/> backed by Meta's Insight Passthrough.
    /// Per the research dossier (§1.5): passthrough is driven at runtime by
    /// OVRManager.isInsightPassthroughEnabled.
    /// </summary>
    public sealed class OvrPassthroughLayer : IPassthroughLayer
    {
        public bool Enabled
        {
            get => OVRManager.isInsightPassthroughEnabled;
            set => OVRManager.isInsightPassthroughEnabled = value;
        }
    }
}
```

- [ ] **Step 3: Input behaviour**

`Assets/Ankhora/Runtime/Xr/PassthroughToggleInput.cs`:
```csharp
using UnityEngine;

namespace Ankhora.Xr
{
    /// <summary>
    /// Toggles passthrough when the controller's primary button (A) is pressed.
    /// Temporary input for the foundation shell; replaced by the Player UI in a later plan.
    /// </summary>
    public sealed class PassthroughToggleInput : MonoBehaviour
    {
        private PassthroughController _controller;

        private void Awake()
        {
            _controller = new PassthroughController(new OvrPassthroughLayer());
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                _controller.Toggle();
            }
        }
    }
}
```

- [ ] **Step 4: Attach to the scene**

In `Assets/Scenes/Ankhora.unity`, create an empty GameObject `AnkhoraShell` and add the **Passthrough Toggle Input** component. Save the scene.
Expected: component present, no errors.

- [ ] **Step 5: Build & run — verify the toggle**

Build & run on device. Start with passthrough off (virtual skybox); press the **A** button.
Expected (**device verification**): pressing A switches between the virtual background and your real room, each press flipping it. (Hand-driven toggle UI comes later; this proves the pipeline.)

- [ ] **Step 6: Commit**

```bash
git add Assets/Ankhora Assets/Scenes/Ankhora.unity Assets/Scenes/Ankhora.unity.meta
git commit -m "feat(xr): toggle passthrough from the controller A button on device"
```

---

## Task 8: Meta XR Simulator (Mac iteration without a headset)

**Files:** none (tooling) — optional but recommended for fast iteration on the Mac station.

- [ ] **Step 1: Enable the Simulator package**

The Meta XR Simulator (`com.meta.xr.simulator`) ships with the Meta XR All-in-One SDK. Confirm it is present in Package Manager → "In Project". Requirements per dossier: **OpenXR Plugin ≥ 1.13.0**, the **Vulkan SDK**, **Meta XR SDK v66+** (Mac ARM package noted as v83.2).

- [ ] **Step 2: Activate the Simulator**

`Meta → Simulator` (or `Oculus → Meta XR Simulator`) → **Activate**. Enter Play Mode.
Expected: a Synthetic Environment window opens and the scene renders in the simulated headset view.

> Limitation (dossier §1.5): hand tracking in **Editor Play Mode via Link is Windows-only**. The Simulator simulates input but is not a substitute for on-device testing of frame timing/thermals. Treat the Simulator as a UI/logic smoke test; confirm hand tracking and passthrough on the real Quest.

- [ ] **Step 3: No commit** (tooling only; nothing tracked changed).

---

## Task 9: Headless build script (the `adb install` loop backbone)

**Files:**
- Create: `Assets/Editor/Ankhora.Editor.asmdef`
- Create: `Assets/Editor/BuildScript.cs`

- [ ] **Step 1: Editor assembly definition**

`Assets/Editor/Ankhora.Editor.asmdef`:
```json
{
    "name": "Ankhora.Editor",
    "rootNamespace": "Ankhora.Editor",
    "references": [],
    "includePlatforms": ["Editor"],
    "autoReferenced": true
}
```

- [ ] **Step 2: Build script**

`Assets/Editor/BuildScript.cs`:
```csharp
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ankhora.Editor
{
    /// <summary>
    /// Headless APK build entrypoint. Invoke from CI or the terminal:
    ///   Unity -quit -batchmode -projectPath . \
    ///     -executeMethod Ankhora.Editor.BuildScript.BuildAndroid \
    ///     -logFile build.log
    /// Output: Build/Ankhora.apk
    /// </summary>
    public static class BuildScript
    {
        private const string OutputPath = "Build/Ankhora.apk";

        public static void BuildAndroid()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException("No enabled scenes in Build Settings.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Android build failed: {report.summary.result}");
            }

            Debug.Log($"Ankhora APK built at {OutputPath} ({report.summary.totalSize} bytes).");
        }
    }
}
```

- [ ] **Step 3: Run the headless build**

From the project root (adjust the Unity path to your install):
```bash
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -projectPath "$(pwd)" \
  -executeMethod Ankhora.Editor.BuildScript.BuildAndroid \
  -logFile build.log
```
Expected: exit code 0; `Build/Ankhora.apk` exists. On failure, read `build.log` for the `BuildFailedException` message.

- [ ] **Step 4: Install on device**

```bash
adb install -r Build/Ankhora.apk
adb logcat -s Unity:V    # stream logs while testing
```
Expected: `Success`; the Ankhora app launches from the headset's App Library (Unknown Sources).

- [ ] **Step 5: Ignore build output in git**

Confirm `Build/` is git-ignored (add to `.gitignore` if not):
```bash
grep -qxF 'Build/' .gitignore || echo 'Build/' >> .gitignore
```

- [ ] **Step 6: Commit**

```bash
git add Assets/Editor .gitignore
git commit -m "build(android): add headless BuildScript for the APK/adb loop"
```

---

## Definition of done (Foundation)

- `adb install -r Build/Ankhora.apk` installs an app that launches on the Quest 3.
- In the app: your **hands are tracked and rendered**.
- Pressing **A** **toggles passthrough** (real room ↔ virtual background).
- `Test Runner → EditMode` is green (`PassthroughControllerTests`).
- The headless `BuildScript.BuildAndroid` produces an APK (reusable by CI).
- The Meta XR Simulator runs the scene on the Mac for UI/logic smoke tests.

---

## Self-review

- **Spec coverage (Foundation slice of `mvp-scope.md`):** Quest build settings ✓ (T3) · Meta XR SDK install ✓ (T2) · Building Blocks + Project Setup Tool ✓ (T4–T5) · hand tracking visible ✓ (T4) · learner-side Passthrough toggle ✓ (T5–T7) · Mac build→APK→`adb` loop ✓ (T9) · Meta XR Simulator ✓ (T8). Persistence, capture, replay, Pins, menus are explicitly **out of this plan** (plans 2–6).
- **Placeholder scan:** no "TBD"/"add error handling" left; XR package versions are intentionally resolved-at-setup (dossier policy), not invented — this is an instruction, not a placeholder.
- **Type consistency:** `IPassthroughLayer.Enabled`, `PassthroughController.Toggle/Set/IsOn`, `OvrPassthroughLayer`, `BuildScript.BuildAndroid`, output `Build/Ankhora.apk` are used identically across tasks 6, 7, and 9.
- **Known soft spots flagged in-plan (verify, don't invent):** exact OVR assembly name in `references` (T7.S1), the Meta passthrough-support menu path label (T5.S2), and the Simulator activation menu label (T8.S2) — each carries a fallback or an observable check.

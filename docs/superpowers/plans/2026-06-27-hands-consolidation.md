# Hands Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the buttonless auto-capture scaffolding with a pinch-triggered take, render the expert's live hands while recording, and replay through a skinned translucent Meta ghost-hand mesh — without changing the record/replay contract.

**Architecture:** ADR-0004 two-assembly split. New pure logic (`RecordingCountdown`, `PinchEdgeDetector`) lands in `Ankhora.Domain` and is fully EditMode-tested on the Mac. The device layer (`PinchRecordingTrigger`, `SkinnedGhostHandView`) lands in `Ankhora.Foundation` as thin shells over that pure logic and is verified on a Quest 3 (hand tracking does not render in the macOS Editor). The capture/replay seams — `RecordingSession`, `OvrHandPoseSource`, `GhostHandPlayer`, `IHandPoseSource`/`IHandSkeletonSource`/`IHandView`, `TimelineSampler` — are reused unchanged.

**Tech Stack:** Unity 6 `6000.4.10f1`, URP 17.4.0, Meta XR SDK all-in-one 201.0.0 (OVRHand/OVRSkeleton/OVRMesh + hand-tracking Building Block), OpenXR 26-joint hand skeleton, NUnit EditMode tests, C# (Unity conventions).

## Global Constraints

- **Assembly split (ADR-0004):** pure logic → `Ankhora.Domain` (namespace `Ankhora.Domain.*`); device/OVR code → `Ankhora.Foundation` (namespace `Ankhora.Foundation.*`); cross-feature wiring → `Foundation/App`. Domain must not reference `Ankhora.Foundation` or `Oculus.VR`.
- **TDD for pure logic:** every new Domain class gets a failing EditMode test first (red → green → commit). Existing EditMode tests must stay green.
- **Hand tracking cannot run in the macOS Editor** — MonoBehaviour/scene/asset behaviour is verified on a Quest 3 (or by Windows teammates over Quest Link). Mac verifies compilation (clean console) + EditMode tests + scene captures only.
- **C# style:** `PascalCase` types, `camelCase` fields, `[SerializeField] private` over public fields. Code/comments in English.
- **Conventional Commits** via the `git-commit` skill. Branch: `feat/hands-consolidation` (already checked out). No direct push to `main`; no `--no-verify`.
- **OpenXR hand skeleton = 26 joints**, not the legacy 19. All bone buffers ≥ 26 and treated as count-agnostic. Never read a fixed bone count.
- **`.meta` files:** create every new asset/script so Unity generates its `.meta`, and commit the `.cs`/asset **and** its `.meta` together. Never commit a script without its `.meta`.
- **Trigger semantics (agreed):** non-dominant index **pinch toggle** — first pinch arms → fixed 3-2-1 countdown (kept out of the recorded window) → recording → second pinch stops & saves. Stop is event-driven, not a timer.
- **Ghost shader (per `urp-shadergraph`):** URP, **Unlit**, transparent (alpha ≈ 0.3), soft Fresnel rim, subtle emissive tint; single-pass-instanced safe; no scene-color / refraction / GrabPass.
- **One rig:** the Meta hand-tracking Building Block (`OVRHandPrefab` L/R) is the single source for both live-hand rendering and capture — no hand-rolled `OVRHand`/`OVRSkeleton`.

---

## File Structure

**Created — Domain (pure, EditMode-tested):**
- `Assets/Scripts/Domain/Recording/RecordingCountdown.cs` — pure countdown gate (phase + integer seconds remaining).
- `Assets/Scripts/Domain/Recording/PinchEdgeDetector.cs` — pure debounced rising-edge detector over a boolean pinch signal.
- `Assets/Tests/EditMode/RecordingCountdownTests.cs`
- `Assets/Tests/EditMode/PinchEdgeDetectorTests.cs`

**Created — Foundation (device layer):**
- `Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs` — `MonoBehaviour`; pinch → countdown → record → save state machine over `RecordingSession`.
- `Assets/Scripts/Foundation/Replay/SkinnedGhostHandView.cs` — `IHandView`; builds the captured-skeleton bone rig and drives a `SkinnedMeshRenderer` wired to the Meta hand mesh.

**Created — Assets:**
- `Assets/Art/Shaders/GhostHands_URP.shadergraph`
- `Assets/Art/Materials/M_GhostHands.mat`

**Renamed:**
- `Assets/Scripts/Foundation/App/FirstLightReplayLink.cs` → `Assets/Scripts/Foundation/App/RecordReplayLink.cs` (class `FirstLightReplayLink` → `RecordReplayLink`).

**Modified:**
- `Assets/Scenes/MainVrScene.unity` — swap hand-rolled `OVRHand`/`OVRSkeleton` for the Meta hand-tracking Building Block; repoint `OvrHandPoseSource` + the new trigger + the ghost views; wire `RecordReplayLink`.

**Deleted (sequenced):**
- Task 8: `Assets/Scripts/Foundation/Recording/FirstLightAutoCapture.cs`, `Assets/Scripts/Domain/Recording/AutoCaptureClock.cs`, `Assets/Tests/EditMode/AutoCaptureClockTests.cs` (replaced by the new trigger + countdown).
- Task 9 (**only after** device-verifying the skinned mesh): `Assets/Scripts/Foundation/Replay/FkGhostHandView.cs`.

**Reused unchanged:** `RecordingSession`, `OvrHandPoseSource`, `IHandPoseSource`, `IHandSkeletonSource`, `GhostHandPlayer`, `IHandView`, `TimelineSampler`, `TimelineRecorder`, `Domain/Model`, persistence.

---

## Task 1: RecordingCountdown (pure Domain)

A pure countdown gate that replaces `AutoCaptureClock`'s fixed-schedule role. It knows only about the lead-in: given the seconds elapsed since the take was armed, it reports the phase (`Counting` while inside the countdown, `Live` after) and the integer seconds still to show (3 → 2 → 1 → 0). It owns no record-duration — the stop is event-driven (a second pinch), so the old `Recording`/`Done`/`RecordElapsed` concept is gone.

**Files:**
- Create: `Assets/Scripts/Domain/Recording/RecordingCountdown.cs`
- Test: `Assets/Tests/EditMode/RecordingCountdownTests.cs`

**Interfaces:**
- Consumes: nothing (pure).
- Produces:
  - `enum Ankhora.Domain.Recording.CountdownPhase { Counting, Live }`
  - `class Ankhora.Domain.Recording.RecordingCountdown`
    - ctor `RecordingCountdown(float countdownSeconds)` — clamps negative to 0.
    - `CountdownPhase PhaseAt(float elapsed)` — `Counting` while `elapsed < countdownSeconds`, else `Live`.
    - `int SecondsRemaining(float elapsed)` — ceil of remaining countdown, clamped to `[0, ceil(countdownSeconds)]`; `0` once `Live`.

- [ ] **Step 1: Write the failing test**

```csharp
using Ankhora.Domain.Recording;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The pinch-armed take runs a fixed 3-2-1 lead-in before recording, kept out of the recorded
    /// window. This pins the pure phase + remaining-seconds logic so the device trigger stays a thin
    /// shell (hand tracking can't be exercised in the macOS Editor).
    /// </summary>
    public class RecordingCountdownTests
    {
        private static RecordingCountdown Countdown() => new RecordingCountdown(countdownSeconds: 3f);

        [Test]
        public void PhaseAt_WhileInsideCountdown_IsCounting()
        {
            Assert.AreEqual(CountdownPhase.Counting, Countdown().PhaseAt(0f));
            Assert.AreEqual(CountdownPhase.Counting, Countdown().PhaseAt(2.99f));
        }

        [Test]
        public void PhaseAt_AtAndAfterCountdownEnd_IsLive()
        {
            Assert.AreEqual(CountdownPhase.Live, Countdown().PhaseAt(3f));
            Assert.AreEqual(CountdownPhase.Live, Countdown().PhaseAt(100f));
        }

        [Test]
        public void SecondsRemaining_CountsDownThreeTwoOne()
        {
            RecordingCountdown c = Countdown();
            Assert.AreEqual(3, c.SecondsRemaining(0f));     // just armed: "3"
            Assert.AreEqual(3, c.SecondsRemaining(0.5f));   // still showing "3"
            Assert.AreEqual(2, c.SecondsRemaining(1f));     // "2"
            Assert.AreEqual(1, c.SecondsRemaining(2f));     // "1"
            Assert.AreEqual(0, c.SecondsRemaining(3f));     // recording starts
        }

        [Test]
        public void SecondsRemaining_IsZeroOnceLive_AndNeverNegative()
        {
            Assert.AreEqual(0, Countdown().SecondsRemaining(50f));
        }

        [Test]
        public void NegativeCountdown_IsClampedToImmediatelyLive()
        {
            var c = new RecordingCountdown(countdownSeconds: -2f);
            Assert.AreEqual(CountdownPhase.Live, c.PhaseAt(0f));
            Assert.AreEqual(0, c.SecondsRemaining(0f));
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run the EditMode suite via the Unity Test Runner (Window > General > Test Runner > EditMode > Run All), or batchmode:
`"/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity" -runTests -batchmode -projectPath . -testPlatform EditMode -testFilter Ankhora.Tests.EditMode.RecordingCountdownTests -logFile -`
Expected: FAIL — `RecordingCountdown` / `CountdownPhase` do not exist (compile error).

- [ ] **Step 3: Write the minimal implementation**

```csharp
using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>The phase of a pinch-armed take's lead-in.</summary>
    public enum CountdownPhase
    {
        Counting,
        Live,
    }

    /// <summary>
    /// Pure lead-in gate for a pinch-armed take: a fixed countdown, then live (recording). All queries
    /// are functions of seconds elapsed since the take was armed, so the driving MonoBehaviour holds no
    /// timing state of its own. The stop is event-driven (a second pinch), so unlike the retired
    /// <c>AutoCaptureClock</c> this owns no record duration.
    /// </summary>
    public class RecordingCountdown
    {
        private readonly float _countdown;

        public RecordingCountdown(float countdownSeconds)
        {
            _countdown = Mathf.Max(0f, countdownSeconds);
        }

        public CountdownPhase PhaseAt(float elapsed) =>
            elapsed < _countdown ? CountdownPhase.Counting : CountdownPhase.Live;

        /// <summary>Integer seconds still to show (3 → 2 → 1 → 0); 0 once live.</summary>
        public int SecondsRemaining(float elapsed)
        {
            float remaining = _countdown - elapsed;
            if (remaining <= 0f)
                return 0;
            return Mathf.Clamp(Mathf.CeilToInt(remaining), 0, Mathf.CeilToInt(_countdown));
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run the same filter as Step 2. Expected: PASS (5/5). Confirm the rest of the EditMode suite is still green.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/Recording/RecordingCountdown.cs Assets/Scripts/Domain/Recording/RecordingCountdown.cs.meta \
        Assets/Tests/EditMode/RecordingCountdownTests.cs Assets/Tests/EditMode/RecordingCountdownTests.cs.meta
git commit -m "feat(recording): pure RecordingCountdown lead-in gate"
```

---

## Task 2: PinchEdgeDetector (pure Domain)

A pure debounced rising-edge detector over a boolean pinch signal. Each tick feeds the current "is pinching" bool; the detector returns `true` only on the frame the signal transitions `false → true`, and only once the signal has been continuously held for a small debounce time (to reject tracking jitter). This is the toggle's heartbeat, kept pure so the toggle is testable without a headset.

**Files:**
- Create: `Assets/Scripts/Domain/Recording/PinchEdgeDetector.cs`
- Test: `Assets/Tests/EditMode/PinchEdgeDetectorTests.cs`

**Interfaces:**
- Consumes: nothing (pure).
- Produces:
  - `class Ankhora.Domain.Recording.PinchEdgeDetector`
    - ctor `PinchEdgeDetector(float debounceSeconds = 0.05f)` — clamps negative to 0.
    - `bool Tick(bool isPinching, float deltaSeconds)` — returns `true` exactly once per fresh pinch, after the signal has been held continuously for `debounceSeconds`. Releasing (`isPinching == false`) re-arms it for the next pinch.

- [ ] **Step 1: Write the failing test**

```csharp
using Ankhora.Domain.Recording;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The pinch toggle must fire once per deliberate pinch, never on jitter and never twice for one
    /// hold. This pins the pure rising-edge + debounce logic so the device trigger stays a thin shell.
    /// </summary>
    public class PinchEdgeDetectorTests
    {
        private const float Dt = 1f / 30f;   // ~33 ms per frame

        [Test]
        public void Tick_FiresOnceAfterDebounce_WhenHeld()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            Assert.IsFalse(d.Tick(true, Dt));   // ~33 ms held — below 50 ms debounce
            Assert.IsTrue(d.Tick(true, Dt));    // ~66 ms held — fires
            Assert.IsFalse(d.Tick(true, Dt));   // still held — no second fire
            Assert.IsFalse(d.Tick(true, Dt));
        }

        [Test]
        public void Tick_NeverFiresWhileReleased()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            Assert.IsFalse(d.Tick(false, Dt));
            Assert.IsFalse(d.Tick(false, Dt));
        }

        [Test]
        public void Tick_RejectsJitter_ShorterThanDebounce()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            Assert.IsFalse(d.Tick(true, Dt));   // ~33 ms then released — too short
            Assert.IsFalse(d.Tick(false, Dt));
            Assert.IsFalse(d.Tick(false, Dt));
        }

        [Test]
        public void Tick_RearmsAfterRelease_ForNextPinch()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0.05f);
            d.Tick(true, Dt);
            Assert.IsTrue(d.Tick(true, Dt));    // first pinch fires
            Assert.IsFalse(d.Tick(false, Dt));  // released
            Assert.IsFalse(d.Tick(true, Dt));   // second pinch: debounce again
            Assert.IsTrue(d.Tick(true, Dt));    // second pinch fires
        }

        [Test]
        public void ZeroDebounce_FiresOnFirstHeldFrame()
        {
            var d = new PinchEdgeDetector(debounceSeconds: 0f);
            Assert.IsTrue(d.Tick(true, Dt));
            Assert.IsFalse(d.Tick(true, Dt));
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Filter `Ankhora.Tests.EditMode.PinchEdgeDetectorTests`. Expected: FAIL — `PinchEdgeDetector` does not exist.

- [ ] **Step 3: Write the minimal implementation**

```csharp
using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>
    /// Pure debounced rising-edge detector over a boolean pinch signal. Fires once per deliberate
    /// pinch — only after the signal has been held continuously for the debounce window (rejecting
    /// hand-tracking jitter), and only once per hold. Releasing re-arms it for the next pinch. Kept
    /// pure (no OVR types, no Unity time) so the pinch toggle is fully EditMode-testable.
    /// </summary>
    public class PinchEdgeDetector
    {
        private readonly float _debounce;
        private float _heldFor;
        private bool _wasPinching;
        private bool _firedThisHold;

        public PinchEdgeDetector(float debounceSeconds = 0.05f)
        {
            _debounce = Mathf.Max(0f, debounceSeconds);
        }

        public bool Tick(bool isPinching, float deltaSeconds)
        {
            if (!isPinching)
            {
                _heldFor = 0f;
                _wasPinching = false;
                _firedThisHold = false;
                return false;
            }

            _heldFor = _wasPinching ? _heldFor + deltaSeconds : deltaSeconds;
            _wasPinching = true;

            if (!_firedThisHold && _heldFor >= _debounce)
            {
                _firedThisHold = true;
                return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Filter as Step 2. Expected: PASS (5/5). Full EditMode suite still green.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/Recording/PinchEdgeDetector.cs Assets/Scripts/Domain/Recording/PinchEdgeDetector.cs.meta \
        Assets/Tests/EditMode/PinchEdgeDetectorTests.cs Assets/Tests/EditMode/PinchEdgeDetectorTests.cs.meta
git commit -m "feat(recording): pure PinchEdgeDetector for the pinch toggle"
```

---

## Task 3: PinchRecordingTrigger (Foundation MonoBehaviour)

The device trigger that replaces `FirstLightAutoCapture`. It reads the non-dominant hand's index pinch from an `OVRHand`, runs it through `PinchEdgeDetector`, and drives the state machine `Idle → CountingDown → Recording → (save) → Idle`. The first detected pinch arms a take and starts a `RecordingCountdown`; when the countdown goes `Live` it calls `RecordingSession.Begin` and `Tick`s every frame; a second detected pinch stops and saves via `RecordingSession.SaveTo`, then raises `OnRecordingSaved`. It exposes a `UnityEvent<int>` for the countdown value (a hook for later visual feedback — logged for now).

This is a thin shell over the Task 1/2 pure logic and the unchanged `RecordingSession`; its behaviour is verified on device (no hand tracking in the Mac Editor). Like the `FirstLightAutoCapture` it replaces, it carries no EditMode test of its own — the testable logic lives in `RecordingCountdown` + `PinchEdgeDetector`. Verification here is: compiles with a clean console (`Unity_ValidateScript` + `Unity_ReadConsole`), plus the device checklist in Task 7.

**Files:**
- Create: `Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs`

**Interfaces:**
- Consumes:
  - `RecordingCountdown(float)`, `.PhaseAt(float)`, `.SecondsRemaining(float)`, `CountdownPhase.{Counting,Live}` (Task 1)
  - `PinchEdgeDetector(float)`, `.Tick(bool, float)` (Task 2)
  - `RecordingSession(IHandPoseSource, float)`, `.Begin(float)`, `.Tick(float)`, `.SaveTo(MasterclassStore, float, out int, out string)`, `.LeftBoneCount`, `.RightBoneCount` (unchanged)
  - `MasterclassStore(string)`, `.Path` (unchanged)
  - `IHandPoseSource` (the pose source MonoBehaviour, unchanged)
  - Meta: `OVRHand.GetFingerIsPinching(OVRHand.HandFinger)`, `OVRHand.IsTracked`
- Produces:
  - `class Ankhora.Foundation.Recording.PinchRecordingTrigger : MonoBehaviour`
    - `UnityEvent OnRecordingSaved` (property) — wired in the scene to `GhostHandPlayer.LoadAndPlay` via `RecordReplayLink` (Task 6). Same name/type as the field on `FirstLightAutoCapture` it replaces, so the scene wiring carries over.

- [ ] **Step 1: Write the implementation**

```csharp
using Ankhora.Domain.Recording;
using Ankhora.Foundation.Persistence;
using UnityEngine;
using UnityEngine.Events;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Pinch-triggered recording: a non-dominant index pinch arms a take, a fixed 3-2-1 countdown keeps
    /// the arming gesture out of the recorded window, recording then runs until a second pinch stops and
    /// saves it. Replaces the buttonless <c>FirstLightAutoCapture</c> bring-up harness. The pinch toggle
    /// and the countdown are pure (<see cref="PinchEdgeDetector"/> / <see cref="RecordingCountdown"/>,
    /// EditMode-tested); this shell only owns the OVR reads and the state machine, verified on device.
    /// <para>
    /// Interim trigger by design — the real record control will come from the product UI later. We pinch
    /// the NON-dominant hand so the dominant hand (the one demonstrating) is never occluded by the
    /// gesture, and so a held controller never disables that hand's tracking.
    /// </para>
    /// </summary>
    public class PinchRecordingTrigger : MonoBehaviour
    {
        [Tooltip("The non-dominant hand whose index pinch arms/stops the take.")]
        [SerializeField] private OVRHand _triggerHand;
        [SerializeField] private MonoBehaviour _poseSourceBehaviour;   // implements IHandPoseSource
        [SerializeField, Min(0f)] private float _countdownSeconds = 3f;
        [SerializeField, Min(1f)] private float _sampleRateHz = 30f;
        [SerializeField, Min(0f)] private float _pinchDebounceSeconds = 0.05f;
        [SerializeField] private string _fileName = "masterclass.json";

        [Tooltip("Raised after the take is saved — wire it to the ghost player's LoadAndPlay in the scene.")]
        [SerializeField] private UnityEvent _onRecordingSaved = new UnityEvent();
        [Tooltip("Raised each second of the countdown with the value to show (3, 2, 1). Hook for future UI.")]
        [SerializeField] private UnityEvent<int> _onCountdownTick = new UnityEvent<int>();

        public UnityEvent OnRecordingSaved => _onRecordingSaved;
        public UnityEvent<int> OnCountdownTick => _onCountdownTick;

        private enum State { Idle, CountingDown, Recording }

        private RecordingSession _session;
        private MasterclassStore _store;
        private RecordingCountdown _countdown;
        private PinchEdgeDetector _pinch;
        private State _state = State.Idle;
        private float _armTime;
        private int _lastSecondShown = -1;

        private void Awake()
        {
            var source = _poseSourceBehaviour as IHandPoseSource;
            if (source == null)
                Debug.LogError("[PinchRecordingTrigger] _poseSourceBehaviour must implement IHandPoseSource.", this);
            else
                _session = new RecordingSession(source, _sampleRateHz);

            if (_triggerHand == null)
                Debug.LogError("[PinchRecordingTrigger] Assign the non-dominant trigger OVRHand.", this);

            _store = new MasterclassStore(_fileName);
            _countdown = new RecordingCountdown(_countdownSeconds);
            _pinch = new PinchEdgeDetector(_pinchDebounceSeconds);
        }

        private void Update()
        {
            if (_session == null || _triggerHand == null)
                return;

            float now = Time.unscaledTime;
            bool isPinching = _triggerHand.IsTracked &&
                              _triggerHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            bool freshPinch = _pinch.Tick(isPinching, Time.unscaledDeltaTime);

            switch (_state)
            {
                case State.Idle:
                    if (freshPinch)
                        Arm(now);
                    break;

                case State.CountingDown:
                    TickCountdown(now);
                    break;

                case State.Recording:
                    _session.Tick(now);
                    if (freshPinch)
                        StopAndPublish(now);
                    break;
            }
        }

        private void Arm(float now)
        {
            _state = State.CountingDown;
            _armTime = now;
            _lastSecondShown = -1;
            Debug.Log($"[PinchRecordingTrigger] Armed — {_countdownSeconds:0}s countdown.");
            TickCountdown(now);
        }

        private void TickCountdown(float now)
        {
            float elapsed = now - _armTime;
            if (_countdown.PhaseAt(elapsed) == CountdownPhase.Live)
            {
                _state = State.Recording;
                _session.Begin(now);
                Debug.Log("[PinchRecordingTrigger] Recording — pinch again to stop.");
                return;
            }

            int second = _countdown.SecondsRemaining(elapsed);
            if (second != _lastSecondShown)
            {
                _lastSecondShown = second;
                _onCountdownTick.Invoke(second);
                Debug.Log($"[PinchRecordingTrigger] {second}...");
            }
        }

        private void StopAndPublish(float now)
        {
            _state = State.Idle;
            bool ok = _session.SaveTo(_store, now, out int frames, out string error);
            if (!ok)
            {
                Debug.LogError($"[PinchRecordingTrigger] Save failed: {error}", this);
                return;
            }

            Debug.Log($"[PinchRecordingTrigger] Saved {frames} frames " +
                      $"(L:{_session.LeftBoneCount} R:{_session.RightBoneCount} bones) to {_store.Path}. Replaying.");
            _onRecordingSaved.Invoke();
        }
    }
}
```

- [ ] **Step 2: Verify it compiles cleanly**

Trigger a Unity asset refresh/compile, then `Unity_ReadConsole` (errors-only). Expected: no compile errors. Optionally `Unity_ValidateScript` on the new file. Confirm `OVRHand.GetFingerIsPinching` and `OVRHand.HandFinger.Index` resolve against SDK 201.0.0 — if the signature differs, confirm the exact API via context7/Meta docs before adjusting (anti-hallucination rule).

- [ ] **Step 3: Confirm the EditMode suite still compiles + passes**

Run all EditMode tests. Expected: still green (this task adds no test but must not break compilation of the test assembly, which references `Ankhora.Foundation`).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs Assets/Scripts/Foundation/Recording/PinchRecordingTrigger.cs.meta
git commit -m "feat(recording): pinch-triggered take state machine (PinchRecordingTrigger)"
```

---

## Task 4: GhostHands_URP shader + M_GhostHands material

A URP **Unlit transparent** Shader Graph for the ghost hands: low alpha (~0.3), a soft Fresnel rim for a "presence" read, a subtle emissive tint, single-pass-instanced safe, no scene-color/refraction (per `urp-shadergraph` mobile-VR rules). Plus an `M_GhostHands` material instance with exposed properties wired so alpha / rim power / tint are tweakable without editing the graph.

**Files:**
- Create: `Assets/Art/Shaders/GhostHands_URP.shadergraph`
- Create: `Assets/Art/Materials/M_GhostHands.mat`

**Interfaces:**
- Consumes: nothing.
- Produces: a material asset `Assets/Art/Materials/M_GhostHands.mat` referencing the `GhostHands_URP` graph — assigned to `SkinnedGhostHandView` in Task 6's scene wiring.

- [ ] **Step 1: Create the Shader Graph**

Use `Unity_ManageShader` (or `Unity_ManageAsset` for a Shader Graph asset) to create `Assets/Art/Shaders/GhostHands_URP.shadergraph`:
- Target: **URP**, Material type **Unlit**, Surface **Transparent**, Blend **Alpha**, Render Face **Both** (hands are thin — show backfaces), **Depth Write Off**.
- Exposed properties:
  - `_BaseColor` (Color, default soft cyan-white e.g. `(0.6, 0.85, 1.0, 1.0)`)
  - `_Alpha` (Float, range 0–1, default `0.3`)
  - `_RimColor` (Color, default `(0.7, 0.9, 1.0, 1.0)`)
  - `_RimPower` (Float, range 0.5–8, default `3.0`)
  - `_EmissionStrength` (Float, range 0–2, default `0.4`)
- Graph: Fresnel Effect (Power = `_RimPower`) → multiply by `_RimColor` → add to `_BaseColor` → into **Base Color** and (scaled by `_EmissionStrength`) into **Emission**. Alpha output = `_Alpha` (optionally boosted by Fresnel so the rim reads slightly more opaque: `saturate(_Alpha + fresnel * 0.3)`). No texture samples, no scene-color/refraction/GrabPass nodes.
- Ensure stereo safety: do not add nodes that break single-pass instancing (no custom screen-position scene sampling).

- [ ] **Step 2: Verify the shader compiles**

`Unity_ReadConsole` (errors-only): no shader compile errors. (Shader Graph has no `Unity_ValidateScript` equivalent — rely on the console.)

- [ ] **Step 3: Create + configure the material**

Use `Unity_ManageAsset` to create `Assets/Art/Materials/M_GhostHands.mat` from the `GhostHands_URP` shader. Leave exposed properties at their defaults (alpha 0.3, rim power 3, emission 0.4).

- [ ] **Step 4: Visual sanity check**

`Unity_SceneView_Capture2DScene` against a primitive (e.g. a temporary sphere) assigned `M_GhostHands`: confirm it renders translucent with a visible rim and is not opaque / not z-fighting. (Final read is verified on device against passthrough in Task 7.) Delete any temporary primitive before committing.

- [ ] **Step 5: Commit**

```bash
git add Assets/Art/Shaders/GhostHands_URP.shadergraph Assets/Art/Shaders/GhostHands_URP.shadergraph.meta \
        Assets/Art/Materials/M_GhostHands.mat Assets/Art/Materials/M_GhostHands.mat.meta
git commit -m "feat(replay): URP unlit translucent ghost-hands shader + material"
```

---

## Task 5: SkinnedGhostHandView (Foundation, IHandView approach A)

The skinned ghost view. It implements the same `IHandView` seam as `FkGhostHandView`, so `GhostHandPlayer` drives it identically. `Bind` builds a parented bone-transform hierarchy from the captured `HandSkeleton` (the **same** rig-build logic proven in `FkGhostHandView`), then attaches a `SkinnedMeshRenderer` whose `sharedMesh` is the Meta hand mesh (`OVRHand_L`/`OVRHand_R`), whose `bones` are the built transforms in skeleton order, and whose `rootBone` is the wrist. `Apply` sets the wrist pose and each bone's local rotation (Unity skinning then deforms the mesh). `Show` toggles the renderer.

The mesh-to-rig binding is the delicate part (bind poses, bone order, weights) and is verified on device; capture and ghost derive from the **same** OVR skeleton, so order and count match by construction. The `FkGhostHandView` fallback stays behind the seam until Task 7 confirms the mesh on device.

**Files:**
- Create: `Assets/Scripts/Foundation/Replay/SkinnedGhostHandView.cs`

**Interfaces:**
- Consumes:
  - `IHandView` (unchanged): `void Bind(HandSkeleton)`, `void Show(bool)`, `void Apply(in Pose, Quaternion[], int)`
  - `HandSkeleton` (unchanged): `int[] boneParents`, `Pose[] boneBindPoses`, `bool IsValid`
- Produces:
  - `class Ankhora.Foundation.Replay.SkinnedGhostHandView : MonoBehaviour, IHandView`
    - `[SerializeField] private Mesh _handMesh;` — the Meta `OVRHand_L`/`OVRHand_R` mesh, assigned per-hand in the scene.
    - `[SerializeField] private Material _ghostMaterial;` — `M_GhostHands` (Task 4).

Reference (the proven rig-build from `FkGhostHandView.BuildRig`, reused verbatim in structure):
```csharp
// _bones[0] = transform; for i in 1..n: new GameObject parented per s.boneParents[i],
// localPosition = s.boneBindPoses[i].position, localRotation = s.boneBindPoses[i].rotation.
```

- [ ] **Step 1: Write the implementation**

```csharp
using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Skinned translucent ghost hand: the captured <see cref="HandSkeleton"/> is rebuilt as a parented
    /// bone-transform hierarchy (same rig-build as <see cref="FkGhostHandView"/>), then a
    /// <see cref="SkinnedMeshRenderer"/> is wired to the Meta hand mesh and skinned to those bones, so
    /// the recorded finger articulation deforms a real hand mesh instead of floating joint spheres.
    /// Drives through the same <see cref="IHandView"/> seam, so <c>GhostHandPlayer</c> is unchanged.
    /// <para>
    /// Approach A (our rig drives the Meta mesh): capture and replay derive from the SAME OVR skeleton,
    /// so bone order and count match by construction (OpenXR 26-joint, count-agnostic). The mesh's own
    /// bind poses are baked into <c>sharedMesh.bindposes</c>; we only supply the bone transforms and a
    /// root. Device-verified — hand tracking does not render in the macOS Editor.
    /// </para>
    /// </summary>
    public class SkinnedGhostHandView : MonoBehaviour, IHandView
    {
        [Tooltip("Meta hand mesh for this hand: OVRHand_L for left, OVRHand_R for right.")]
        [SerializeField] private Mesh _handMesh;
        [SerializeField] private Material _ghostMaterial;

        private Transform[] _bones;     // index-aligned with captured boneRotations; _bones[0] == this.transform
        private SkinnedMeshRenderer _renderer;
        private bool _built;

        public void Bind(HandSkeleton skeleton)
        {
            if (_built || skeleton == null || !skeleton.IsValid)
                return;
            if (_handMesh == null)
            {
                Debug.LogError("[SkinnedGhostHandView] No hand mesh assigned.", this);
                return;
            }
            BuildRig(skeleton);
            BuildRenderer();
            _built = true;
            Show(false);
        }

        private void BuildRig(HandSkeleton s)
        {
            int n = s.boneParents.Length;
            _bones = new Transform[n];
            _bones[0] = transform; // wrist container; positioned by Apply from the tracking-space root

            for (int i = 1; i < n; i++)
                _bones[i] = new GameObject($"Bone_{i}").transform;

            for (int i = 1; i < n; i++)
            {
                int p = s.boneParents[i];
                Transform parent = (p >= 0 && p < n) ? _bones[p] : transform;
                _bones[i].SetParent(parent, false);
                _bones[i].localPosition = s.boneBindPoses[i].position;
                _bones[i].localRotation = s.boneBindPoses[i].rotation;
            }
        }

        private void BuildRenderer()
        {
            var go = new GameObject("GhostMesh");
            go.transform.SetParent(transform, false);
            _renderer = go.AddComponent<SkinnedMeshRenderer>();
            _renderer.sharedMesh = _handMesh;       // bind poses are baked into the mesh
            _renderer.bones = _bones;               // built transforms in captured-skeleton order
            _renderer.rootBone = _bones[0];
            _renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 0.4f); // hand-sized; avoids culling pops
            _renderer.updateWhenOffscreen = true;
            if (_ghostMaterial != null)
                _renderer.sharedMaterial = _ghostMaterial;
        }

        public void Show(bool visible)
        {
            if (_renderer != null)
                _renderer.enabled = visible;
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_bones == null || boneRotations == null)
                return;

            // Wrist (bone 0): placed from the tracking-space root (carries the hand's gross motion).
            transform.localPosition = root.position;
            transform.localRotation = root.rotation;

            int n = Mathf.Min(_bones.Length, boneCount);
            for (int i = 1; i < n; i++)
                if (_bones[i] != null)
                    _bones[i].localRotation = boneRotations[i];
        }
    }
}
```

- [ ] **Step 2: Verify it compiles cleanly**

Refresh/compile, then `Unity_ReadConsole` (errors-only): no compile errors. Run the full EditMode suite — still green.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Foundation/Replay/SkinnedGhostHandView.cs Assets/Scripts/Foundation/Replay/SkinnedGhostHandView.cs.meta
git commit -m "feat(replay): skinned Meta ghost-hand mesh view (IHandView)"
```

> **Note on bind poses:** `SkinnedMeshRenderer` uses `sharedMesh.bindposes` (baked into `OVRHand_L/R.fbx`) together with the live `bones[]` world transforms. Because our rig is built from the captured rest pose (`boneBindPoses`) in the same bone order the mesh was authored against, the skinning resolves correctly. If on-device verification (Task 7) shows the mesh inverted/exploded, the mismatch is bone order or a left/right mesh swap — that is the device-debug step the spec's Risk 1 anticipates, with the FK fallback as the escape hatch.

---

## Task 6: Rename FirstLightReplayLink → RecordReplayLink + scene wiring

Rename the composition root off the "FirstLight" scaffolding name and repoint it at `PinchRecordingTrigger`. Then rebuild the scene's hand setup: replace the hand-rolled `OVRHand`/`OVRSkeleton` objects with the Meta hand-tracking Building Block, repoint the pose source + trigger + ghost views, and assign the ghost mesh/material.

**Files:**
- Rename: `Assets/Scripts/Foundation/App/FirstLightReplayLink.cs` → `Assets/Scripts/Foundation/App/RecordReplayLink.cs`
- Modify: `Assets/Scenes/MainVrScene.unity`

**Interfaces:**
- Consumes: `PinchRecordingTrigger.OnRecordingSaved` (Task 3), `GhostHandPlayer.LoadAndPlay` (unchanged), `SkinnedGhostHandView` (Task 5), `M_GhostHands` (Task 4).
- Produces: `class Ankhora.Foundation.App.RecordReplayLink : MonoBehaviour` (wires save → playback).

- [ ] **Step 1: Rename the script + class, keeping its `.meta` GUID**

Rename the file and class via `Unity_ManageScript` (or rename the file on disk **and** its `.meta` so the GUID is preserved — preserving the GUID keeps the scene's component reference intact). New content:

```csharp
using Ankhora.Foundation.Recording;
using Ankhora.Foundation.Replay;
using UnityEngine;

namespace Ankhora.Foundation.App
{
    /// <summary>
    /// Scene composition root: links the recorder's "saved" event to the ghost player's playback so the
    /// Recording code stays ignorant of the Replay code (ADR-0004 — features don't reference each other;
    /// the wiring lives here).
    /// </summary>
    public class RecordReplayLink : MonoBehaviour
    {
        [SerializeField] private PinchRecordingTrigger _recorder;
        [SerializeField] private GhostHandPlayer _player;

        private void Awake()
        {
            if (_recorder == null || _player == null)
            {
                Debug.LogError("[RecordReplayLink] Assign both the recorder and the player.", this);
                return;
            }
            _recorder.OnRecordingSaved.AddListener(_player.LoadAndPlay);
        }

        private void OnDestroy()
        {
            if (_recorder != null && _player != null)
                _recorder.OnRecordingSaved.RemoveListener(_player.LoadAndPlay);
        }
    }
}
```

- [ ] **Step 2: Verify rename compiles**

`Unity_ReadConsole` (errors-only): no errors, no dangling references to `FirstLightReplayLink`. (`FirstLightAutoCapture` still exists at this point — it is deleted in Task 8; the scene reference to it is replaced in Step 4 below.)

- [ ] **Step 3: Add the Meta hand-tracking Building Block to the scene**

In `MainVrScene.unity`, under the camera rig: confirm a camera rig exists (`meta_get_config_information` first per the `new-xr-interaction` skill), then add the Meta hand-tracking Building Block (the `OVRHandPrefab` L/R from `com.meta.xr.sdk.core` BuildingBlocks). Delete the hand-rolled `OVRHand`/`OVRSkeleton` GameObjects. The Building Block hands are pre-configured (`HandType`/`_skeletonType` set correctly — avoiding the `HandType = -1`/0-bone bug documented in `CLAUDE.md`).

- [ ] **Step 4: Repoint references in the scene**

- `OvrHandPoseSource`: set `_leftSkeleton` / `_rightSkeleton` to the Building Block's left/right `OVRSkeleton`, and `_centerEye` / `_trackingSpace` to the camera rig (unchanged if already wired).
- Replace the `FirstLightAutoCapture` component on the recorder object with `PinchRecordingTrigger`; set `_triggerHand` to the **non-dominant** Building Block `OVRHand`, `_poseSourceBehaviour` to the `OvrHandPoseSource`.
- `GhostHandPlayer`: set `_leftViewBehaviour` / `_rightViewBehaviour` to `SkinnedGhostHandView` components (add two — one per hand). Assign each its `_handMesh` (`OVRHand_L.fbx` mesh for left, `OVRHand_R.fbx` for right) and `_ghostMaterial` = `M_GhostHands`. Keep the `FkGhostHandView` objects in the scene but disconnected (fallback until Task 9).
- `RecordReplayLink`: set `_recorder` to the `PinchRecordingTrigger`, `_player` to the `GhostHandPlayer`.

- [ ] **Step 5: Verify the scene loads + captures**

`Unity_ReadConsole` (errors-only): no missing-reference or null errors on scene open. `Unity_SceneView_Capture2DScene`: confirm the rig + ghost objects are present and sanely placed. (Hand tracking itself renders only on device.)

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Foundation/App/RecordReplayLink.cs Assets/Scripts/Foundation/App/RecordReplayLink.cs.meta \
        Assets/Scenes/MainVrScene.unity
git rm Assets/Scripts/Foundation/App/FirstLightReplayLink.cs Assets/Scripts/Foundation/App/FirstLightReplayLink.cs.meta 2>/dev/null || true
git commit -m "refactor(app): rename to RecordReplayLink + wire Meta hand rig in scene"
```

> If `Unity_ManageScript` performed the rename in-place (same GUID), the `git rm` is a no-op (the `|| true` guards it). If you renamed on disk, ensure the old paths are removed and the GUID was preserved.

---

## Task 7: Device verification (Quest 3)

The slice's behaviour is only observable on device. This task has no code — it is the mandatory on-device acceptance gate before the FK fallback is removed.

**Files:** none (build + run on device).

- [ ] **Step 1: Build & run on the Quest 3**

`Cmd+B` (Build And Run) on the Mac station, or a Windows teammate over Quest Link. Confirm the build succeeds (the `MetaAarNamespacePatcher` patches the two AARs — log shows two `[MetaAarNamespacePatcher] Patched …` lines).

- [ ] **Step 2: Acceptance checklist (on device)**

- Live hands are visible while recording (the Building Block hands render).
- Non-dominant index pinch arms the take; the console logs `3... 2... 1...` then `Recording`.
- A second pinch stops and saves; console logs `Saved N frames (L:… R:… bones)` with bone counts ≥ 26 (OpenXR).
- Replay shows the **skinned translucent ghost mesh** (not joint spheres), correct left/right hands, correct finger articulation, deforming with the recorded motion.
- The ghost reads clearly against passthrough (translucent + rim, not washed out / not opaque).
- A/B against the FK fallback (temporarily swap `GhostHandPlayer`'s views) confirms the mesh is at least as faithful.

Drive logs without a headset display via adb (device tethered to the Mac):
```bash
ADB=/Applications/Unity/Hub/Editor/6000.4.10f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
$ADB shell am force-stop com.ankhora.app; $ADB logcat -c
$ADB shell monkey -p com.ankhora.app -c android.intent.category.LAUNCHER 1
sleep 20; $ADB logcat -d -s Unity
```

- [ ] **Step 3: Record the result**

If the mesh binds correctly → proceed to Task 8 (deletions) and Task 9 (remove FK). If the mesh is inverted/exploded → debug bone order / left-right mesh assignment (spec Risk 1); the slice can still ship on FK by leaving `GhostHandPlayer` pointed at `FkGhostHandView` and deferring Task 9. Note the outcome in the PR description.

---

## Task 8: Delete the replaced scaffolding

Now that `PinchRecordingTrigger` + `RecordingCountdown` are in and the trigger is wired, remove the auto-capture harness they replace. Independent of the device result (these are unconditionally superseded).

**Files:**
- Delete: `Assets/Scripts/Foundation/Recording/FirstLightAutoCapture.cs` (+ `.meta`)
- Delete: `Assets/Scripts/Domain/Recording/AutoCaptureClock.cs` (+ `.meta`)
- Delete: `Assets/Tests/EditMode/AutoCaptureClockTests.cs` (+ `.meta`)

- [ ] **Step 1: Delete the files**

```bash
git rm Assets/Scripts/Foundation/Recording/FirstLightAutoCapture.cs Assets/Scripts/Foundation/Recording/FirstLightAutoCapture.cs.meta
git rm Assets/Scripts/Domain/Recording/AutoCaptureClock.cs Assets/Scripts/Domain/Recording/AutoCaptureClock.cs.meta
git rm Assets/Tests/EditMode/AutoCaptureClockTests.cs Assets/Tests/EditMode/AutoCaptureClockTests.cs.meta
```

- [ ] **Step 2: Verify nothing references them**

`grep -rn "FirstLightAutoCapture\|AutoCaptureClock\|AutoCapturePhase" Assets/` returns nothing. `Unity_ReadConsole` (errors-only): no compile errors. Run the full EditMode suite — green (the deleted `AutoCaptureClockTests` is gone; `RecordingCountdownTests` + `PinchEdgeDetectorTests` cover the replacement).

- [ ] **Step 3: Commit**

```bash
git commit -m "chore(recording): remove FirstLightAutoCapture + AutoCaptureClock scaffolding"
```

---

## Task 9: Remove the FK fallback (only after Task 7 passes)

**Gated on Task 7 Step 3 reporting the skinned mesh verified on device.** If the mesh was not verified, **skip this task** and leave `FkGhostHandView` as the active view; note it in the PR.

**Files:**
- Delete: `Assets/Scripts/Foundation/Replay/FkGhostHandView.cs` (+ `.meta`)
- Modify: `Assets/Scenes/MainVrScene.unity` (remove the now-unused FK view objects)

- [ ] **Step 1: Remove FK view objects from the scene**

In `MainVrScene.unity`, delete the disconnected `FkGhostHandView` GameObjects/components left as the fallback in Task 6. Confirm `GhostHandPlayer` still points only at the `SkinnedGhostHandView` components.

- [ ] **Step 2: Delete the script**

```bash
git rm Assets/Scripts/Foundation/Replay/FkGhostHandView.cs Assets/Scripts/Foundation/Replay/FkGhostHandView.cs.meta
```

- [ ] **Step 3: Verify**

`grep -rn "FkGhostHandView" Assets/` returns nothing. `Unity_ReadConsole` (errors-only): no missing-reference errors on scene open. Full EditMode suite green.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/MainVrScene.unity
git commit -m "chore(replay): drop FK joint-sphere ghost fallback after device verification"
```

---

## Self-Review

**1. Spec coverage** (against `docs/superpowers/specs/2026-06-27-hands-consolidation-design.md`):
- Decision 1 (pinch toggle + countdown) → Tasks 1, 2, 3. ✅
- Decision 2 (live hands recording-only) → Task 6 Step 3 (Building Block hands render live); replay shows ghost only (unchanged `GhostHandPlayer`). ✅
- Decision 3 (skinned Meta mesh, FK fallback behind seam) → Tasks 4, 5; fallback kept through Task 8, removed in Task 9. ✅
- Decision 4 (one Meta-provided rig, no hand-rolled skeleton) → Task 6 Step 3. ✅
- New Domain (`RecordingCountdown`, `PinchEdgeDetector`) → Tasks 1, 2 (EditMode-tested). ✅
- New Foundation (`PinchRecordingTrigger`, `SkinnedGhostHandView`) → Tasks 3, 5. ✅
- New Assets (`GhostHands_URP` + `M_GhostHands`) → Task 4. ✅
- Rename `FirstLightReplayLink` → `RecordReplayLink` → Task 6. ✅
- Scene swap to Building Block + repoint refs → Task 6. ✅
- Unchanged spine (`RecordingSession`, `OvrHandPoseSource`, `GhostHandPlayer`, seams, `TimelineSampler`, model) → never edited. ✅
- Sequenced deletions (auto-capture now, FK after device-verify) → Tasks 8, 9. ✅
- Error handling (untracked hand ignored, missing refs log+no-op, count-agnostic bind, save failure surfaced) → Task 3 (`IsTracked` gate, Awake guards, `StopAndPublish` error path), Task 5 (`Bind` sizes from captured skeleton, `Apply` count-agnostic). ✅
- Testing strategy (EditMode pure on Mac; device for the rest) → Tasks 1–2 EditMode, Tasks 3/5/6 compile-clean, Task 7 device gate. ✅

**2. Placeholder scan:** No TBD/TODO/"handle edge cases"; every code step shows complete code; commands are concrete. The one genuinely device-only unknown (mesh bind correctness) is handled as an explicit verification gate (Task 7) with a named fallback, not a placeholder. ✅

**3. Type consistency:** `CountdownPhase{Counting,Live}`, `RecordingCountdown(float)`/`PhaseAt`/`SecondsRemaining`, `PinchEdgeDetector(float)`/`Tick(bool,float)`, `PinchRecordingTrigger.OnRecordingSaved : UnityEvent`, `RecordReplayLink._recorder : PinchRecordingTrigger`, `SkinnedGhostHandView : IHandView` with `_handMesh`/`_ghostMaterial` — names used identically across producing and consuming tasks. `RecordingSession`/`MasterclassStore`/`IHandView`/`HandSkeleton` signatures match the unchanged source read during planning. ✅

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-27-hands-consolidation.md`. Two execution options:

1. **Subagent-Driven (recommended)** — fresh implementer subagent per task, spec+quality review between tasks, fast iteration. Pure-logic tasks (1, 2) → cheap model; MonoBehaviour/scene tasks (3, 5, 6) → standard; shader (4) and device gate (7) → standard.
2. **Inline Execution** — execute tasks in this session with checkpoints for review.

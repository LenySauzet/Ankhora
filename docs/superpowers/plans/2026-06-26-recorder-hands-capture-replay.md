# Hands Capture + Ghost-Hand Replay (S3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Capture the instructor's hand joints (+ head) into the existing `Timeline` spine, persist to JSON on device, and replay it as smoothly-interpolated translucent ghost hands.

**Architecture:** Pure, EditMode-tested logic in `Ankhora.Domain` (a fixed-rate `TimelineRecorder` + an alloc-free `TimelineSampler.SampleHand`), driven by thin device-only MonoBehaviour adapters in `Ankhora.Foundation` (OVRSkeleton read, button controller, ghost-hand view) behind interface seams — mirroring the shipped passthrough feature. Hands-only slice; reuses the #26 data model and `JsonMasterclassSerializer` unchanged.

**Tech Stack:** Unity 6 (`6000.4.10f1`), URP 17.4, Meta XR SDK 201 (`OVRSkeleton`/`OVRHand`), C#, Unity Test Framework (NUnit, EditMode), `JsonUtility`.

## Global Constraints

- **Spec:** `docs/03-xr/recorder-hands-capture-replay.md` (tracking issue #30). Branch: `feat/recorder-hands-capture-replay` (already checked out).
- **No schema change.** Reuse `Masterclass → Chapter → Timeline → PoseFrame { t, head, leftHand, rightHand }`, `HandPose { Pose root, Quaternion[] boneRotations }`. `Masterclass.CurrentSchemaVersion == 1`.
- **Hand-tracking cannot run in Editor Play Mode on macOS** (Quest Link is Windows-only). All non-device logic MUST be EditMode-testable; OVR/MonoBehaviour glue is verified on device or in Meta XR Simulator.
- **Confirm every Meta API signature via context7/Meta docs before writing it** (anti-hallucination, `@CLAUDE.md`). `OVRSkeleton` exposes 19 skinnable bones (`Hand_WristRoot`..`Hand_Pinky3`).
- **No allocation in the replay hot loop.** The sampler writes into caller-owned arrays; the player pre-allocates them once.
- **Sample rate:** fixed **30 Hz**, decoupled from frame rate.
- **C# style:** `PascalCase` types, `camelCase` fields, `[SerializeField] private` over public. Folder == namespace (`Ankhora.Domain.*`, `Ankhora.Foundation.*`). Tests in namespace `Ankhora.Tests.EditMode`.
- **Conventional Commits**; commit after every green step. Every new `.cs` needs its generated `.meta` committed too.

### Running EditMode tests

The Unity Editor is normally open for MCP, which holds the project lock and blocks the CLI runner. To run EditMode tests locally, **either**:
- **Editor open:** `Window > General > Test Runner > EditMode > Run All` (or run a single test by name), or
- **Editor closed:** run the CLI runner —
  ```bash
  "/Applications/Unity/Hub/Editor/6000.4.10f1/Unity.app/Contents/MacOS/Unity" \
    -runTests -batchmode -projectPath "$(pwd)" -testPlatform EditMode \
    -testResults "$(pwd)/Logs/editmode.xml" -logFile "$(pwd)/Logs/editmode.log"
  # exit code 0 = all passed; inspect Logs/editmode.xml on failure
  ```
- **Backstop:** CI (`ci.yml`, `unity-test-runner` EditMode) runs them on every push.

After creating any `.cs` outside the Editor, trigger a reimport so Unity generates the `.meta` (focus the Editor, or via MCP `Unity_RunCommand` calling `AssetDatabase.Refresh()`), then `git add` both the `.cs` and its `.meta`.

---

## Phase A — Domain (pure, fully EditMode-tested, no headset)

### Task 1: `TimelineRecorder` — fixed-rate capture accumulator

**Files:**
- Create: `Assets/Scripts/Domain/Recording/TimelineRecorder.cs`
- Test: `Assets/Tests/EditMode/TimelineRecorderTests.cs`

**Interfaces:**
- Consumes: `Ankhora.Domain.Model.{Timeline, PoseFrame, HandPose}`, `UnityEngine.Pose`.
- Produces: `Ankhora.Domain.Recording.TimelineRecorder` with `TimelineRecorder(float sampleRateHz)`, `bool IsRecording`, `void Begin(float now)`, `void Push(float now, in Pose head, in HandPose left, in HandPose right)`, `Timeline Finish(float now)`.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/TimelineRecorderTests.cs`:

```csharp
using Ankhora.Domain.Model;
using Ankhora.Domain.Recording;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class TimelineRecorderTests
    {
        // 10 Hz -> 0.1 s interval, for round numbers.
        private static TimelineRecorder TenHz() => new TimelineRecorder(10f);
        private static HandPose Tracked() => new HandPose { boneRotations = new[] { Quaternion.identity } };

        [Test]
        public void Begin_ThenFirstPush_EmitsFrameAtZero()
        {
            var rec = TenHz();
            rec.Begin(100f);                                  // non-zero start: t must be relative
            rec.Push(100f, default, Tracked(), Tracked());
            Timeline tl = rec.Finish(100f);

            Assert.AreEqual(1, tl.frames.Count);
            Assert.That(tl.frames[0].t, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Push_WithinInterval_DoesNotEmitSecondFrame()
        {
            var rec = TenHz();
            rec.Begin(0f);
            rec.Push(0f, default, Tracked(), Tracked());      // frame at t=0
            rec.Push(0.05f, default, Tracked(), Tracked());   // 0.05 < 0.1 -> ignored
            Timeline tl = rec.Finish(0.05f);

            Assert.AreEqual(1, tl.frames.Count);
        }

        [Test]
        public void Push_AcrossIntervals_EmitsAtFixedCadence()
        {
            var rec = TenHz();
            rec.Begin(0f);
            rec.Push(0f, default, Tracked(), Tracked());      // t=0
            rec.Push(0.05f, default, Tracked(), Tracked());   // ignored
            rec.Push(0.1f, default, Tracked(), Tracked());    // t=0.1
            rec.Push(0.2f, default, Tracked(), Tracked());    // t=0.2
            Timeline tl = rec.Finish(0.2f);

            Assert.AreEqual(3, tl.frames.Count);
            Assert.That(tl.frames[1].t, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(tl.frames[2].t, Is.EqualTo(0.2f).Within(1e-4f));
        }

        [Test]
        public void Finish_SetsDurationRelativeToStart()
        {
            var rec = TenHz();
            rec.Begin(5f);
            rec.Push(5f, default, Tracked(), Tracked());
            Timeline tl = rec.Finish(7.5f);

            Assert.That(tl.durationSeconds, Is.EqualTo(2.5f).Within(1e-4f));
        }

        [Test]
        public void Push_BeforeBegin_IsIgnored()
        {
            var rec = TenHz();
            rec.Push(0f, default, Tracked(), Tracked());      // no Begin yet
            rec.Begin(0f);
            rec.Push(0f, default, Tracked(), Tracked());
            Timeline tl = rec.Finish(0f);

            Assert.AreEqual(1, tl.frames.Count);
        }

        [Test]
        public void StoresHeadAndBothHands()
        {
            var rec = TenHz();
            rec.Begin(0f);
            var head = new Pose(new Vector3(1f, 2f, 3f), Quaternion.identity);
            rec.Push(0f, head, Tracked(), Tracked());
            Timeline tl = rec.Finish(0f);

            Assert.That(tl.frames[0].head.position.z, Is.EqualTo(3f).Within(1e-4f));
            Assert.AreEqual(1, tl.frames[0].leftHand.boneRotations.Length);
            Assert.AreEqual(1, tl.frames[0].rightHand.boneRotations.Length);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode suite (see Global Constraints). Expected: FAIL — `TimelineRecorder` does not exist (compile error).

- [ ] **Step 3: Write the minimal implementation**

Create `Assets/Scripts/Domain/Recording/TimelineRecorder.cs`:

```csharp
using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>
    /// Builds a <see cref="Timeline"/> by sampling head + hand poses at a fixed rate on one
    /// monotonic clock, independent of frame rate. Pure and deterministic (no engine state, no
    /// wall-clock) so the capture cadence is EditMode-testable without a headset. The caller feeds
    /// it the current time + poses every frame via <see cref="Push"/>; it decides when to emit a
    /// <see cref="PoseFrame"/>.
    /// </summary>
    public class TimelineRecorder
    {
        private readonly float _sampleInterval;
        private Timeline _timeline;
        private float _startTime;
        private float _nextSampleTime;
        private bool _recording;

        /// <param name="sampleRateHz">Frames per second to capture (e.g. 30).</param>
        public TimelineRecorder(float sampleRateHz)
        {
            _sampleInterval = sampleRateHz > 0f ? 1f / sampleRateHz : 0f;
        }

        public bool IsRecording => _recording;

        /// <summary>Start a fresh recording; <paramref name="now"/> is the zero of the timeline clock.</summary>
        public void Begin(float now)
        {
            _timeline = new Timeline();
            _startTime = now;
            _nextSampleTime = now;   // emit the first frame immediately at t = 0
            _recording = true;
        }

        /// <summary>
        /// Call every frame while recording with the current clock + poses. Emits a frame each time
        /// the fixed interval has elapsed; otherwise does nothing.
        /// </summary>
        public void Push(float now, in Pose head, in HandPose left, in HandPose right)
        {
            if (!_recording || now < _nextSampleTime)
                return;

            _timeline.frames.Add(new PoseFrame
            {
                t = now - _startTime,
                head = head,
                leftHand = left,
                rightHand = right,
            });

            _nextSampleTime += _sampleInterval;
            // If a frame hitched and we fell behind, resync to avoid a burst of catch-up frames.
            if (_nextSampleTime < now)
                _nextSampleTime = now + _sampleInterval;
        }

        /// <summary>Stop recording and return the finished timeline with its duration set.</summary>
        public Timeline Finish(float now)
        {
            _recording = false;
            Timeline result = _timeline;
            if (result != null)
                result.durationSeconds = now - _startTime;
            _timeline = null;
            return result;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode suite. Expected: PASS (all 6 `TimelineRecorderTests`, and the existing suites stay green).

- [ ] **Step 5: Refresh Unity so the `.meta` files generate, then commit**

```bash
git add Assets/Scripts/Domain/Recording/TimelineRecorder.cs \
        Assets/Scripts/Domain/Recording/TimelineRecorder.cs.meta \
        Assets/Tests/EditMode/TimelineRecorderTests.cs \
        Assets/Tests/EditMode/TimelineRecorderTests.cs.meta
git commit -m "feat(domain): add fixed-rate TimelineRecorder (#30)"
```

---

### Task 2: `TimelineSampler.SampleHand` — alloc-free hand interpolation

**Files:**
- Modify: `Assets/Scripts/Domain/Sampling/TimelineSampler.cs`
- Test: `Assets/Tests/EditMode/TimelineSamplerHandTests.cs`

**Interfaces:**
- Consumes: `Timeline`, `PoseFrame`, `HandPose`, `UnityEngine.{Pose, Quaternion, Vector3, Mathf}`.
- Produces: `static bool TimelineSampler.SampleHand(Timeline timeline, float t, bool rightHand, Quaternion[] into, out Pose root)` — returns `true` when the hand is tracked at `t` (and fills `into`/`root`), `false` otherwise.

- [ ] **Step 1: Write the failing tests**

Create `Assets/Tests/EditMode/TimelineSamplerHandTests.cs`:

```csharp
using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class TimelineSamplerHandTests
    {
        private static HandPose Hand(Vector3 rootPos, params Quaternion[] bones) =>
            new HandPose { root = new Pose(rootPos, Quaternion.identity), boneRotations = bones };

        // Two frames 1s apart; right hand root x goes 0 -> 10, one bone 0deg -> 90deg about Z.
        private static Timeline TwoHandFrames()
        {
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f, rightHand = Hand(new Vector3(0f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 1f, rightHand = Hand(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f)) });
            return tl;
        }

        [Test]
        public void SampleHand_AtFrameTime_ReturnsThatFrameRootAndBones()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 0f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Quaternion.Angle(into[0], Quaternion.identity), Is.LessThan(0.1f));
        }

        [Test]
        public void SampleHand_Midpoint_InterpolatesRootAndBones()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 0.5f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(5f).Within(1e-4f));          // lerp
            Assert.That(Quaternion.Angle(into[0], Quaternion.Euler(0f, 0f, 45f)), Is.LessThan(0.5f)); // slerp
        }

        [Test]
        public void SampleHand_BeforeFirst_ClampsToFirst()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), -1f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_AfterLast_ClampsToLast()
        {
            var into = new Quaternion[1];
            bool tracked = TimelineSampler.SampleHand(TwoHandFrames(), 99f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_NotTracked_ReturnsFalse()
        {
            // Both frames have an empty right hand (boneRotations null) -> not tracked.
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f });
            tl.frames.Add(new PoseFrame { t = 1f });
            var into = new Quaternion[1];

            bool tracked = TimelineSampler.SampleHand(tl, 0.5f, rightHand: true, into, out _);

            Assert.IsFalse(tracked);
        }

        [Test]
        public void SampleHand_MixedTracking_UsesTheTrackedFrame()
        {
            // Frame A tracked, frame B not: a sample between them should use A and report tracked.
            var tl = new Timeline { durationSeconds = 1f };
            tl.frames.Add(new PoseFrame { t = 0f, rightHand = Hand(new Vector3(2f, 0f, 0f), Quaternion.identity) });
            tl.frames.Add(new PoseFrame { t = 1f });
            var into = new Quaternion[1];

            bool tracked = TimelineSampler.SampleHand(tl, 0.5f, rightHand: true, into, out Pose root);

            Assert.IsTrue(tracked);
            Assert.That(root.position.x, Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void SampleHand_WritesIntoCallerArray_NoNewAllocation()
        {
            var into = new Quaternion[1];
            Quaternion[] reference = into;

            TimelineSampler.SampleHand(TwoHandFrames(), 0f, rightHand: true, into, out _);

            Assert.AreSame(reference, into, "Sampler must fill the caller-owned array, not replace it.");
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run the EditMode suite. Expected: FAIL — `SampleHand` does not exist (compile error). Existing `TimelineSamplerTests` (head) still compile.

- [ ] **Step 3: Add the implementation**

In `Assets/Scripts/Domain/Sampling/TimelineSampler.cs`, add the following members inside the `TimelineSampler` class (keep `SampleHead` as-is):

```csharp
/// <summary>
/// Samples one hand at time <paramref name="t"/> into the caller-owned <paramref name="into"/>
/// array (no allocation), returning whether the hand is tracked there. Clamps to the first/last
/// frame outside the range; interpolates root (lerp/slerp) and each bone rotation (slerp) between
/// the two bracketing frames. When only one bracketing frame has the hand tracked, uses that one;
/// when neither does, returns false and leaves <paramref name="into"/> untouched.
/// </summary>
public static bool SampleHand(Timeline timeline, float t, bool rightHand, Quaternion[] into, out Pose root)
{
    root = default;
    var frames = timeline?.frames;
    if (frames == null || frames.Count == 0)
        return false;

    int lastIndex = frames.Count - 1;
    if (t <= frames[0].t)
        return EmitHand(HandOf(frames[0], rightHand), into, out root);
    if (t >= frames[lastIndex].t)
        return EmitHand(HandOf(frames[lastIndex], rightHand), into, out root);

    int lo = 0;
    int hi = lastIndex;
    while (hi - lo > 1)
    {
        int mid = (lo + hi) >> 1;
        if (frames[mid].t <= t)
            lo = mid;
        else
            hi = mid;
    }

    PoseFrame fa = frames[lo];
    PoseFrame fb = frames[hi];
    HandPose a = HandOf(fa, rightHand);
    HandPose b = HandOf(fb, rightHand);
    bool ta = IsTracked(a);
    bool tb = IsTracked(b);

    if (!ta && !tb)
        return false;
    if (ta && !tb)
        return EmitHand(a, into, out root);
    if (!ta)
        return EmitHand(b, into, out root);

    float span = fb.t - fa.t;
    float u = span > 0f ? (t - fa.t) / span : 0f;
    root = new Pose(
        Vector3.LerpUnclamped(a.root.position, b.root.position, u),
        Quaternion.SlerpUnclamped(a.root.rotation, b.root.rotation, u));

    int n = Mathf.Min(into.Length, Mathf.Min(a.boneRotations.Length, b.boneRotations.Length));
    for (int i = 0; i < n; i++)
        into[i] = Quaternion.SlerpUnclamped(a.boneRotations[i], b.boneRotations[i], u);
    return true;
}

private static HandPose HandOf(in PoseFrame f, bool rightHand) => rightHand ? f.rightHand : f.leftHand;

private static bool IsTracked(in HandPose h) => h.boneRotations != null && h.boneRotations.Length > 0;

private static bool EmitHand(HandPose h, Quaternion[] into, out Pose root)
{
    root = default;
    if (!IsTracked(h))
        return false;

    root = h.root;
    int n = Mathf.Min(into.Length, h.boneRotations.Length);
    for (int i = 0; i < n; i++)
        into[i] = h.boneRotations[i];
    return true;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run the EditMode suite. Expected: PASS (7 new `TimelineSamplerHandTests` + existing head/serialization suites green).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Domain/Sampling/TimelineSampler.cs \
        Assets/Tests/EditMode/TimelineSamplerHandTests.cs \
        Assets/Tests/EditMode/TimelineSamplerHandTests.cs.meta
git commit -m "feat(domain): add alloc-free TimelineSampler.SampleHand (#30)"
```

---

### Task 3: Captured-shape round-trip test (19 bones × 2 hands × many frames)

**Files:**
- Modify: `Assets/Tests/EditMode/MasterclassSerializationTests.cs`

**Interfaces:**
- Consumes: `TimelineRecorder` (Task 1), `JsonMasterclassSerializer`, the model types.

This proves the spec's persistence acceptance criterion with realistic capture data — the existing tests cover the mechanism on 1–3 bones; this covers a full 19-bone, two-hand, multi-frame recording produced by the recorder.

- [ ] **Step 1: Write the failing test**

Add to `MasterclassSerializationTests` (in `MasterclassSerializationTests.cs`):

```csharp
[Test]
public void RoundTrip_FullCapturedTimeline_PreservesAllFramesAndBones()
{
    // Build a realistic capture: 30 frames, both hands, 19 bones each, via the recorder.
    const int boneCount = 19;
    var recorder = new Ankhora.Domain.Recording.TimelineRecorder(30f);
    recorder.Begin(0f);
    for (int frame = 0; frame < 30; frame++)
    {
        float now = frame / 30f;
        var left = new HandPose { root = new Pose(Vector3.one * frame, Quaternion.identity), boneRotations = new Quaternion[boneCount] };
        var right = new HandPose { root = new Pose(Vector3.one * -frame, Quaternion.identity), boneRotations = new Quaternion[boneCount] };
        for (int b = 0; b < boneCount; b++)
        {
            left.boneRotations[b] = Quaternion.Euler(frame + b, 0f, 0f);
            right.boneRotations[b] = Quaternion.Euler(0f, frame + b, 0f);
        }
        recorder.Push(now, new Pose(Vector3.up * frame, Quaternion.identity), left, right);
    }
    Timeline tl = recorder.Finish(29f / 30f);

    var mc = new Masterclass { id = "mc", title = "Captured" };
    var ch = new Chapter { id = "c", timeline = tl };
    mc.chapters.Add(ch);

    IMasterclassSerializer serializer = new JsonMasterclassSerializer();
    Masterclass restored = serializer.Deserialize(serializer.Serialize(mc));

    Timeline rtl = restored.chapters[0].timeline;
    Assert.AreEqual(tl.frames.Count, rtl.frames.Count, "All sampled frames must survive the round-trip.");
    Assert.AreEqual(boneCount, rtl.frames[10].leftHand.boneRotations.Length);
    Assert.AreEqual(boneCount, rtl.frames[10].rightHand.boneRotations.Length);
    Assert.That(Quaternion.Angle(rtl.frames[10].leftHand.boneRotations[5], Quaternion.Euler(15f, 0f, 0f)), Is.LessThan(0.5f));
}
```

- [ ] **Step 2: Run the test to verify it fails (or passes-by-construction)**

Run the EditMode suite. Expected: PASS once Task 1 exists — this is an integration test over already-correct code. If `frames.Count` mismatches, the recorder cadence is wrong (return to Task 1). Treat a failure here as a real defect, not a missing feature.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/EditMode/MasterclassSerializationTests.cs
git commit -m "test(domain): round-trip a full 19-bone two-hand captured timeline (#30)"
```

---

## Phase B — Capture adapters (`Ankhora.Foundation`, device-verified)

> These read OVR/engine state and cannot be EditMode-tested on macOS. Each task ends with: compile clean (Unity Console shows no errors) + an on-device/Simulator verification step. CI compiles them on push.

### Task 4: `IHandPoseSource` seam + `SimulatedHandPoseSource`

**Files:**
- Create: `Assets/Scripts/Foundation/Recording/IHandPoseSource.cs`
- Create: `Assets/Scripts/Foundation/Recording/SimulatedHandPoseSource.cs`

**Interfaces:**
- Produces: `Ankhora.Foundation.Recording.IHandPoseSource` with `bool TryGetHead(out Pose head)` and `bool TryGetHand(bool rightHand, ref HandPose pose)` (fills `pose.root` + `pose.boneRotations`, returns tracked). `SimulatedHandPoseSource : MonoBehaviour, IHandPoseSource` produces deterministic synthetic motion for headless smoke-testing.

- [ ] **Step 1: Create the seam**

`Assets/Scripts/Foundation/Recording/IHandPoseSource.cs`:

```csharp
using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Per-frame source of head + hand poses for the recorder. Behind an interface so the recording
    /// loop can be driven by the real OVR skeleton on device or a simulated source headless — the
    /// engine/OVR dependency stays on the concrete implementations.
    /// </summary>
    public interface IHandPoseSource
    {
        /// <summary>Current head pose in tracking space; false if unavailable.</summary>
        bool TryGetHead(out Pose head);

        /// <summary>
        /// Fills <paramref name="pose"/> (root + bone rotations) for one hand and returns whether it
        /// is tracked. Implementations reuse <paramref name="pose"/>'s bone array when its length
        /// already matches to avoid per-frame allocation.
        /// </summary>
        bool TryGetHand(bool rightHand, ref HandPose pose);
    }
}
```

- [ ] **Step 2: Create the simulated source**

`Assets/Scripts/Foundation/Recording/SimulatedHandPoseSource.cs`:

```csharp
using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Deterministic synthetic <see cref="IHandPoseSource"/> for smoke-testing the record -> replay
    /// loop without a headset (Editor Play Mode on macOS cannot produce hand tracking). Drives both
    /// hands through a slow looping wave so a recorded take has visible motion to replay.
    /// </summary>
    public class SimulatedHandPoseSource : MonoBehaviour, IHandPoseSource
    {
        [SerializeField, Min(1)] private int _boneCount = 19;

        public bool TryGetHead(out Pose head)
        {
            head = new Pose(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            return true;
        }

        public bool TryGetHand(bool rightHand, ref HandPose pose)
        {
            float phase = Time.time * 2f + (rightHand ? Mathf.PI : 0f);
            float side = rightHand ? 0.2f : -0.2f;
            pose.root = new Pose(
                new Vector3(side, 1.2f + 0.1f * Mathf.Sin(phase), 0.4f),
                Quaternion.Euler(0f, 0f, 20f * Mathf.Sin(phase)));

            if (pose.boneRotations == null || pose.boneRotations.Length != _boneCount)
                pose.boneRotations = new Quaternion[_boneCount];
            for (int i = 0; i < _boneCount; i++)
                pose.boneRotations[i] = Quaternion.Euler(15f * Mathf.Sin(phase + i * 0.3f), 0f, 0f);
            return true;
        }
    }
}
```

- [ ] **Step 3: Verify it compiles**

Refresh Unity (focus Editor or MCP `AssetDatabase.Refresh()`), then confirm the Console has no compile errors (MCP `Unity_ReadConsole` Errors, or the Editor Console).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Foundation/Recording/IHandPoseSource.cs \
        Assets/Scripts/Foundation/Recording/IHandPoseSource.cs.meta \
        Assets/Scripts/Foundation/Recording/SimulatedHandPoseSource.cs \
        Assets/Scripts/Foundation/Recording/SimulatedHandPoseSource.cs.meta
git commit -m "feat(foundation): add IHandPoseSource seam + simulated source (#30)"
```

---

### Task 5: `OvrHandPoseSource` — read OVRSkeleton on device

**Files:**
- Create: `Assets/Scripts/Foundation/Recording/OvrHandPoseSource.cs`

**Interfaces:**
- Consumes: `IHandPoseSource` (Task 4), Meta `OVRSkeleton`/`OVRHand`, a head `Transform`.
- Produces: `OvrHandPoseSource : MonoBehaviour, IHandPoseSource`.

- [ ] **Step 1: Confirm the OVRSkeleton API via context7**

Before writing OVR calls, query context7 / Meta docs to confirm, for SDK 201: how to read per-bone **local** rotations (`OVRSkeleton.Bones`, `bone.Transform.localRotation`, `bone.Id`), the bone count / `GetCurrentNumBones`, the wrist root, and tracking validity (`OVRSkeleton.IsDataValid` / `IsDataHighConfidence`). Adjust the code in Step 2 to the confirmed signatures. Do not guess.

- [ ] **Step 2: Implement against the confirmed API**

`Assets/Scripts/Foundation/Recording/OvrHandPoseSource.cs` (the OVR member names below are the expected SDK-201 shape — reconcile with Step 1 before relying on them):

```csharp
using Ankhora.Domain.Model;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Reads the live Meta hand skeletons + head each frame as <see cref="IHandPoseSource"/> for the
    /// recorder. Device-only: OVRSkeleton produces no data in macOS Editor Play Mode. Captures each
    /// bone's LOCAL rotation + the wrist root pose (compact, retargetable) — never world transforms.
    /// </summary>
    public class OvrHandPoseSource : MonoBehaviour, IHandPoseSource
    {
        [SerializeField] private OVRSkeleton _leftSkeleton;
        [SerializeField] private OVRSkeleton _rightSkeleton;
        [SerializeField] private Transform _centerEye;

        public bool TryGetHead(out Pose head)
        {
            if (_centerEye == null) { head = default; return false; }
            head = new Pose(_centerEye.localPosition, _centerEye.localRotation);
            return true;
        }

        public bool TryGetHand(bool rightHand, ref HandPose pose)
        {
            OVRSkeleton skeleton = rightHand ? _rightSkeleton : _leftSkeleton;
            if (skeleton == null || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence)
                return false;

            var bones = skeleton.Bones;                 // IList<OVRBone>; index 0 == Hand_WristRoot
            int count = bones.Count;
            if (count == 0)
                return false;

            Transform wrist = bones[0].Transform;
            pose.root = new Pose(wrist.localPosition, wrist.localRotation);

            if (pose.boneRotations == null || pose.boneRotations.Length != count)
                pose.boneRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
                pose.boneRotations[i] = bones[i].Transform.localRotation;
            return true;
        }
    }
}
```

- [ ] **Step 3: Verify it compiles**

Refresh Unity; confirm no Console errors (the `Ankhora.Foundation` asmdef already references `Oculus.VR`, so OVR types resolve).

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Foundation/Recording/OvrHandPoseSource.cs \
        Assets/Scripts/Foundation/Recording/OvrHandPoseSource.cs.meta
git commit -m "feat(foundation): add OvrHandPoseSource reading OVRSkeleton (#30)"
```

---

### Task 6: `MasterclassRecorderController` — button + record loop + save

**Files:**
- Create: `Assets/Scripts/Foundation/Recording/MasterclassRecorderController.cs`

**Interfaces:**
- Consumes: `IHandPoseSource` (Task 4), `TimelineRecorder` (Task 1), `JsonMasterclassSerializer`, `OVRInput`.
- Produces: `MasterclassRecorderController : MonoBehaviour`; writes a JSON file at `Application.persistentDataPath/<fileName>`; exposes `string SavedFilePath` and `bool IsRecording`.

- [ ] **Step 1: Implement**

`Assets/Scripts/Foundation/Recording/MasterclassRecorderController.cs`:

```csharp
using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Recording;
using Ankhora.Domain.Serialization;
using UnityEngine;

namespace Ankhora.Foundation.Recording
{
    /// <summary>
    /// Drives a single hands-only recording: a controller button toggles record; while recording it
    /// pushes head + both hands from an <see cref="IHandPoseSource"/> into a <see cref="TimelineRecorder"/>
    /// at a fixed rate; on stop it wraps the timeline in a Masterclass and writes it to device storage
    /// as JSON. The pure cadence/serialisation logic is tested in EditMode; this MonoBehaviour is the
    /// thin device-side wiring (verified on headset).
    /// </summary>
    public class MasterclassRecorderController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _poseSourceBehaviour; // must implement IHandPoseSource
        [SerializeField, Min(1f)] private float _sampleRateHz = 30f;
        [SerializeField] private OVRInput.Button _recordButton = OVRInput.Button.One; // A / X
        [SerializeField] private string _fileName = "masterclass.json";

        private IHandPoseSource _source;
        private TimelineRecorder _recorder;
        private readonly IMasterclassSerializer _serializer = new JsonMasterclassSerializer();
        private HandPose _left;   // reused buffers (boneRotations arrays are reused by the source)
        private HandPose _right;

        public bool IsRecording => _recorder != null && _recorder.IsRecording;
        public string SavedFilePath { get; private set; }

        private void Awake()
        {
            _source = _poseSourceBehaviour as IHandPoseSource;
            _recorder = new TimelineRecorder(_sampleRateHz);
            SavedFilePath = Path.Combine(Application.persistentDataPath, _fileName);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_recordButton))
                Toggle();

            if (!IsRecording || _source == null)
                return;

            float now = Time.unscaledTime;
            _source.TryGetHead(out Pose head);
            if (!_source.TryGetHand(false, ref _left)) _left.boneRotations = null;
            if (!_source.TryGetHand(true, ref _right)) _right.boneRotations = null;
            _recorder.Push(now, head, _left, _right);
        }

        public void Toggle()
        {
            if (IsRecording) StopAndSave();
            else _recorder.Begin(Time.unscaledTime);
        }

        private void StopAndSave()
        {
            Timeline timeline = _recorder.Finish(Time.unscaledTime);
            var masterclass = new Masterclass { id = "mc-local", title = "Local recording" };
            masterclass.chapters.Add(new Chapter { id = "ch-1", timeline = timeline });

            File.WriteAllText(SavedFilePath, _serializer.Serialize(masterclass));
            Debug.Log($"[MasterclassRecorderController] Saved {timeline.frames.Count} frames to {SavedFilePath}");
        }
    }
}
```

- [ ] **Step 2: Verify it compiles** — refresh Unity, no Console errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Foundation/Recording/MasterclassRecorderController.cs \
        Assets/Scripts/Foundation/Recording/MasterclassRecorderController.cs.meta
git commit -m "feat(foundation): add MasterclassRecorderController with JSON save (#30)"
```

---

## Phase C — Replay (`Ankhora.Foundation`, device-verified)

### Task 7: `IHandView` seam + `DebugJointsHandView` + `MetaGhostHandView`

**Files:**
- Create: `Assets/Scripts/Foundation/Replay/IHandView.cs`
- Create: `Assets/Scripts/Foundation/Replay/DebugJointsHandView.cs`
- Create: `Assets/Scripts/Foundation/Replay/MetaGhostHandView.cs`

**Interfaces:**
- Produces: `Ankhora.Foundation.Replay.IHandView` with `void Show(bool visible)` and `void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)`. Two MonoBehaviour implementations.

- [ ] **Step 1: Create the seam**

`Assets/Scripts/Foundation/Replay/IHandView.cs`:

```csharp
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// A renderable ghost hand the player drives from sampled poses. Behind an interface so the
    /// product visual (skinned Meta mesh) and a debug visual (joint spheres) are interchangeable
    /// behind the same call site.
    /// </summary>
    public interface IHandView
    {
        /// <summary>Show or hide this hand (hidden when the recorded hand is untracked).</summary>
        void Show(bool visible);

        /// <summary>
        /// Apply a sampled pose: <paramref name="root"/> wrist pose + the first
        /// <paramref name="boneCount"/> local bone rotations from <paramref name="boneRotations"/>.
        /// </summary>
        void Apply(in Pose root, Quaternion[] boneRotations, int boneCount);
    }
}
```

- [ ] **Step 2: Create the debug view**

`Assets/Scripts/Foundation/Replay/DebugJointsHandView.cs`:

```csharp
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Diagnostic <see cref="IHandView"/>: drives a pre-assigned chain of joint transforms (small
    /// spheres) by setting each one's local rotation from the sampled bone rotations and the wrist
    /// from the root. Proves the data pipeline cheaply when the skinned mesh looks wrong on device.
    /// </summary>
    public class DebugJointsHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private Transform _wrist;
        [SerializeField] private Transform[] _joints; // ordered to match the captured bone order

        public void Show(bool visible)
        {
            if (_wrist != null)
                _wrist.gameObject.SetActive(visible);
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_wrist != null)
            {
                _wrist.localPosition = root.position;
                _wrist.localRotation = root.rotation;
            }
            if (_joints == null)
                return;
            int n = Mathf.Min(_joints.Length, boneCount);
            for (int i = 0; i < n; i++)
                if (_joints[i] != null)
                    _joints[i].localRotation = boneRotations[i];
        }
    }
}
```

- [ ] **Step 3: Create the Meta ghost view**

`Assets/Scripts/Foundation/Replay/MetaGhostHandView.cs`:

```csharp
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Product <see cref="IHandView"/>: a duplicated Meta hand rig (skinned mesh) rendered with the
    /// translucent fresnel ghost material (see the urp-shadergraph skill). The bone transforms are
    /// assigned in the same order the capture wrote them (OVRSkeleton bone order, wrist at index 0)
    /// so sampled local rotations retarget directly. Device-verified.
    /// </summary>
    public class MetaGhostHandView : MonoBehaviour, IHandView
    {
        [SerializeField] private GameObject _rigRoot;   // the skinned ghost hand, hidden until replay
        [SerializeField] private Transform _wrist;      // wrist bone (drives root pose)
        [SerializeField] private Transform[] _bones;    // bone transforms, capture order (index 0 == wrist)

        public void Show(bool visible)
        {
            if (_rigRoot != null)
                _rigRoot.SetActive(visible);
        }

        public void Apply(in Pose root, Quaternion[] boneRotations, int boneCount)
        {
            if (_wrist != null)
            {
                _wrist.localPosition = root.position;
                _wrist.localRotation = root.rotation;
            }
            if (_bones == null)
                return;
            int n = Mathf.Min(_bones.Length, boneCount);
            for (int i = 0; i < n; i++)
                if (_bones[i] != null)
                    _bones[i].localRotation = boneRotations[i];
        }
    }
}
```

- [ ] **Step 4: Verify it compiles** — refresh Unity, no Console errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Foundation/Replay/IHandView.cs \
        Assets/Scripts/Foundation/Replay/IHandView.cs.meta \
        Assets/Scripts/Foundation/Replay/DebugJointsHandView.cs \
        Assets/Scripts/Foundation/Replay/DebugJointsHandView.cs.meta \
        Assets/Scripts/Foundation/Replay/MetaGhostHandView.cs \
        Assets/Scripts/Foundation/Replay/MetaGhostHandView.cs.meta
git commit -m "feat(foundation): add IHandView seam + ghost/debug hand views (#30)"
```

---

### Task 8: `GhostHandPlayer` — load + play + drive ghost hands

**Files:**
- Create: `Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs`

**Interfaces:**
- Consumes: `IHandView` (Task 7), `TimelineSampler.SampleHand` (Task 2), `JsonMasterclassSerializer`, `OVRInput`.
- Produces: `GhostHandPlayer : MonoBehaviour`.

- [ ] **Step 1: Implement**

`Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs`:

```csharp
using System.IO;
using Ankhora.Domain.Model;
using Ankhora.Domain.Sampling;
using Ankhora.Domain.Serialization;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Loads a recorded masterclass from device storage and replays its first chapter as ghost hands:
    /// advances a playback clock, samples both hands from the <see cref="Timeline"/> each frame
    /// (into reused arrays, no hot-loop allocation), and drives an <see cref="IHandView"/> per hand.
    /// A controller button starts playback; replay loops if enabled.
    /// </summary>
    public class GhostHandPlayer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _leftViewBehaviour;   // implements IHandView
        [SerializeField] private MonoBehaviour _rightViewBehaviour;  // implements IHandView
        [SerializeField] private OVRInput.Button _playButton = OVRInput.Button.Two; // shared B/Y? choose a free button
        [SerializeField] private string _fileName = "masterclass.json";
        [SerializeField] private bool _loop = true;
        [SerializeField, Min(1)] private int _boneCapacity = 19;

        private readonly IMasterclassSerializer _serializer = new JsonMasterclassSerializer();
        private IHandView _leftView;
        private IHandView _rightView;
        private Timeline _timeline;
        private Quaternion[] _leftBones;
        private Quaternion[] _rightBones;
        private float _clock;
        private bool _playing;

        private void Awake()
        {
            _leftView = _leftViewBehaviour as IHandView;
            _rightView = _rightViewBehaviour as IHandView;
            _leftBones = new Quaternion[_boneCapacity];
            _rightBones = new Quaternion[_boneCapacity];
            _leftView?.Show(false);
            _rightView?.Show(false);
        }

        private void Update()
        {
            if (OVRInput.GetDown(_playButton))
                LoadAndPlay();

            if (!_playing || _timeline == null)
                return;

            _clock += Time.deltaTime;
            if (_clock >= _timeline.durationSeconds)
            {
                if (_loop) _clock = 0f;
                else { Stop(); return; }
            }

            DriveHand(_leftView, rightHand: false, _leftBones);
            DriveHand(_rightView, rightHand: true, _rightBones);
        }

        private void DriveHand(IHandView view, bool rightHand, Quaternion[] buffer)
        {
            if (view == null)
                return;
            bool tracked = TimelineSampler.SampleHand(_timeline, _clock, rightHand, buffer, out Pose root);
            view.Show(tracked);
            if (tracked)
                view.Apply(root, buffer, buffer.Length);
        }

        public void LoadAndPlay()
        {
            string path = Path.Combine(Application.persistentDataPath, _fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[GhostHandPlayer] No recording at {path}");
                return;
            }

            Masterclass mc;
            try { mc = _serializer.Deserialize(File.ReadAllText(path)); }
            catch (System.Exception e) { Debug.LogError($"[GhostHandPlayer] Load failed: {e.Message}"); return; }

            if (mc.chapters.Count == 0 || mc.chapters[0].timeline.frames.Count == 0)
            {
                Debug.LogWarning("[GhostHandPlayer] Recording has no frames.");
                return;
            }

            _timeline = mc.chapters[0].timeline;
            _clock = 0f;
            _playing = true;
        }

        public void Stop()
        {
            _playing = false;
            _leftView?.Show(false);
            _rightView?.Show(false);
        }
    }
}
```

- [ ] **Step 2: Verify it compiles** — refresh Unity, no Console errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs \
        Assets/Scripts/Foundation/Replay/GhostHandPlayer.cs.meta
git commit -m "feat(foundation): add GhostHandPlayer driving ghost hands from JSON (#30)"
```

---

## Phase D — Scene wiring + on-device verification

### Task 9: Wire `MainVrScene` and verify the loop end-to-end on Quest

**Files:**
- Modify: `Assets/Scenes/MainVrScene.unity` (via the Editor / MCP — not hand-edited YAML)

**This task has no unit test** — it is the on-device acceptance gate. Use the `xr-scene-builder` agent or the Editor + MCP `meta_*`/`Unity_*` tools.

- [ ] **Step 1: Confirm the live hand rig + ghost mesh source**

Call `meta_get_config_information`; confirm hand tracking is enabled in the project config and the `OVRCameraRig` exposes left/right `OVRSkeleton` (add an interaction/hand rig via `meta_add_interactionrig` if absent). Resolve the open question from the spec: pick the ghost-hand mesh source (duplicated Meta hand rig vs a skinned hand prefab) and confirm its bone transform order matches OVRSkeleton (wrist at index 0).

- [ ] **Step 2: Add and wire the capture objects**

In `MainVrScene`: add a `MasterclassRecorderController`; assign its `_poseSourceBehaviour` to an `OvrHandPoseSource` wired to the scene's left/right `OVRSkeleton` + centre-eye. Confirm the record button (`OVRInput.Button.One`, A/X) does not collide with the passthrough toggle (`OVRInput.Button.Two`, B/Y).

- [ ] **Step 3: Add and wire the replay objects**

Add two ghost hand rigs (left/right, hidden), each with a `MetaGhostHandView` (bones assigned in capture order) using the translucent ghost material. Add a `GhostHandPlayer`, assign its two views, set its play button to a free input (e.g. a trigger), and the same `_fileName` as the recorder. Optionally add `DebugJointsHandView` rigs behind the same fields for diagnostics.

- [ ] **Step 4: Capture a sanity view**

`Unity_SceneView_Capture2DScene` (or `CaptureMultiAngleSceneView`) to confirm the rigs are present and placed. `Unity_ReadConsole` shows no errors.

- [ ] **Step 5: Build & run on device and verify acceptance criteria**

`Cmd+B` (or `/build-android`). On the Quest 3:
- Press A/X → record a clear hand gesture (wave) for ~5 s → press A/X to stop.
- Confirm the log line `Saved N frames to …/masterclass.json` (N ≈ 5 s × 30 = ~150).
- Press the play button → translucent ghost hands replay the gesture, smoothly, in place.
- Toggle passthrough (B/Y) during replay → ghosts remain visible and stable; **90 FPS** held (OVR Metrics / Profiler).
- Pull the file to confirm persistence: `adb pull /sdcard/Android/data/com.ankhora.app/files/masterclass.json` (path per `persistentDataPath`).
- If the ghost looks wrong, switch the player's view fields to `DebugJointsHandView` to confirm the data vs the skinning.

- [ ] **Step 6: Commit the scene + open the PR**

```bash
git add Assets/Scenes/MainVrScene.unity
git commit -m "feat(scene): wire hands capture + ghost replay into MainVrScene (#30)"
git push
gh pr create --base main --title "feat(recorder): hands capture + ghost-hand replay (S3)" \
  --body "Closes #30. See docs/03-xr/recorder-hands-capture-replay.md."
```

---

## Self-Review (completed)

- **Spec coverage:** capture (Tasks 1, 4–6) · alloc-free hand sampling (Task 2) · JSON round-trip on device (Tasks 3, 6, 8) · ghost-hand replay with debug seam (Tasks 7–8) · 90 FPS / persistence / on-device acceptance (Task 9). All acceptance criteria map to a task.
- **Out of scope** honored: no voice, pins, Player UI, anchors.
- **Type consistency:** `IHandPoseSource.TryGetHand(bool, ref HandPose)`, `IHandView.Apply(in Pose, Quaternion[], int)`, `TimelineSampler.SampleHand(Timeline, float, bool, Quaternion[], out Pose)`, `TimelineRecorder.{Begin,Push,Finish}` — names/signatures consistent across all consuming tasks.
- **Known follow-ups flagged in-task, not placeholders:** OVRSkeleton API confirmation (Task 5 Step 1), ghost-mesh source decision (Task 9 Step 1) — both are spec "Open questions" resolved at implementation against live docs/config per the anti-hallucination rule.

---
name: unity-testability
description: Use when deciding how to make Ankhora gameplay logic testable, isolating logic out of MonoBehaviour, or choosing between EditMode and PlayMode tests. Especially for the record/replay core (serialising hand poses, timeline stepping, annotation/anchor data) which should be plain C# and EditMode-tested. Triggers: testability, how to test this, write a test, unit test, EditMode, PlayMode, isolate logic, mockable, record replay logic, dependency injection in Unity.
---

# Unity testability (Ankhora)

CI runs `unity-test-runner` (EditMode) — it compiles the project and runs tests without
`BuildPlayer` (`@CLAUDE.md` § *What "build"/"run"/"test" mean today*). EditMode tests are
therefore the cheap, fast safety net. The goal of this skill: keep Ankhora's logic in a
shape where that net actually catches things.

## The core principle

**Push decisions into plain C# classes; keep `MonoBehaviour` thin.** A `MonoBehaviour`
should mostly *gather Unity input* (poses, time, events) and *delegate* to a pure class
that holds the actual rule. The pure class is what you unit-test in EditMode.

This matters most for **Ankhora's record/replay core**: capturing a hand-pose stream,
stepping a timeline, diffing learner vs. expert, (de)serialising a masterclass. None of
that needs a live scene — it is data transformation, ideal for EditMode tests.

## Testability checklist (ask before writing the class)

- Can the rule/algorithm run **without** `Transform`, `GameObject`, or scene state?
  → If yes, put it in a plain C# class (no `MonoBehaviour` base).
- Is time **injected** (a `float deltaTime` / timestamp parameter) rather than read from
  `Time.deltaTime` inside the logic? → Inject it, so tests can drive the timeline deterministically.
- Is configuration **passed in** (constructor / method args / a `ScriptableObject` handed
  over) instead of read through static singletons? → Inject it.
- Does this genuinely need a running scene (physics, frame loop, real input)?
  → Only then reach for a **PlayMode** test. Default to EditMode.

## Shape to aim for

```
// Pure, EditMode-testable: no UnityEngine scene dependencies in the logic.
public sealed class ReplayTimeline
{
    public PoseFrame Sample(float tSeconds) { /* deterministic */ }
}

// Thin MonoBehaviour: gathers Unity state, delegates, applies the result.
public sealed class ReplayPlayer : MonoBehaviour
{
    [SerializeField] private GhostHands ghostHands;
    private ReplayTimeline timeline;
    private void Update() => ghostHands.Apply(timeline.Sample(Time.time));
}
```

## Where tests live

- EditMode: `Assets/Tests/EditMode/` (assembly `Ankhora.Tests.EditMode`, Editor-only).
- PlayMode: `Assets/Tests/PlayMode/` (create when a test genuinely needs the frame loop).

> Note: `.meta` files for test assets are being normalised on `fix/unity-test-metas`.
> When adding test files, ensure each `.cs` has a committed `.meta` (the
> `unity-test-author` agent handles this).

## Hand off

For actually authoring the tests, dispatch the **`unity-test-author`** agent — this skill
decides *what shape* the code should take; that agent writes the tests against it.

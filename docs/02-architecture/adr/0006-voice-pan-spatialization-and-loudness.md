# ADR-0006: Stereo-pan spatialization + RMS loudness for voice narration (HRTF deferred)

- **Status:** Accepted
- **Date:** 2026-06-30
- **Deciders:** Lény Sauzet (device-verified on Quest 3, iterative)
- **Tags:** xr, audio, voice, mvp

## Context and problem

The voice-capture+replay slice (PR #37) adds the Instructor's spatialised narration
alongside the ghost hands, replayed positioned in space and synced to the replay clock.
The original design (spec `docs/superpowers/specs/2026-06-29-voice-capture-replay-design.md`)
called for the **Meta XR Audio spatialiser (HRTF)** so the voice emanates convincingly from
the ghost's head in 3D.

On device this produced **near-inaudible narration** even at half headset volume. Root cause,
confirmed iteratively on Quest 3:

1. With `AudioSource.spatialize = true`, the Meta XR Audio plugin applies its own (heavy)
   distance attenuation and **overrides Unity's `rolloffMode`/`minDistance`** — so tuning the
   Unity rolloff was a no-op while the spatialiser stayed quiet.
2. The Quest microphone captures at a **low gain**, and *peak* normalisation barely lifts
   dynamic speech (peaks can already sit near full-scale while the average level is low).
3. At masterclass range (instructor ~1–2 m, roughly where the learner is looking), the HRTF
   directional cue was **imperceptible** to the tester — it cost loudness for no felt benefit.

Hard constraints: ultra-thin MVP, ~10–14 effective coding days, audio only validatable on
device (the Mac Editor can't render hand tracking but does play audio). Intelligibility of the
narration is the product-critical property for a training tool; precise 3D localisation is not.

## Decision drivers

- Narration must be **clearly audible** at a comfortable, user-adjustable headset volume.
- Audio needs must accommodate **varying hearing abilities and environments** (controllable level).
- Keep the voice **roughly positioned** at the demonstrator without sacrificing loudness.
- Stay within the MVP budget — no DSP rabbit holes; keep the level math pure and EditMode-tested.
- Decision must be reversible: HRTF should remain a clean future option, not be designed out.

## Considered options

1. **Meta XR Audio HRTF** (`spatialize = true`) — true 3D localisation, but heavy attenuation,
   overrides Unity rolloff, imperceptible benefit here, near-inaudible without further plugin
   gain configuration (`MetaXRAudioSource` component + tuning).
2. **Stereo-pan 3D** (`spatialBlend = 1`, `spatialize = false`) — Unity constant-power pan toward
   the source position + Unity-controlled distance rolloff. Loud, light positioning, simple.
3. **Flat 2D** (`spatialBlend = 0`) — loudest, but no positioning at all; the voice no longer
   "comes from" the ghost.

## Decision

We chose **Option 2 — stereo-pan 3D with the HRTF disabled**, paired with **RMS loudness
normalisation** of the captured audio.

The single most important reason: for a training tool, **intelligible, level-controllable
narration beats subtle 3D localisation** — and Option 2 keeps the voice positioned at the ghost
while removing the spatialiser's attenuation.

Implementation (PR #37):

- **Playback** (`Foundation/Replay/VoicePlayer.cs`): `spatialBlend = 1` (constant-power pan toward
  the ghost head, which `GhostHandPlayer` positions each frame), `spatialize = false`,
  `rolloffMode = Linear`, `minDistance = 10 m` / `maxDistance = 30 m` so the voice stays at full
  level across a normal room.
- **Capture loudness** (`Domain/Audio/AudioLevels.NormalizeLoudness`, pure + EditMode-tested):
  RMS-target makeup gain (default `targetRms = 0.22`) capped at `14×` to avoid blowing up the
  noise floor of a near-silent take, then a hard limiter at `0.98`. Applied in
  `VoiceRecorder.TryEndCapture` before WAV encoding. RMS targeting (perceived loudness) is what
  actually lifts quiet, dynamic speech — peak normalisation did not.
- **Tuning knobs** for on-device trimming: `targetRms` (capture level) and `minDistance` (full-volume
  zone). Documented in the source comments.

## Consequences

- **Positive:** narration is clearly audible with comfortable, controllable level; the voice still
  pans toward the demonstrator; the level math is pure C# in Domain and EditMode-tested; the
  decision is fully reversible (re-enabling HRTF is a few lines once its gain is configured).
- **Negative / accepted trade-offs:**
  - We lose true HRTF 3D localisation (elevation / front-back); only L/R + distance pan remains.
  - Sound *quality* is unchanged and remains **headset-hardware- and 16 kHz-mono-bound** — adequate
    for speech intelligibility, not hi-fi. Possible mild saturation at high headset volume is most
    likely the headset speakers driven hard (our hard limiter can also contribute on transients).
  - The hard limiter is crude; a soft limiter would be gentler on peaks.
- **Follow-ups (future investigation — not MVP-blocking):**
  - **Re-investigate HRTF**: re-enable the Meta XR Audio spatialiser *with* proper gain
    configuration (`MetaXRAudioSource` component / plugin attenuation settings) so we get both
    optimal spatialisation **and** adequate loudness. This is the main open audio question.
  - **Adaptive / per-user audio**: an in-experience volume control (and possibly per-environment
    presets) so the level adapts to different hearing needs and rooms — a settings-UI concern (V2).
  - **Soft limiting** (e.g. tanh soft-clip) to reduce any DSP-side saturation at high gain.
  - **Sound quality**: evaluate a higher capture sample rate and/or light noise reduction; confirm
    how much is genuinely headset-bound vs. improvable in-pipeline.

## Links

- Spec: [`docs/superpowers/specs/2026-06-29-voice-capture-replay-design.md`](../../superpowers/specs/2026-06-29-voice-capture-replay-design.md)
- Plan: [`docs/superpowers/plans/2026-06-29-voice-capture-replay.md`](../../superpowers/plans/2026-06-29-voice-capture-replay.md)
- Related: [ADR-0004](0004-domain-foundation-two-assembly-split.md) (the Domain/Foundation split
  the pure `AudioLevels` DSP and the device-layer `VoicePlayer`/`VoiceRecorder` sit either side of)
- PR #37 — the voice-capture+replay slice this decision lives in (device-verified on Quest 3)

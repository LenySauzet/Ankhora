---
name: voice-spatial-audio
description: Use when capturing the expert's voice during a masterclass recording, or replaying it as spatialised audio in Ankhora — microphone capture, timeline-aligned storage, and 3D playback via the Meta XR Audio spatialiser. Triggers: voice, microphone, record audio, narration, spatial audio, Meta XR Audio, spatialise, 3D sound, audio playback, mic permission, audio timeline.
---

# Voice & Spatial Audio (Meta XR Audio)

Voice is half of an Ankhora masterclass (voice + ghost hands). This skill covers capturing
the expert's narration during recording and replaying it as **spatialised** audio coming
from where the expert "was", in sync with the ghost hands.

> **Verify the API first.** Microphone capture uses Unity's `Microphone` API; spatialisation
> uses the **Meta XR Audio** plugin (`MetaXRAudioSource` / the Meta XR Audio spatialiser).
> Confirm the current component names and the spatialiser plugin setting via context7/Meta
> docs before coding — names differ across SDK versions.

## Capture (during recording)

- Request the mic permission: `android.permission.RECORD_AUDIO` (via `meta_update_android_manifest`).
- Record with Unity `Microphone.Start`; capture a timestamp at record-start so the audio
  track aligns with the pose/event timeline (see `record-replay-contract`).
- Store the clip alongside the masterclass data (compressed — Vorbis/AAC; raw PCM is heavy).
  Keep the **audio ↔ timeline offset** in the contract so replay stays in sync.

## Replay (spatialised)

- Set the project audio spatialiser to **Meta XR Audio** (Project Settings → Audio), and
  the AudioSource spatialBlend to 3D with the Meta spatial component.
- Position the audio source at the expert's recorded head/mouth pose each frame (sampled
  from the timeline) so the narration appears to come from the right place in the room.
- Drive playback from the same replay clock as the ghost hands — never two independent clocks.

## Keep it testable

The sync logic (audio offset, timeline → playback position) is plain C# math → EditMode-test
it (see `unity-testability`). Only `Microphone` / `AudioSource` calls touch Unity.

## Verify

- On device / simulator: record a short clip, replay, confirm voice is in sync with hands
  and spatially placed. Mac Editor can play audio but not hand tracking — verify sync on
  device and say so.
- `Unity_ReadConsole`: no audio/spatialiser errors; confirm the Meta spatialiser is the
  active plugin.

## Out of scope

Noise suppression, voice cloning, real-time TTS, multi-track mixing — V2. MVP = one narration
track per masterclass, captured and spatialised.

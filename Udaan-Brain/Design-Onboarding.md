# Udaan — Onboarding / Tutorial Design

_Spec, session 064. Rationale: 3D-flight onboarding is a proven friction point (market research: Sky Gamblers' tutorial called "overwhelming"). The goal is a short, in-context, playable first-run that teaches one thing at a time._

## Principles

1. **Teach by doing, not by reading.** Each step is gated by the player performing the action, not dismissing a text box.
2. **One concept per step.** Never introduce two mechanics at once.
3. **Always playable, never punishing.** Invulnerable during the tutorial; you cannot fail it, only progress.
4. **Short.** Target < 90 seconds to first real kill. Skippable for returning players (flag in save).
5. **Diegetic prompts.** Minimal HUD hints tied to the current step, using the CORE-marker style callouts, not modal popups.

## Flow (gated steps)

| # | Teaches | Gate (advance when…) | Hint |
|---|---------|----------------------|------|
| 1 | Throttle + hover | player climbs above X m and holds ~2s | "Throttle up — get off the ground" |
| 2 | Yaw/pitch + free-look | player rotates view to face a marker | "Look around — drag to aim your view" |
| 3 | Fire (primary) | player destroys a slow, non-shooting drone | "Line it up and fire" |
| 4 | Soft lock-on | player locks + destroys a drone while it drifts | "Lock on — let the assist help you aim" |
| 5 | Dash / dodge | player dashes through a gate/ring | "Dash to dodge" |
| 6 | First skirmish | clear 2 easy enemies (they shoot, low dmg) | "Now the real thing — clear them out" |

After step 6 → hand off to Mission 1 (Sky Sentinel) proper.

## Implementation notes

- A lightweight `TutorialDirector` (mirrors `MissionDirector`'s coroutine-per-step pattern) drives the gates; reuse `TargetHealth`, `CombatSpawner` (spawn non-shooting or low-damage dummies), and the objective/marker HUD.
- Player `SetProtected` for the whole tutorial (existing invuln path).
- Persist `tutorialDone` (PlayerPrefs) → offer Skip on subsequent launches.
- Each step exposes a completion predicate + a hint string; no hard-coded timers except the "hold to confirm."

## Success metrics (wire into telemetry #80)

- Tutorial completion rate, time-per-step (find the wall), time-to-first-kill, skip rate.
- If any step's median time spikes, that mechanic's teaching needs work.

## Non-goals

- No cinematic, no voiceover (deferred with audio). No forced camera takeovers — the player always has control.

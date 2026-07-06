# Udaan — Soft Lock-on: Utility-Score Targeting (spec)

_Session 064. Turns the current distance/centeredness sort in `TargetingSystem` into a tunable **utility score**, following the Pixonic/MY.GAMES auto-aim design (see `Market-Research-2026.md`). Auto-aim is the enabling tech for touch combat — the goal is a fair, legible, player-tunable lock._

## The score

For each candidate target, compute a utility in [0,1] and lock the highest:

```
Utility = wAim*Aim + wDist*Dist + wHP*HP + wCover*Cover + wThreat*Threat
          (then × Sticky bonus for the current target)
```

Each factor normalized 0..1:

| Factor | Meaning | Normalization |
|--------|---------|---------------|
| **Aim** | how centered in view / close to reticle | 1 at crosshair, 0 at `assistMaxAngle` |
| **Dist** | proximity | 1 near, 0 at `lockRange` |
| **HP** | prefer finishing wounded (or ignore) | 1 at low HP … configurable direction |
| **Cover** | line-of-sight clear? | 1 if LOS clear, low/zero if blocked (reuse the AI's LOS raycast) |
| **Threat** | is it aiming at / near the player or Core? | 1 for active threats |

**Sticky:** while firing, multiply the *current* target's utility by `stickyBonus` (e.g. 1.25) so the lock doesn't hop to a newer/closer enemy mid-burst. Critical for feel.

## Player-tunable priority presets

Expose weight sets the player can pick (research: players loved choosing their own priority; ship opt-in):

| Preset | Emphasis | Weights (Aim/Dist/HP/Cover/Threat) |
|--------|----------|-------------------------------------|
| **Nearest** (default) | closest in view | 0.4 / 0.4 / 0.0 / 0.2 / 0.0 |
| **Finisher** | low-HP first | 0.3 / 0.2 / 0.4 / 0.1 / 0.0 |
| **Centered** | whatever you're looking at | 0.6 / 0.2 / 0.0 / 0.2 / 0.0 |
| **Defender** | biggest threat to you/Core | 0.2 / 0.2 / 0.0 / 0.2 / 0.4 |

## Integration into `TargetingSystem`

- Replace the current `SortKey` (priority → angle → distance) with `UtilityScore(t)`; keep `targetPriority` as a small additive bias (class weighting: a healer/boss still gets a nudge).
- Add a `stickyBonus` applied to `CurrentTarget` while `firePrimary` is held.
- Reuse the LOS raycast from `EnemyDroneAI.HasLineOfSight` (extract to a shared helper) for the Cover factor.
- Add a `TargetPriorityPreset` enum + weights struct; expose in settings (#81). Manual cycle (`CycleTarget`) still overrides.
- Keep `assistStrength`/`assistMaxAngle` as-is (the aim *bend*); this spec is about target *selection*.

## Notes / guardrails

- Ship as an **opt-in** setting or a clearly-labeled default (research: forced control changes alienate; opt-in kept 80% of adopters).
- Keep it cheap: score only the already-filtered on-screen/in-range set (the registry list), once per scan (10 Hz), not per frame.
- Gameplay-readability colors (lock = yellow) unchanged.

## Success metrics (telemetry #80)

- Player accuracy / hit-rate, target-switch frequency, and preset usage distribution.

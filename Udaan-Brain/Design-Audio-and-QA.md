# Udaan — Audio Design + QA Plan (spec)

_Session 064. Two lightweight specs: the audio plan (currently procedural SFX; authored assets deferred) and a QA checklist for the mission slice._

---

## Part A — Audio design

**Current state:** procedurally synthesized SFX (bullet, explosion, hit) via `Sfx.cs`. No authored audio/music.

**Categories & intent:**

| Bus | Sounds | Notes |
|-----|--------|-------|
| **Weapons** | player bullet, rocket, enemy fire (quieter), reload, out-of-ammo | player fire louder/clearer than enemy to avoid spam fatigue (already partly done) |
| **Impacts** | hit marker, shield hit, kill pop, player-hurt, Core-hurt | distinct "your hit landed" cue is high-value feedback |
| **Drone / engine** | a subtle rotor loop that **rises in pitch/richness per player tier** (junk buzz → smooth hum → energy whine) — reinforces progression audibly | ties to the drone tier ladder |
| **World / ambient** | wind, distant birds (park), sunset calm bed | supports the Ghibli warmth |
| **UI** | lock-on chirp, capture progress tick, capture success, objective change, upgrade select | short, non-fatiguing |
| **Stingers / music** | mission start, wave clear, Defend tension bed, boss theme, victory/defeat | deferred (no assets); interim = procedural stingers |

**Mix guidelines:** enemy fire ducked under player fire; hit/kill cues sit on top; ambient low; one music bed at a time with smooth crossfade on phase change. Master limiter. Expose master/SFX/music volume in settings (#81).

**Staging:** keep procedural SFX as the interim; when authored assets arrive, swap per-category behind the same `Sfx` API so nothing else changes.

---

## Part B — QA checklist (mission slice)

Run before each build. Pair with the console scorecard + CSV telemetry (#80).

**Flow / beats**
- [ ] Intro stealth: player invuln for the launch window; hostiles spawn after.
- [ ] Waves: each clears → advances; per-wave scaling visibly ramps.
- [ ] Capture: outpost bubbles fill, orb turns blue, allies spawn; enemies can contest in later phases.
- [ ] Defend: CORE marker + beam visible; **continuous regen** holds vs 1 straggler, fails vs a push; retaliation peels attackers when shot.
- [ ] Upgrade: 1/2/3 (or gamepad) applies; effect real.
- [ ] Boss: spawns, buffed; killing it → Victory.
- [ ] Victory/Defeat scorecard prints with reason; **CSV row appended**.

**Edge cases (regression watch)**
- [ ] Core does NOT false-defeat at spawn (Configure fills HP).
- [ ] Respawn: safe, high, centered; lives decrement; out-of-lives → Defeat with reason.
- [ ] Outpost re-capture refreshes overshield without HP leak; expiry clamps HP.
- [ ] Enemies don't shoot through walls (LOS) and don't wedge on geometry (stuck-recovery).
- [ ] Radar: no crash on destroyed target; outpost blips + triangle self; terrain toggle (T).
- [ ] Difficulty presets: Recruit easier / Ace harder; lives adjust; no restart-stacking.

**Performance (mobile budget)**
- [ ] Frame time stable at peak drone count (~8–10) during Defend.
- [ ] No steady GC spikes (registry + NonAlloc should hold) — profile the Defend push.

**Devices**
- [ ] PC + gamepad (baseline), then a mid-range Android touch pass (control feel, thumb reach, readability).

**Sign-off:** a build passes when all Flow + Edge boxes are green and Defend holds a stable frame time at peak.

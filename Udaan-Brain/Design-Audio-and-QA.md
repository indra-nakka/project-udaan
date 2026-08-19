# Udaan — Audio Design + QA Plan (spec)

_Session 064. Two lightweight specs: the audio plan (currently procedural SFX; authored assets deferred) and a QA checklist for the mission slice._

---

## Part A — Audio design

**Current state (session-068):** procedural SFX via `Sfx.cs` **and procedural music via `Music.cs`**. Three code-synthesized looping BGM moods: **Menu** (calm major pentatonic), **Battle** (mid-tempo *melodic flight* theme — airy, pad-backed, not an arcade pump), **Boss** (SLOW, sparse, atmospheric minor with a sustained low drone + wide vibrato — tension over speed). Layers: bass + sustained pad + vibrato'd melody + optional drone. Lazily built + cached, crossfaded on one persistent 2D `AudioSource` at ~0.34 gain (under the SFX). Cued by `DemoFlow` (menu / play / results) and `MissionDirector.SpawnBoss`. Boss-laugh is still the procedural `Sfx.BossLaugh` placeholder. No authored/recorded audio yet — announcer VO is deferred (needs voice assets).

**How to tweak the music (no audio software needed):** open `Music.cs` — the top holds three `Song` objects (`MENU` / `BATTLE` / `BOSS`) with a full field guide in the class comment. The quick knobs:

| Want | Change |
|------|--------|
| Faster / slower | `bpm` |
| Different key / darker | `rootHz` (110=A2 dark, 130.81=C3, 146.83=D3…) |
| Happier / sadder | `minor` + `scale` (major-pentatonic vs natural-minor arrays) |
| A new tune | `melody` array — one scale-degree per beat, `-1` = rest; keep length = 4 × number of chords |
| Chord changes | `progression` (chord root per bar, semitones from key) |
| Warmer / atmospheric | `pad` (0 = off) and lower `decay` (sustained vs plucky) |
| More expressive | `vibrato` (0 = flat, ~0.2 = singing) |
| Softer/rounder vs brighter | `tri` (triangle) false = pure sine |

Edit, save, press Play (the mood rebuilds on next entry). If you want a mood to re-synth without restarting Unity, it's cached per session — a code recompile clears it.

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

# Udaan — Game Design Document

_**⭐ SINGLE SOURCE OF TRUTH** (session 065 pivot). If any other doc disagrees, this wins. Entry map: `UDAAN_ROUTER.md`. Current audience: **kids 5–10, learn + play.** Legacy PvP/MOBA docs are reference-only. Pairs with `Design-Garage-and-Learning.md`, `Design-Weapons-and-Tools.md`, `Art-Direction.md`._

---

## 1. Vision & pillars

> **AUDIENCE PIVOT (session 065):** Udaan is a **build-and-play drone game for children ages 5–10 — learn + play.** Kids build a real-ish quadcopter in a **garage** (learning STEM by *feeling* trade-offs), then fly **varied playful missions** with it. "Combat" is playful and non-violent (foam/water/bubble toy blasters; bots get gently powered-down, never destroyed). See `Design-Garage-and-Learning.md`, `Design-Weapons-and-Tools.md`, `Art-Direction.md`.

**Udaan** — a touch-first drone game where you **build** a quadcopter part by part and **play** with what you made. The garage teaches how drones really work (battery, props, frame, motors…) through visible, felt trade-offs; missions (delivery, rescue, races, photo, playful skirmish) let kids fly and improve. Tested on PC + gamepad, designed touch-first.

Design pillars:

1. **The garage is the teacher.** Every visible part is a real concept with a trade-off you see and feel — stealth STEM, no lectures. (This is the heart of the game.)
2. **The drone is the progress bar.** A realistic quadcopter rebuilt from junk (T0) to advanced (T5), part by part, in the garage.
3. **Playful & kind.** Non-violent toy weapons; no "death" — crashes are "oops, rebuild!" Warm, bright, encouraging, safe for little kids.
4. **Easy to fly, hard to put down.** Big tap controls, strong aim-assist, forgiving physics that still lets trade-offs be felt. Short sessions.
5. **Flight feels real-ish.** Believable quad flight and prop/body ratios sell the fantasy; assists scale so older kids can dial them down.
6. **Device-agnostic input.** One input abstraction drives player, AI, and allies identically (see §7).

Differentiation (see `Market-Research-2026.md`): **touch drone-building + play for young kids is unserved** — it combines an unserved 6DOF-touch niche with a genuine STEM-learning hook (the visible-component garage).

_Legacy note: the built "Sky Sentinel" combat slice (waves→capture→defend→boss) remains the working sandbox; it is being **re-skinned playful** and folded into the mixed-mission set, not thrown away._

---

## 2. Core loop

**Moment-to-moment:** fly → acquire/lock a target → maneuver into its arc → fire (bullets or rockets) → dodge/kite → manage ammo/health → repeat.

**Mission loop:** spawn → get bearings (launch stealth) → clear waves → capture outposts (gain allies + buffs) → defend the Core → pick an upgrade → beat the boss → victory/defeat → **run summary scorecard** → replay.

**Session target:** ~3 minutes per mission (industry-validated cadence for mobile).

---

## 3. The vertical slice — "Sky Sentinel"

A full playable mission arc, driven by `MissionDirector` (coroutine state machine). Phases:

| Phase | Objective | Key systems |
|-------|-----------|-------------|
| **Intro** | Get bearings; 3s launch stealth (untargetable + invulnerable) | spawn protection |
| **Wave** | Destroy escalating enemy waves | progressive per-wave scaling (HP/damage/fire-rate) |
| **Capture** | Fly into outpost bubbles and hold until they turn blue | contestable outposts, allies |
| **Defend** | Hold the Core against a push for ~35s | Core beacon + marker, self-repair loop, guardians |
| **Upgrade** | Pick one: Fire Rate / Damage / Armor | mid-mission power choice |
| **Boss** | Destroy the buffed boss drone | boss archetype |
| **Victory/Defeat** | Scorecard printed to console; press R to replay | `MissionStats` |

**Win:** survive all phases and kill the boss. **Lose:** run out of lives, or the Core is destroyed. Every run ends with a printed **scorecard** (result + reason, time, per-wave clear times, kills split you/allies, outposts captured, allies spawned/lost, damage dealt by you/allies, damage taken, lives).

---

## 4. Combat systems

**Flight** (`DroneFlightController`): thrust, yaw, pitch, strafe, altitude; bank/upright assist that fades near vertical so you can fly straight up without spin. Arena is a **sphere** boundary (acts as a dome — no separate ceiling; ~120m up at center).

**Aim & targeting** (`TargetingSystem` + `DroneWeapon`): free-look camera (right stick orbits view), **soft lock-on** with aim-assist that bends fire onto the locked target; target cycling; centered crosshair; off-screen target arrow. The player fires down the camera view; AI fires down the nose + assist. Two weapons: bullets (fast, low damage) and rockets (splash). Ammo + reload for the player; enemies have infinite ammo but slower fire.

**Factions** (`TargetHealth.team`): 1 = player + allies, 2 = enemy. Targeting and damage only apply across teams — **friendly fire is off** (you cannot damage your own Core or allies). Health scales the drone's visual size; death pops with VFX.

**Enemy AI** (`EnemyDroneAI`, "Tier-1"): acquires nearest cross-team target, holds an engagement band, orbit-strafes, fires only with **line-of-sight** (no shooting through walls), **steers around obstacles** (whisker sensors + stuck-recovery), **kites** at low HP (backs off but keeps firing), and **retaliates** — when shot, it breaks off its current target (e.g. the Core) and hunts the nearest attacker for a few seconds. AI is "just another input source," so it reuses the exact player flight/weapon stack.

**Allies:** team-1 AI drones (same brain), nerfed (slower fire, lower damage) so the player stays the main threat. Spawned by captured outposts and as Core guardians. They defend themselves (retaliation applies to them too).

---

## 5. Outposts (the "Precinct-Assault seed")

Contestable capture points fought over by **both** sides. A small beacon inside a translucent capture **bubble**. Whichever team is *alone* inside fills the capture bar; hold it and the outpost flips — the orb goes white (neutral) → blue (you) / red (enemy), glow on when owned. An owned outpost periodically spawns **that team's** drones (allies for you, hostiles for the enemy). Placed at random, spaced positions.

**Capture reward:** securing an outpost grants a **resupply** — full heal + a temporary **40% overshield for 30s** (reconciled so it stacks cleanly with the Armor upgrade).

Design intent: outposts turn the map into contested territory — the enemy can steal them and reinforce, so they matter in Defend/Boss, not just Capture.

---

## 6. The Defend beat (design deep-dive)

The hardest beat to make *legible* and *fair*. Current design:

- **Findability:** a cyan **CORE marker** (label + live HP%) tracks the Core on-screen and becomes an **edge arrow** when it's behind you; a tall **blue light-beam beacon** makes it visible map-wide.
- **Fair loop:** the Core **self-repairs** (45 HP/s) when no enemy is within 45m for ~2.5s. So *clearing attackers actively heals it* — the intended skill loop. Objective text calls the state ("UNDER ATTACK — kill the N near it" vs "safe — repairing").
- **Peel mechanic:** because enemies **retaliate** when shot, tagging a Core-diver pulls it off the Core onto you. Sustained fire keeps it peeled.
- **Support:** 2 allied guardians spawn to help; the reinforcement cap counts *all* live hostiles (incl. outpost-spawned) so it can't runaway-swarm.

Open balance question: because *every* hit peels an enemy, a skilled player can keep the whole push distracted. If Defend becomes trivial, shorten `retaliateDuration` or make only a fraction of enemies retaliate so some always pressure the Core.

---

## 7. Input architecture (device-agnostic)

The keystone. `IFlightInput` interface → `FlightInputRouter` merges sources. **AI is just another `IFlightInput` source**, so enemies and allies reuse the same `DroneFlightController` + `DroneWeapon` the player uses. This is why a single AI brain drives both factions and why touch/gamepad/keyboard all work through one path. Preserve this invariant.

---

## 8. Controls (current)

- **Player:** touch (virtual sticks + fire buttons + HUD), gamepad, and keyboard/mouse-free PC. Free-look camera on the right stick; fire buttons for the two weapons; dash/juke; target cycle; restart (R).
- **HUD:** reticle + lock marker, off-screen arrow, heading-up **tactical radar** (allies blue / enemies red / lock yellow / outposts white-blue-red squares, self = upward triangle, optional terrain toggle on **T**), artificial horizon, flight pip, speed/altitude, HP bar, ammo, hit flash/marker, objective text, CORE marker.

Direction (from market research): add a **tunable utility-score** to the soft lock (distance × HP × cover × reticle-proximity × sticky-fire), let players customize priority weights, and add an **optional, unfiltered gyro** fine-aim layer. Ship control changes as opt-in settings.

---

## 9. Progression & economy (future)

Currently a single replayable mission with a mid-mission upgrade pick. Direction:

- **Run-based structure** (roguelike-adjacent, cf. Firehawk FPV): power grows within/across runs.
- **Upgrade tree** for weapons/drone stats — grindable, with cosmetics and *convenience* (not power) as the monetization surface.
- **Monetization when the time comes:** cosmetics + generous battle pass + rewarded video; sell convenience, never raw power; disclose odds. (See `Market-Research-2026.md`; NGO/multiplayer is currently parked.)
- **Deferred:** difficulty settings (data-driven presets), authored audio/music, on-device touch polish, the Traverse intro beat.

---

## 10. Art & audio (current = greybox)

- **Visuals:** procedural primitives — a children's-park greybox map (trees, slides, gyms, climbing towers, maze walls) for cover and verticality; code-built VFX (explosions, sparks, tracers, camera shake). Playful palette. Art comes later on the same layout.
- **Audio:** procedurally synthesized SFX (bullet/explosion/hit). Authored audio + music deferred (no asset files yet).

---

## 11. Technical state

Unity 6000.4.3f1, URP, mobile target. NGO (Netcode) present but multiplayer **parked** — everything runs single-player through offline fast-paths. Performance was refactored in session 063 (static target registry + NonAlloc physics) to remove per-frame full-scene scans. See `architecture/Performance-and-Architecture-Audit.md` for the open backlog (the parked-netcode cruft and the offline scrap-drop gap are the notable items).

---

## 12. Open questions / decisions

1. **Defend balance** — does universal retaliation make Defend trivial? (tune `retaliateDuration` / partial retaliation)
2. **Scrap drops** — the piñata drop is dead in single-player (MP-only guard). Add an offline path or cut it?
3. **Difficulty** — data-driven presets (deferred). The new balance model (`Balance-Model.xlsx`) is the tuning surface.
4. **Campaign shape** — how many missions, what new beats? (see `Enemy-Roster-and-Campaign.md`)
5. **Multiplayer** — keep parked, or is it a design goal? (netcode is existential if so.)

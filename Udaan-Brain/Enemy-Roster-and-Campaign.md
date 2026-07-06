# Udaan — Enemy Roster & Campaign Design

_Design doc, session 063. Everything here is spec'd to implement on the existing systems: `EnemyDroneAI` (input-source AI), `TargetHealth` (factions/HP), `CombatSpawner` (factory), `MissionDirector` (phase machine). Nothing here is built yet — it's the ready-to-build backlog._

---

## Part A — Enemy roster

Today there's one archetype: **Tier-1 drone** (engage-band orbiter with LOS firing, obstacle avoidance, kite, retaliation) plus a **Boss** (a buffed Tier-1). The roster below adds variety so encounters pose *different questions*, not just bigger numbers. Each is expressed as a tweak to the existing AI + weapon, so they reuse the whole flight/weapon stack.

Design rule: an archetype should force a **distinct player response**. Avoid stat-only variants.

### Tier-2 archetypes

**1. Interceptor (Rusher)**
- Fast, low HP, closes to melee-range and brawls. High turn gain, small engage band, dashes.
- *Player response:* stop orbiting, use burst + dodge; punishes turret-camping.
- *Impl:* `engageMin/Max` small, `turnGain` high, speed up, HP down; optional contact damage.

**2. Sniper (Marksman)**
- Long range, slow fire, high per-shot damage, keeps distance and kites constantly. Telegraphed charge before firing.
- *Player response:* break line-of-sight (the map's cover finally matters), close the gap.
- *Impl:* `weaponRange` large, `fireInterval` long, `bulletDamage` high, engage band far; add a pre-shot charge tell (VFX + delay).

**3. Shielded (Bulwark)**
- Front-facing shield: takes reduced damage from the front, full damage from behind/flank. Slow, tanky.
- *Player response:* flank it — rewards 3D maneuvering.
- *Impl:* directional damage multiplier in `TargetHealth.TakeDamage` using hit direction vs facing (needs attacker position — the `lastAttacker` plumbing groundwork exists).

**4. Kamikaze (Diver)**
- No gun; charges the player/Core and detonates (AoE). Fragile, fast, beeps/glows before blowing.
- *Player response:* shoot it down early or juke the dive; a real threat to the Core in Defend.
- *Impl:* no weapon; steer straight at target; on proximity → `Vfx.Explode` + area damage; strong Core-diver in Defend.

### Tier-3 / support archetypes

**5. Healer/Repair drone**
- Doesn't attack; orbits and heals nearby enemies. Priority kill target.
- *Player response:* target-priority decision — kill the healer first.
- *Impl:* emits a heal tick to nearby team-2 `TargetHealth`; low HP; high target-priority so the lock favors it.

**6. Turret (static)**
- Ground/wall-mounted, no flight, 360° fire, high HP, area-denial. Uses the same weapon, no `DroneFlightController`.
- *Player response:* use cover, pop and shoot.
- *Impl:* `TargetHealth` + `DroneWeapon` on a static object (no flight controller — note this also excludes it from `NearestCombatant`, which is correct; it's not a "fighter" for retaliation targeting, so give it its own targeting).

**7. Swarm (Minion cloud)**
- Many tiny, weak, cheap drones spawned in clusters. Individually trivial, dangerous en masse.
- *Player response:* rockets (splash) become the answer — teaches weapon-switching.
- *Impl:* small scale, low HP, cheap spawn; lean on rocket splash.

### Boss direction

Current boss = buffed Tier-1. Future bosses should be **multi-phase** and combine archetypes:
- **Phase gates by HP%:** e.g. 100–66% shielded (flank it), 66–33% summons swarms (splash them), <33% enraged rusher (dodge + burst).
- **Weak points / adds:** destroy healer-adds or shield-generators to open a damage window.
- One memorable mechanic per boss beats raw stat inflation.

### Roster tuning table (starting points — validate in the balance model)

| Archetype | HP (× Tier-1) | Damage | Fire rate | Speed | Range | Special |
|-----------|---------------|--------|-----------|-------|-------|---------|
| Tier-1 | 1.0 | 14 | 0.5s | 1.0 | 70 | baseline |
| Interceptor | 0.6 | 10 | 0.4s | 1.5 | 30 | dash, contact dmg |
| Sniper | 0.8 | 34 | 1.6s | 0.9 | 110 | charge tell |
| Bulwark | 2.2 | 16 | 0.6s | 0.7 | 55 | front DR |
| Kamikaze | 0.4 | — | — | 1.6 | — | AoE detonate |
| Healer | 0.7 | — | — | 0.9 | — | heal aura |
| Turret | 3.0 | 20 | 0.5s | 0 | 80 | static, 360° |
| Swarm unit | 0.2 | 6 | 0.7s | 1.2 | 40 | spawned in clusters |

---

## Part B — Campaign design

Sky Sentinel is mission 1 (the vertical slice). A campaign strings missions with a rising difficulty curve, introducing **one new idea per mission** and reusing the phase machine.

### Structure

- **Run-based** (cf. Firehawk FPV): each mission is a ~3-minute run; power/upgrades carry within a run; failure returns you to a hub/restart.
- **Difficulty curve:** each mission raises `perWave*` scaling and introduces a new archetype or beat, so the player is always learning one new thing.

### Mission ladder (proposed)

| # | Name | New idea introduced | Beats |
|---|------|---------------------|-------|
| 1 | **Sky Sentinel** (built) | The full loop | Intro → Waves → Capture → Defend → Upgrade → Boss |
| 2 | **First Light** (Traverse — the missing beat) | **Traverse**: fly a route through the maze to a target under fire | Intro → Traverse → Waves → mini-boss |
| 3 | **Crossfire** | **Sniper + cover play** — LOS matters | Waves(sniper) → Capture → Defend |
| 4 | **The Anvil** | **Bulwark + flanking**, Kamikaze Core-divers | Capture → Defend(kamikaze) → Boss(shielded) |
| 5 | **Hydra** | **Healer priority + Swarm** (rockets) | Waves(swarm) → Escort → Boss(multi-phase w/ healer adds) |
| 6 | **Overlord** | Everything; territory war over many outposts | Contested outposts → Defend → multi-phase Boss |

### New beats to build (beyond what exists)

- **Traverse** (highest priority — the one missing demo beat): a waypoint route through the map the player must fly, with light resistance. Reuses the CORE-marker system for waypoints and the arena/map. Small `MissionDirector` phase.
- **Escort**: protect a moving allied unit from A to B (moving version of Defend; reuse the CORE marker + self-repair concepts on a mover).
- **Territory war**: many outposts, win by holding a majority for a timer (the outpost system already supports contest; add a majority-hold win condition).

### Difficulty presets (deferred feature, spec)

Data-driven multipliers over the existing knobs — no new systems:

| Preset | Enemy HP × | Enemy dmg × | Player dmg × | Core HP × | Lives |
|--------|-----------|-------------|--------------|-----------|-------|
| Recruit | 0.8 | 0.7 | 1.2 | 1.3 | 4 |
| Pilot (default) | 1.0 | 1.0 | 1.0 | 1.0 | 3 |
| Ace | 1.3 | 1.4 | 0.9 | 0.8 | 2 |

Implement as a `DifficultyProfile` struct read by `MissionDirector`/`CombatSpawner` at run start. Validate against the balance model.

---

## Build order recommendation

1. **Traverse beat** (completes the six demo beats; small, low-risk).
2. **Two contrasting Tier-2 enemies** — **Sniper** (makes cover matter) and **Kamikaze** (Core threat in Defend). Biggest variety-per-effort.
3. **Directional-damage Bulwark** (exercises 3D flanking — Udaan's signature).
4. **Multi-phase boss** framework.
5. **Difficulty presets** (data-only, ties to the balance model).

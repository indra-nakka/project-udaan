# Udaan — Architecture & Performance Audit

_Session 063 · Unity 6000.4.3f1 · URP · single-player (NGO parked)_

This is a full read of `udaan-client/Assets/Scripts` (34 files). It catalogues performance hotspots, GC pressure, coupling, the parked multiplayer code, and a prioritized fix list. **Items marked ✅ RESOLVED were fixed in the session-063 perf refactor; the rest are open.**

Scene scale assumed (from `SinglePlayerBootstrap` + `MissionDirector`): 1 player, 3–5 wave enemies, 2 core guardians, 3 outposts (up to 2 units each), 1 boss — peak ~8–10 live drones during Defend. Every drone runs its own `EnemyDroneAI`, `TargetingSystem`, `DroneWeapon`, `HealthBar`; the player also runs `TouchFlightHUD`.

---

## 1. Performance hotspots

### 1a. Full-scene `FindObjectsByType<TargetHealth>` scans — WAS the dominant cost ✅ RESOLVED

The scene was being re-scanned from scratch (each call allocates a fresh array) many times per second:

| Site | Method | Old frequency |
|------|--------|---------------|
| `TargetingSystem.RefreshOnScreen` | 10 Hz × every drone → **~90 scans/sec** | worst |
| `TouchFlightHUD.UpdateRadar` | 10 Hz × player | |
| `EnemyDroneAI.NearestCombatant` | per damage event + per-frame while retaliating | |
| `MissionDirector.EnemiesNear` | **every frame during Defend** | |
| `MissionDirector.CountLiveEnemies` | every frame during Defend | |
| `TouchFlightHUD.DrawOutposts` | **every frame** (`FindObjectsByType<Outpost>`) | |

**Fix applied:** a self-maintained static registry — `TargetHealth.All` and `Outpost.All`, populated in `OnEnable`, removed in `OnDisable`. All six sites now iterate a ~10-element list with zero allocation. This removed ~180 allocating full-scene scans/sec. Query-site filters (team / alive / range / targetable) were preserved.

Remaining cold `Find*` calls are fine as-is: `ClearAll` (once per run), `CacheTerrainPoints` (one-time), `_hud`/`_race` lookups (cached after first hit).

### 1b. Physics queries in hot paths — partially resolved

| Site | Query | Status |
|------|-------|--------|
| `EnemyDroneAI.Clearance` (3× per frame per drone) | `RaycastAll` → **`RaycastNonAlloc`** | ✅ RESOLVED (shared 24-slot buffer) |
| `EnemyDroneAI.HasLineOfSight` | `RaycastAll` → **`RaycastNonAlloc`** | ✅ RESOLVED |
| `EnemyDroneAI.TouchingObstacle` | `OverlapSphere` → **`OverlapSphereNonAlloc`** | ✅ RESOLVED |
| `Outpost.SoloTeamInside` (per frame × 3 outposts) | `OverlapSphere` → **`OverlapSphereNonAlloc`** | ✅ RESOLVED |
| `Projectile` splash | `OverlapSphere` on rocket impact | OPEN (low priority — per-death, not per-frame) |
| `DroneFlightController` hover ray | single `Raycast` in FixedUpdate | fine (no alloc) |

Note on NonAlloc buffers: they silently truncate to capacity. Buffers were sized to 24 to tolerate the now-denser map; if drones cluster tightly in a corridor a raycast could still exceed 24 hits and mis-read clearance. Watch for it; bump if needed.

---

## 2. GC pressure (per-frame allocations) — mostly resolved

- **`FindObjectsByType` / `RaycastAll` / `OverlapSphere` arrays** — the largest steady GC source. ✅ Removed by the registry + NonAlloc work above.
- **`TargetingSystem` sort closure** — `_onScreen.Sort((a,b)=>…)` allocates a delegate every 10 Hz scan, and the comparator calls `GetComponent<TargetHealth>` for every candidate on every comparison (an O(n log n) `GetComponent` storm). OPEN — P1. Hoist the key function and cache each candidate's priority once.
- **HUD objective strings** — the mission objective loops run under `yield return null` (every frame) and rebuilt interpolated strings each frame. ✅ Partially resolved: `MissionDirector.Objective` now de-dupes unchanged text so it no longer re-sets `Text.text` (which forces a canvas rebuild) each frame. The interpolation itself still runs; caching per-value would remove it entirely (minor, OPEN).
- **`TouchFlightHUD` stat/ammo strings** — `_statText.text` and the ammo string rebuild every frame. OPEN — P2. Only assign on change.
- **Per-instance materials** — `CombatSpawner.Tint`, `ParkMapGenerator.Prim`, `Outpost`, `SpawnCore`, `HealthBar` all `new Material(...)` per object. Spawn-time only (not per-frame) but defeats batching and leaks material instances. OPEN — P2. Use shared materials + `MaterialPropertyBlock`.

---

## 3. Architecture & coupling

**Static singletons / global state**
- `MissionStats.Active` — static mutable, written by `MissionDirector` and by `CombatSpawner.Ally`. Couples the "factory" to run-scoped global state; blocks tests/concurrent runs. OPEN — P2 (inject a reference or fire an `OnAllySpawned` event).
- `ArenaBounds.Enabled/Center/Radius`, `RaceManager.FlightLocked` — static flags read across flight/weapon systems. Convenient but global.

**Static events (the clean part of the design)**
- `TargetHealth.OnAnyDamaged`, `TargetHealth.OnDeath`, `Outpost.OnCaptured` — sensible pub/sub, subscribed by `MissionDirector`, `EnemyDroneAI`, `TouchFlightHUD`. One note: `OnAnyDamaged` fires to every `EnemyDroneAI` on every hit (each early-outs on `victim != _health`); cheap per call, but the retaliation path then does work — kept light now that `NearestCombatant` is registry-backed.

**Cross-object reach-throughs**
- `MissionDirector` mutates `player.GetComponent<DroneWeapon>()` fields directly (upgrades, enemy tuning) and does in-place `player.maxHealth` arithmetic for the overshield (strip-then-reapply, mirrored in `Update`). Fragile but currently correct.
- `EnemyDroneAI.Awake` configures sibling components by reaching into their public fields. Configuration-by-reach-through rather than data.

**Parked NGO / Netcode multiplayer code (inactive in single-player)**
- `TargetHealth : NetworkBehaviour`, gating mutation behind `HasDamageAuthority => !IsSpawned || IsServer`. Offline everything runs via the `!IsSpawned` fast-path, but it drags in dead branches:
  - **The piñata scrap-drop (`Pop()`) never runs offline** — guarded by `NetworkManager.Singleton != null && IsServer`. If scrap drops are expected in single-player, this is a silent gameplay gap. **Decision needed** (P1): add an offline drop path or document it as MP-only.
  - `NetworkObject.Despawn()` / server-relocate branches are dead offline.
- `DroneWeapon`, `DroneFlightController`, `CameraController`, `PlayerEconomy`, `PlayerHUDController`, `ScrapItem`, `DroneClassSpawner` are all `NetworkBehaviour`. The whole `DroneClassSpawner → ClassSelectionUI → PlayerEconomy` chain is unused by the single-player bootstrap.
- `SinglePlayerBootstrap.SpawnTargets` bolts a `NetworkObject` onto spawns "to keep TargetHealth happy" — a smell that the netcode base leaks into offline setup.

**Dead / unused code**
- `SinglePlayerBootstrap.SpawnTargets()` — never called (dummies removed). OPEN — P2, delete or gate.
- `TargetHealth` piñata-drop — dead offline (above).

**TODO/HACK comments:** none (the code uses ticket-style tags like `ECON-02`).

---

## 4. Testability

**Pure logic that could be unit-tested today** (extract to plain classes so the user's NUnit EditMode tests can cover them):
- `MissionDirector.SpawnWaveTuned` scaling math (hp/dmg/fire formulas) → a `WaveTuning` struct.
- The overshield reconciliation math → a pure helper.
- `TargetingSystem.AimDir` and the priority sort key → pure given inputs.
- `TouchFlightHUD.PointInTri` / `Cross` → trivially testable geometry.

**Highest-value testability refactor:** split a plain `HealthModel` (currentHealth, maxHealth, TakeDamage, HealthFraction) out of `TargetHealth`, leaving the `NetworkBehaviour` as a thin wrapper. Makes combat math unit-testable and shrinks the offline netcode surface. OPEN — P2.

---

## 5. Prioritized backlog (post-refactor)

**P0 — done this session ✅**
1. Static target/outpost registry replacing hot `FindObjectsByType` scans.
2. NonAlloc AI physics + Outpost capture check.
3. Objective HUD text de-dupe.

**P1 — next**
4. `TargetingSystem` sort: hoist the comparator closure and cache per-candidate priority (kills an O(n log n) `GetComponent` storm 10 Hz/drone).
5. Decide the piñata scrap-drop's offline fate (silent gap today).
6. Stop rebuilding unchanged `_statText`/ammo HUD strings every frame.

**P2 — cleanup**
7. Extract `HealthModel` from `TargetHealth` for testability + smaller netcode surface.
8. Delete/gate dead `SpawnTargets()`.
9. Break the `CombatSpawner → MissionStats.Active` global coupling.
10. Shared materials + `MaterialPropertyBlock` in the procedural builders (draw-call/memory win on mobile).

---

## Appendix — mobile budget context

Target is mobile; the frame budget is tight and the GC is the enemy. The registry + NonAlloc work removed the two allocation sources that scaled with drone count (full-scene scans and physics-query arrays), which were the mobile-critical ones. The remaining open items are smaller steady allocations (sort closure, HUD strings) and one-time spawn costs (materials). None are blocking; they're the polish pass before a device performance test.

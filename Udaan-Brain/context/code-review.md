---
type: review
status: living
updated: 2026-06-30
tags: [code, tech-debt, review]
---

# 🔎 Code Review — udaan-client/Assets/Scripts

**Role:** Living review of the C# codebase. Findings are prioritized; check items off as fixed and log the fix in [[changelog]]. Snapshot date 2026-06-30 (12 scripts, sandbox stage). Confirmed footguns also live in [[gotchas]]; structural rules in [[invariants]].

## 🟥 Critical (fix before/at M2–M3)

- [ ] **Projectiles are not networked → client weapons deal no damage.** `DroneWeapon.Shoot()` uses `Instantiate(dartPrefab…)` locally (no `NetworkObject.Spawn()`), and `DartCollision` calls `TargetHealth.TakeDamage()`, which is gated by `if (!IsServer) return;`. Result: only the **host's** shots register; a client's darts are local-only visuals that no-op on damage, and remote players never see each other's darts. **This means multiplayer combat is effectively broken today.** Fix = server-authoritative shooting: client sends a fire **ServerRpc** (or input), server spawns/raycasts and applies damage. This is exactly the kind of thing the netcode choice (DEC-003) should make easy — decide that first.
- [ ] **Flight input is desktop-only (`Input.GetAxis`) → nothing works on touch.** `DroneFlightController.HandleFlightMovement()` reads `Xbox_RT`/`Vertical`/`Horizontal`/`TargetYaw`/`TargetPitch`. The product is mobile touch (M1). This whole input path needs a touch layer. Confirms the M1 risk in [[📋 Master Plan]].

## 🟧 High (architectural debt that compounds)

- [ ] **`try { Input.GetAxis(...) } catch {}` ×5 every FixedUpdate.** Swallowing exceptions per physics tick is a GC/perf smell and hides missing axis config. Define the axes once (Input Manager / new Input System) and remove the try/catch. (`DroneFlightController`)
- [ ] **Movement is client-authoritative with zero validation** → trivially cheatable (speed/teleport). Accepted for feel per [[DEC-001_Netcode_NGO]], but for competitive PvP the server must at least sanity-check movement. Tie-in to DEC-003.
- [ ] **No object pooling.** `DroneWeapon` and `TargetHealth.Pop()` `Instantiate`/`Destroy` per shot/drop. Violates the pooling rule in [[invariants]]; will hitch/crash on mobile. Build a pooled projectile/VFX system when combat is reworked.
- [ ] **`CameraController` reparents `Camera.main` to the drone and never un-parents.** On death/despawn the main camera is destroyed with the drone. Add cleanup in `OnNetworkDespawn`/`OnDestroy`, or use a non-parented follow camera.
- [ ] **Euler-based pitch/bank** in `HandleFlightMovement` reads/writes `transform.eulerAngles` each frame → gimbal flips at steep angles for a 6-DoF flyer. Prefer quaternion-relative rotation; revisit during the M1 flight-feel pass.

## 🟨 Medium (cleanup / scalability)

- [ ] **Magic numbers not data-driven:** scrap value `+10` hardcoded in `PlayerEconomy`; spawn ranges, forces, fire rate, dart damage scattered in code. Move to ScriptableObjects/config (the project's own pattern — see [[conventions]]). Keep [[💰 Economy_Balance]] as the source of truth.
- [ ] **`ClassSelectionUI` hardcodes Striker/Bulwark buttons.** Generate the class list from `DroneClassSpawner.availableClasses` so adding a class doesn't mean editing UI code.
- [ ] **`DroneClassData` only carries health/thrust/drag.** The class identity (weapon loadout, ability, mass, hitbox) will need fields here as the hero-class layer lands.
- [ ] **No namespace.** All scripts are global. Add `namespace Udaan.*` before the codebase grows — cheap now, painful later.
- [ ] **`TargetHealth` shrinks `localScale` on hit (server only)** — won't replicate to clients unless scale is synced; fine for a dummy, but don't rely on this pattern for real synced feedback.

## 🟩 Good (keep doing this)
- Consistent **defensive null-guards** with clear `Debug.LogWarning/Error` fallbacks.
- Correct **authority guards** where they exist (`if (!IsOwner) return;`, `if (!IsServer) return;`), and proper `NetworkObject.Spawn()` for scrap drops in `TargetHealth.Pop()`.
- Clean **event hook lifecycle** (`OnSpeedModifierUpgraded` subscribe in `OnNetworkSpawn`, unsubscribe in `OnNetworkDespawn`).
- **ScriptableObject-driven** class/upgrade data — the right scalable pattern.

## Suggested sequencing
1. Decide the network stack (**DEC-003**) — it dictates how projectiles/combat are rebuilt.
2. M1 touch input layer (replaces `Input.GetAxis`).
3. M2 combat rebuild on the chosen stack: server-authoritative, pooled projectiles, data-driven weapons.
4. Add a namespace + a small config pass while the surface area is still small.

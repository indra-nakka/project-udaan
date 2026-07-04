---
type: architecture
status: active
updated: 2026-06-30
tags: [architecture, system-map, read-first]
---

# Project Udaan: System Architecture

**Role:** 1-page system map — read first every session. Rules that must hold: [[invariants]]. Patterns: [[Unity_NGO_Patterns]]. Variables: [[glossary]].

## 📡 Networking & Authority Layer
- **Framework:** Unity Netcode for GameObjects (NGO). Rationale: [[DEC-001_Netcode_NGO]].
- **Hybrid authority:**
  - **Movement → client-authoritative** via `Network Transform` / `Network Rigidbody` (Authority = **Owner**) for responsive feel.
  - **Gameplay state → server-authoritative** (currency, health, damage, upgrades). Mutated only on the server (`if (!IsServer) return;`), replicated via `NetworkVariable<T>` (e.g. `PlayerEconomy.scrapCount`).
- **Spawn Handling:** Random coordinate allocation inside `OnNetworkSpawn` to prevent overlapping on scene entry. Networked objects use `Spawn()`/`Despawn()`, never `Instantiate`/`Destroy`.

## 🎥 Camera Pipeline
- **Rig:** Dynamic Camera Controller targeting empty child mounts on the player object (`FPV_Mount` [25° up-tilt] and `TPV_Mount`).
- **Network Sync:** Script targets `Camera.main` on local launch and establishes ownership using `if (!IsOwner) return;`.

## 🔫 Combat & Physics
- **Projectiles:** Spongy foam darts instantiated at runtime, inheriting parent velocity vectors with an added 90-degree relative X-axis calculation (`Quaternion.Euler(90f, 0f, 0f)`) to match cylinder forward-facing vectors.
- **Bounciness:** Dedicated Physic Material (`BouncyFoam`) handling deflection angles.

# 🧠 Unity NGO Patterns

**Role:** Netcode-for-GameObjects conventions for this project. Authority model is fixed in [[invariants]] and [[DEC-001_Netcode_NGO]]; common traps in [[gotchas]].

## Lifecycle
- Initialize synced data in **`OnNetworkSpawn`**, not `Awake`/`Start` (network identity isn't ready in Awake).
- Spawn/despawn networked objects with **`NetworkObject.Spawn()` / `.Despawn()`** — never plain `Instantiate`/`Destroy`.

## Authority
- **Movement: client-authoritative** (NetworkTransform/Rigidbody, Owner) for feel.
- **Gameplay state: server-authoritative.** Mutate currency/health/damage/upgrades only on the server; guard with **`if (!IsServer) return;`** (see `PlayerEconomy.cs`).
- Owner-only logic (camera, local input) guards with **`if (!IsOwner) return;`**.

## Replication
- Synced state via **`NetworkVariable<T>`** (server writes, clients read). Register every one in [[glossary]].
- **ServerRpc** for client→server requests that affect others (damage, class selection, purchases).
- **ClientRpc** for server→client effects (VFX, announcements).
- Keep RPC payloads small; prefer value types and quantized data.

## Watch-outs (→ [[gotchas]])
- Despawn vs Destroy desync · uninitialized values when set in Awake · missing `.Spawn()` on new objects.

## Open question
- For fast 6-DoF PvP, client-auth + NGO may not be enough (rubber-banding, cheating). Evaluate **client-side prediction/reconciliation** or **Photon Fusion/Quantum** before M3 — tracked in [[assumptions]] and [[UDAAN_MASTER_PROJECT_PLAN]] Phase 2.

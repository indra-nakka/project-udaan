# 🪤 Gotchas — Known Footguns

**Role:** Things that *look* fine but silently break — engine quirks, netcode traps, and mistakes already made on this project. Read before debugging; add to it whenever something surprises you. (Rules that must hold live in [[invariants]]; this file is the "here's how you'll get bitten" companion.)

> Format: **Symptom → Cause → Fix.** Keep entries short. Cite `file:line` where useful.

## 🛰️ Netcode (NGO)
- **Object disappears for the host but lingers on clients (or vice-versa)** → used `Destroy()` on a networked object → call `NetworkObject.Despawn()` on the server instead.
- **"Scrap" / currency desyncs or double-counts** → mutated state on a client → wrap in `if (!IsServer) return;`. Currency is server-authoritative (`PlayerEconomy.cs`).
- **Newly spawned object isn't visible to other players** → forgot `.Spawn()` after `Instantiate` → spawn it on the server.
- **A client's shots do no damage; remote players don't see each other's projectiles** → projectiles are `Instantiate`'d locally (not networked) while damage is server-gated (`if(!IsServer) return;`) → only the host's shots register. Fix: server-authoritative shooting via ServerRpc + `NetworkObject.Spawn()`. (Current `DroneWeapon`/`DartCollision` — see [[code-review]] critical.)
- **Values not initialized on join** → set them in `OnNetworkSpawn`, not `Awake`/`Start` (network identity isn't ready in Awake).

## 🎮 Input
- **`Input.GetAxis` throws / InvalidOperationException on Unity 6** → New Input System is the sole handler → set **Active Input Handling = Both** (see [[DEC-002_Classic_Input_Manager]]).
- **Custom axes (`Xbox_RT`, `TargetYaw`, `TargetPitch`) return 0** → axis not defined in the legacy Input Manager → add it there; cross-check names against [[glossary]] Input Axis Registry.

## 🛸 Flight / Physics
- **Drone drifts forever / feels like ice** → drag too low or hover override stuck on → check `rb.linearDamping` (from Class Data) and `isManuallyOverridingHover` reset.
- **Projectile fires sideways** → cylinder forward ≠ transform forward → apply `Quaternion.Euler(90f, 0f, 0f)` relative rotation (see [[🏗️ Architecture]] Combat).
- **Class stats ignored at runtime** → `DroneClassData` not assigned → code falls back to hardcoded inspector presets (see `DroneFlightController.Awake` warning log).

## 📱 Mobile
- **Frame drops / crashes under load** → per-shot `Instantiate`/`Destroy` → object pooling is mandatory ([[invariants]], [[Mobile_Optimization]]).
- **Fine in Editor, dies on device** → always profile on real mid-range hardware; the Editor lies about thermal/memory.

## 🗃️ Repo
- **Bloated repo / broken binaries** → 3D/audio/texture assets must go through **Git LFS** (configured in `.gitattributes`).

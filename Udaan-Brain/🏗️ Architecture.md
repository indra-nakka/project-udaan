# Project Udaan: System Architecture

## 📡 Networking & Authority Layer
- **Framework:** Unity Netcode for GameObjects (NGO)
- **Authority Mode:** Client-Authoritative via `Network Transform` and `Network Rigidbody` (Authority Mode set to **Owner**).
- **Spawn Handling:** Random coordinate allocation inside `OnNetworkSpawn` to prevent overlapping on scene entry.

## 🎥 Camera Pipeline
- **Rig:** Dynamic Camera Controller targeting empty child mounts on the player object (`FPV_Mount` [25° up-tilt] and `TPV_Mount`).
- **Network Sync:** Script targets `Camera.main` on local launch and establishes ownership using `if (!IsOwner) return;`.

## 🔫 Combat & Physics
- **Projectiles:** Spongy foam darts instantiated at runtime, inheriting parent velocity vectors with an added 90-degree relative X-axis calculation (`Quaternion.Euler(90f, 0f, 0f)`) to match cylinder forward-facing vectors.
- **Bounciness:** Dedicated Physic Material (`BouncyFoam`) handling deflection angles.

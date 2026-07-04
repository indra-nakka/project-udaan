# Project Udaan: Data & Variable Glossary

## 🛸 Flight & Physics Variables
| Variable Name | Type | Data Scope | Ownership / Authority | System Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `forwardThrust` | `float` | Local Cache | Client (Owner) | Determines raw force added along `transform.forward` during forward acceleration passes. Inherited from Class Card data. |
| `isManuallyOverridingHover` | `bool` | Physics State | Client (Owner) | Flags true when player pushes thrust/altitude axes, instantly dampening the automatic hover height constraints to free up flight control. |
| `drag` | `float` | Rigidbody State | Client (Owner) | Unity physics drag value applied to simulate air resistance, preventing infinite sliding. Inherited from Class Card data. |
| `activeSpeedModifier` | `float` | Local Cache | Client (Owner) | A runtime multiplier (default `1.0f`) that scales `forwardThrust` when mid-game upgrades are consumed. |
| `thrustMultiplier` | `float` | Local Cache | Client (Owner) | M1 tuning (default `3f`): scales forward/strafe forces on top of class data to raise top speed. New serialized field → prefab picks up the code default. |
| `reverseScale` | `float` | Local Cache | Client (Owner) | Reverse force as a fraction of forward (default `0.6f`), applied when `thrust` < 0. |
| `steeringMultiplier` | `float` | Local Cache | Client (Owner) | Scales `turnSpeed`/`pitchSpeed` (default `1.5f`) so gates stay makeable at the higher speed. |

## 💰 Economy & State Variables
| Variable Name | Type | Data Scope | Ownership / Authority | System Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `scrapCount` | `NetworkVariable<int>` | Replicated | Server-Authoritative | Tracks the active currency balance. Modified *only* on the Server; automatically synchronized down to all connected clients. |
| `testUpgradeAsset` | `DroneUpgradeData` | Inspector Test Field | Local Context Only | A temporary debug reference slot used to manually trigger item purchases directly via the Unity Editor UI. |

## 🏭 Spawning & Class Variables
| Variable Name | Type | Data Scope | Ownership / Authority | System Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `availableClasses` | `DroneClassData[]` | Global Registry | Server-Authoritative | Array of all available class templates. Referenced by the server during RPC requests to apply specific stats to player drones. |
| `defaultClassData` | `DroneClassData` | Local Component | Local Context | The specific class template assigned to a drone or target, injected dynamically by the spawner at runtime. |

## 🖼️ UI State Variables
| Variable Name | Type | Data Scope | Ownership / Authority | System Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `cachedClassIndex` | `int` | Local UI | Local Context | Temporarily holds the player's class selection index (-1 if none) until the network connection is established and the RPC is ready to fire. |
| `hostButton` | `Button` | Local Component | Local Context | Executes OnHostPressed to spin up a Host server loop and flush the cached class selection over RPC. |
| `clientButton` | `Button` | Local Component | Local Context | Executes OnClientPressed to connect locally and flush the cached class selection over RPC. |

## 🎮 Input Axis Registry
| Axis String | Type | Device / Control | System Purpose |
| :--- | :--- | :--- | :--- |
| `Xbox_RT` | `float` | Gamepad RT | Forward thrust (combined with LT → `thrust` −1..1) |
| `Xbox_LT` | `float` | Gamepad LT | Reverse thrust (optional; undefined axis reads 0) |
| `Vertical` | `float` | Gamepad L-Stick Y | **Aim Pitch** (nose up/down) — session-028 swap |
| `Horizontal` | `float` | Gamepad L-Stick X | **Aim Yaw** (turn) — session-028 swap |
| `TargetYaw` | `float` | Gamepad R-Stick X | **Strafe** (lateral) — session-028 swap |
| `TargetPitch` | `float` | Gamepad R-Stick Y | **Altitude** (ascend/descend) — session-028 swap |

> **Note (session-028):** axis *strings* still read the same physical sticks, but their *roles* are swapped so the LEFT stick aims. Mapping lives in `GamepadFlightInput.cs`; touch mirrors it via `TouchFlightHUD.leftStickAims`.

## 🔫 Weapon Input & Aim (session-031)
| Field / State | Type | Scope | Purpose |
| :--- | :--- | :--- | :--- |
| `FlightInputState.aimX/aimY` | `float` (−1..1) | Input snapshot | Free-aim reticle offset (0 = nose). Right stick in FreeAim mode. |
| `FlightInputState.firePrimary` | `bool` | Input snapshot | Hold to fire bullets. |
| `FlightInputState.fireSecondary` | `bool` | Input snapshot | Hold to fire rockets. |
| `FlightInputRouter.Aim` | `AimMode` | Per-drone | NoseAim / FreeAim; cycled by the AIM-MODE button. |
| `FlightInputRouter.Last` | `FlightInputState` | Per-drone | Cached merged input; HUD mirrors it, `DroneWeapon` reads it. |

`DroneWeapon` stats (bullet/rocket rate·speed·damage·life, `rocketSplash`, `maxAimAngle`) are inspector-tunable; projectiles are pooled via `Projectile` + `DroneWeapon`'s queues.

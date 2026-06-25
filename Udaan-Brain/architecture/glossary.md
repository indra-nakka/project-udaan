# Project Udaan: Data & Variable Glossary

## 🛸 Flight & Physics Variables
| Variable Name | Type | Data Scope | Ownership / Authority | System Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `forwardThrust` | `float` | Local Cache | Client (Owner) | Determines raw force added along `transform.forward` during forward acceleration passes. Inherited from Class Card data. |
| `isManuallyOverridingHover` | `bool` | Physics State | Client (Owner) | Flags true when player pushes thrust/altitude axes, instantly dampening the automatic hover height constraints to free up flight control. |
| `drag` | `float` | Rigidbody State | Client (Owner) | Unity physics drag value applied to simulate air resistance, preventing infinite sliding. Inherited from Class Card data. |
| `activeSpeedModifier` | `float` | Local Cache | Client (Owner) | A runtime multiplier (default `1.0f`) that scales `forwardThrust` when mid-game upgrades are consumed. |

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
| `Xbox_RT` | `float` | Gamepad RT | Forward acceleration / thrust |
| `Vertical` | `float` | Gamepad L-Stick Y | Altitude (Ascend/Descend) |
| `Horizontal` | `float` | Gamepad L-Stick X | Lateral translation (Strafe) |
| `TargetYaw` | `float` | Gamepad R-Stick X | Aim Yaw (Turn Left/Right) |
| `TargetPitch` | `float` | Gamepad R-Stick Y | Aim Pitch (Nose Up/Down) |

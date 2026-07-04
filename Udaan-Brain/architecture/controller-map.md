# 🎮 Controller Map — Canonical Flight Schema

**Role:** The authoritative gamepad layout (Xbox reference). The core 5 axes are mirrored as code variables in [[glossary]] (Input Axis Registry) — keep both in sync. Touch mapping is defined below (M1). Input-handling decision: [[DEC-002_Classic_Input_Manager]].

> **Input is device-agnostic (as of session-026).** Every source (gamepad, touch) produces a `FlightInputState` (5 axes + brake/boost) via the `IFlightInput` interface; `FlightInputRouter` merges sources so gamepad + touch can drive the same drone. `DroneFlightController` reads only from the router — it no longer calls `Input.GetAxis` directly. Source: `Assets/Scripts/Input/`.
>
> **Runtime binding (session-028):** the **LEFT stick now aims (yaw/pitch)** and the **RIGHT stick translates (strafe/altitude)** — on both gamepad and touch — so the primary hand does the aiming work. The gamepad table below shows the original design intent; the live binding is left↔right swapped. `thrust` is now −1..1 (RT − LT on gamepad; center-detent slider on touch).

| Input | Action | Function / Purpose |
|---|---|---|
| Left Stick | Push Any Direction | 3D Position: Ascend/descend (Up/Down) & Strafe/roll (Left/Right) |
| Right Stick | Push Any Direction | Aiming: Pitch nose up/down & Yaw nose left/right |
| Right Trigger (RT) | Hold | Thrust Forward: Accelerate straight ahead |
| Left Trigger (LT) | Hold + Right Stick | Free-Look: Look around without changing flight direction |
| Left Bumper (LB) | Hold / Tap | Light Bullets: Continuous rapid-fire weapon |
| Right Bumper (RB) | Tap / Hold | Heavy Rockets: Fire missile / Hold to lock-on |
| Button [A] | Hold / Tap | Air Brake / Reverse: Slow down quickly for tight turns |
| Button [X] | Tap + Left Stick | Quick Dodge: Evade incoming missiles in stick direction |
| Button [Y] | Tap | In-Flight Quick Menu: Radial overlay for shield/repair systems |
| Button [B] | Tap | Interact / Land: Contextual docking, takeoff, and landing |
| Right Stick Click (RS) | Press | Target Lock: Snap camera and target tracking to nearest enemy |
| D-Pad Up / Down | Press | Flight Mode Switch: Toggle between Beginner and Acro modes |
| D-Pad Left / Right | Press | Weapon Cycle: Switch bullet/rocket ammunition variants |
| Menu Button | Press | Pause Menu: Game settings and system options |

## 📱 Touch Control Map (M1 — `TouchFlightHUD`)

Two schemes, toggled in-game via the **AIM** button (or `T` key in Editor). **LEFT is the primary control (aim), RIGHT handles translation** — session-028 swap; flip via `leftStickAims`. Throttle is a persistent center-detent slider (up = forward, down = reverse). Regions/flags are inspector-exposed for on-device tuning.

| Touch Control | Scheme | Maps to | Function |
|---|---|---|---|
| Left virtual stick (dynamic) | Both | `yaw` (X) + `pitch` (Y) | **Aim** — turn + nose up/down (primary) |
| Right virtual stick (dynamic) | **TwinStick** | `strafe` (X) + `altitude` (Y) | Lateral drift + ascend/descend |
| Right-side drag (relative) | **MoveAndDrag** | `strafe`/`altitude` rate from finger delta | Swipe-to-translate, auto-recenters |
| Throttle slider (right edge) | Both | `thrust` (−1..1, center 0) | Forward / neutral / reverse; persistent (optional self-center) |
| AIM button | Both | — | Toggle TwinStick ⇆ MoveAndDrag |
| AIM-MODE button (`V` / gamepad X) | Both | `FlightInputRouter.CycleAim()` | NoseAim ⇆ FreeAim (right stick = strafe/alt ⇆ free-aim reticle) |
| PRIMARY fire button (LB / LMB) | Both | `firePrimary` | Rapid bullets (held) |
| SECONDARY fire button (RB / RMB) | Both | `fireSecondary` | Dumb rockets, splash (held) |
| VIEW button | Both | `CameraController.ToggleView()` | Chase ⇆ FPV |
| RESTART button (or `R` key) | Both | `RaceManager.RestartRace()` | Reset drone to start + replay countdown |

**Race flow (`RaceManager`):** 3-2-1-GO countdown freezes the drone (`FlightLocked` + kinematic); cross the **gold** start/finish line to start the clock, hit the **green** gates in order, then cross the start/finish line again to close each lap (× `totalLaps`).

Assists (in `DroneFlightController`): **auto-level** returns the nose to level when no pitch input (big touch-usability win); cosmetic bank on turn is preserved. **`invertPitch`** (default on) flips vertical aim to taste. A `-o-` **crosshair** marks screen center (`TouchFlightHUD.showCrosshair`).

**M1 touch simplification (session-030):** no brake button — the throttle has a **sticky neutral** (`throttleCenterDeadzone`) so you coast into turns by centering it. **Controller mirroring:** when a gamepad is connected, `controllerHudMode` = *Mirror* shows fixed-home sticks that reflect the live physical input (via `FlightInputRouter.Last`), or *Hide* removes the touch UI. Gamepad + touch coexist; the final game supports connected controllers as first-class input.

## 🔫 Weapons & Aim (session-031, v1 — to tune by playtest)

Two weapons, two fire buttons (`DroneWeapon`): **primary bullets** (rapid, low damage) and **secondary rockets** (slow, splash). Projectiles are procedurally created + **pooled** (invariant), fired along the nose rotated within a `maxAimAngle` cone by the free-aim reticle.

**Aim model (session-037): free-look camera + full-AI crosshair.**
- **Aim modes** (`FlightInputRouter.AimMode`, cycled by AIM-MODE button / `V`): **FreeAim (default)** — the right stick orbits the **camera/view** within a cone (`CameraController.maxLookYaw/Pitch`), decoupled from flight, recentering on release, so you look & fire in one direction while flying another. **NoseAim** — right stick = strafe/altitude, view locked forward.
- **Crosshair is AI-driven:** `TargetingSystem` finds all `TargetHealth` within `lockRange` that are **on-screen**, locks the **nearest** by default, and the HUD's `-o-` crosshair snaps onto it (center when no lock). Weapons fire straight down the camera view, then bend onto the lock (`assistStrength` ~0.9 = near-auto-aim; lower = skill/hard mode, capped by `maxAssistAngle`).
- **Target switch:** dedicated input — RS-click (`JoystickButton9`) / `Tab` / on-screen **TARGET** button cycles the on-screen candidates (nearest-first). Use-case is narrow (close/overlapping targets), hence a button not the analog.
- **Dash/juke:** gamepad **X** (`JoystickButton2`) / Left Ctrl → instant burst in the left-stick direction (cooldown `dashCooldown`). Touch swipe TBD.

Targets for testing: `SinglePlayerBootstrap` spawns procedural `TargetHealth` dummies; `TargetHealth` now applies damage offline (`!IsSpawned || IsServer`) and respawns on pop.

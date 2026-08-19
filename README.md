# 🛸 Project Udaan

> **A Touch-First Build-and-Play Drone Game for Kids (Ages 5–10)**  
> *Combining stealth STEM learning through modular quadcopter assembly with playful, non-violent flight missions.*

---

## 📌 Overview

**Udaan** ("Flight") is an upcoming mobile and PC game designed for young players (ages 5–10). Kids build and customize real-ish quadcopters in an interactive **Garage**, discovering STEM concepts (mass vs. thrust, battery weight vs. flight time, prop pitch vs. speed) through visible and felt physical trade-offs. They then pilot their custom creations through varied playful missions—delivery, rescue, races, photo ops, and friendly toy-blaster skirmishes.

---

## 🌟 Key Pillars & Features

* **🛠️ The Garage is the Teacher:** Stealth STEM learning with no lectures. Kids swap frames, motors, propellers, batteries, and sensors, seeing how every part affects handling, speed, and durability.
* **🔋 The Drone is the Progress Bar:** Rebuild a drone from scavenged junk parts (Tier 0) up to sleek, high-tech custom builds (Tier 5).
* **🎈 Playful & Kid-Safe:** Features non-lethal toy weapons (foam dart cannons, water blasters, bubble sprayers, net launchers). Opponent bots gently power down instead of being destroyed.
* **🕹️ Device-Agnostic Input Architecture:** A unified `IFlightInput` interface powers player controls (Touch HUD, Gamepad, Keyboard) and AI pilots (Enemies and Allies) identically.
* **🎯 Accessible Flight & Aim Assist:** Features intuitive altitude hold, soft lock-on targeting, and smart aim assist tailored for touchscreen and controller play.
* **🚀 Mission Arc ("Sky Sentinel"):** Playable slice featuring wave skirmishes, outpost captures, core defense, mid-mission power upgrades, and memorable boss battles.

---

## 📂 Repository Structure

```
project-udaan/
├── 🎮 udaan-client/        # Unity (C#) Client project (Unity 6+)
│   └── Assets/
│       ├── Scripts/         # Flight, Weapons, Targeting, UI & Gameplay logic
│       ├── Art/             # Models, Textures, Materials & Visual FX
│       └── Scenes/          # Main gameplay and sandbox scenes
├── 🌐 udaan-server/        # Backend server infrastructure (multiplayer stack parked)
├── 🧠 Udaan-Brain/          # Single Source of Truth design documentation vault
│   ├── Game-Design-Document.md  # Core GDD (Source of truth)
│   ├── UDAAN_ROUTER.md          # Central documentation index
│   ├── Design-Garage-and-Learning.md # Garage STEM trade-offs & progression
│   ├── Art-Direction.md         # Visual guide & asset tiers
│   └── blender-previews/        # Rendered previews of procedural Blender models
├── 🎨 build_models.py       # Headless Blender Python script for 3D model generation
├── ⚙️ build-models.bat     # Windows batch runner for Blender model generation & export
└── 🧪 compile-check.bat    # Headless Unity script compilation checker
```

---

## 🧰 Key Subsystems & Architecture

| Subsystem | File / Location | Description |
|---|---|---|
| **Flight Physics** | [`DroneFlightController.cs`](file:///d:/Game%20Dev/project-udaan/udaan-client/Assets/Scripts/DroneFlightController.cs) | Handles thrust, yaw, pitch, roll, upright restoration, and bank mechanics. |
| **Input Routing** | [`IFlightInput.cs`](file:///d:/Game%20Dev/project-udaan/udaan-client/Assets/Scripts/Input/) | Interface allowing Keyboard, Touch, Gamepad, or AI to drive any drone. |
| **Weapons & Tools** | [`DroneWeapon.cs`](file:///d:/Game%20Dev/project-udaan/udaan-client/Assets/Scripts/DroneWeapon.cs) | Implements non-lethal weapons (darts/rockets) with cooldowns and reload mechanics. |
| **Targeting & Health** | [`TargetHealth.cs`](file:///d:/Game%20Dev/project-udaan/udaan-client/Assets/Scripts/TargetHealth.cs) | Soft lock-on assist, team management (friendly fire off), and health scaling. |
| **Asset Pipeline** | [`build_models.py`](file:///d:/Game%20Dev/project-udaan/build_models.py) | Batch Blender script creating low-poly quadcopters, weapons, and accessories. |

---

## ⚙️ Development Workflows

### 1. Unity Client Setup
- **Unity Version:** Unity 6 (`6000.4.3f1` or later recommended).
- Open `udaan-client/` in Unity Hub and launch the project.

### 2. Procedural 3D Model Generation (Blender)
The project includes a headless Blender automation script that builds 3D models, exports `.fbx`/`.obj` assets directly into `udaan-client/Assets/Art/Models/`, and renders preview images to `Udaan-Brain/blender-previews/`.
- Ensure Blender is installed and added to PATH (or configure `build-models.bat`).
- Run `build-models.bat` from the root directory.

### 3. Headless Script Compilation Check
To verify C# scripts compile cleanly without launching the Unity GUI:
- Close any open Unity Editor instances.
- Double-click `compile-check.bat`.
- Check `compile.log` for output.

---

## 📚 Documentation & Reference

All design decisions, game mechanics, and technical rubrics are maintained in `Udaan-Brain/`:
* 📄 **[Game Design Document](file:///d:/Game%20Dev/project-udaan/Udaan-Brain/Game-Design-Document.md):** Single Source of Truth for vision, mechanics, and audience.
* 🗺️ **[Documentation Router](file:///d:/Game%20Dev/project-udaan/Udaan-Brain/UDAAN_ROUTER.md):** Index of all system specs, ADRs, tasks, and changelogs.
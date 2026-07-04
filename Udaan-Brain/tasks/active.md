# 🏃 Active Sprint Tasks

**Role:** The *current* focus only. Completed items move to [[tasks/done]] (never deleted). Big-picture sequencing lives in [[📋 Master Plan]] and [[🎮 Combat & Game Vision]].

## 🎯 Current focus: Combat prototype → "Sky Sentinel" demo
*Flight + aim + weapons feel is landing. Next: make the range fight back, then assemble the vertical-slice demo.*

### In progress
- [ ] **Tier-1 enemy AI** — an AI "brain" that pilots a drone via `IFlightInput` (seek/strafe/keep-distance + engage/evade states) and shoots using `TargetingSystem`. Reuses all player flight/weapon code.

### Up next
- [ ] Hit feedback (hit-marker/flash, damage numbers) + audio for weapons — "feel" pass
- [ ] Assemble the **"Sky Sentinel"** demo mission (traverse → skirmish → capture outposts → defend → boss); see [[🎮 Combat & Game Vision]]
- [ ] Collision push-off (clip a ring/ground/wall = bounce, keep flow)
- [ ] On-device touch pass: validate two-thumb feel + declutter HUD on a real phone
- [ ] Full PUBG-style HUD layout customization (rects/sizes already inspector-exposed)

## ✅ Recently completed (full archive in [[tasks/done]])
- **Combat aim (session 031–042):** device-agnostic weapons; **free-look camera** (right stick orbits view, decoupled from flight); **full-AI crosshair** + soft **lock-on** (`TargetingSystem`) with class/centered/distance priority, target switching (button + flick), off-screen arrow; two weapons (bullets + splash rockets, pooled, multi-muzzle-ready); juke/dash; HUD instruments — corner-bracket reticle, green flight chevron, artificial horizon, SPD/ALT, **mini-radar**; FPV hides own body.
- **M1 Touch Flight Toy (session 026–030):** input layer + touch HUD, offline play, hoop-racing circuit, start/finish line + countdown + restart, stick-swap, center-detent throttle, ~3x speed, controller mirroring/auto-hide.
- Foundations: Unity URP + mobile, NGO network sandbox, class-spawn RPCs; scrap economy + upgrade tree + HUD; drone class framework (Striker, Bulwark) + 5-axis flight engine.

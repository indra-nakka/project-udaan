# 🏃 Active Sprint Tasks

**Role:** The *current* focus only. Completed items move to [[tasks/done]] (never deleted). Big-picture sequencing lives in [[📋 Master Plan]] and [[🎮 Combat & Game Vision]].

## 🎯 Current focus: Combat prototype → "Sky Sentinel" demo
*Flight + aim + weapons feel is landing. Next: make the range fight back, then assemble the vertical-slice demo.*

### In progress
- [ ] **"Sky Sentinel" demo — test & tune** (spine done, session-050: `MissionDirector` waves→boss→win/lose). Tune wave counts/HP, boss stats, pacing. Then add the richer beats.

### Up next (demo depth)
- [x] Juice pass v1 — VFX (explosions/sparks/tracers/shake) + procedural SFX (session-052)
- [x] Capturable **outposts** + allied AI drones (session-053)
- [x] **Defend** objective beat — hold the core (session-054)
- [x] Mid-mission **upgrade** pick (session-054)
- [ ] Difficulty settings (assist strength / enemy stats presets) — requested, later
- [ ] **Authored audio** (real weapon/impact SFX + music) — needs asset files
- [ ] **On-device touch pass** (shipping target)

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

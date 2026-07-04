---
id: 2026-07-04-combat-aim
type: session
status: active
task: [[tasks/active]]
related: [[🎮 Combat & Game Vision]], [[controller-map]], [[glossary]]
---

## 🎯 Goal
Prototype combat: aiming + weapons that feel good on controller/touch without a mouse, reference **Future Cop: LAPD** (aim-assist/lock-on, proto-MOBA). Build toward the "Sky Sentinel" vertical-slice demo.

## ⚙️ Execution Mode
Iterative playtest loop — implement, user tests in Editor (PC + controller), tune. Sessions 031–042.

## 📝 Actions Log (by session)
- **031:** Weapons v1 — `DroneWeapon` rewrite (bullets + splash rockets, pooled procedural `Projectile`), aim modes (Nose/Free), fire buttons; offline-friendly `TargetHealth` + procedural dummy targets.
- **032–033:** Tuning — bright targets, health-driven shrink, damage (6 bullets / 2 rockets to pop); **auto-level OFF** (was breaking hold-to-aim); free-aim invert fix; targets on the gate ring.
- **035–037:** **Free-look camera** (right stick orbits view, decoupled from flight, recenters) + **full-AI crosshair** + soft **lock-on** (`TargetingSystem`); fire down the view; target switching (RS-click / Tab / TARGET button + right-stick flick).
- **038:** Fire calibration (full aim onto lock → hits crosshair at edges); **off-screen target arrow**; **multi-muzzle** groundwork.
- **039:** Target priority (class > centered > distance via `TargetHealth.targetPriority`); flight-vector pip; SPD/ALT readout; smaller buttons.
- **040–041:** Corner-bracket reticle (scales with distance); **artificial horizon** (fixed reference + rolling/pitching bar); flight marker → green chevron; **FPV hides own body**.
- **042:** **Mini-radar** (blips relative to heading, lock highlighted).
- Also: juke/**dash** (X / Ctrl → burst in left-stick direction), FreeAim default.

## 🧠 Key architectural wins
- **Input is device-agnostic** (`IFlightInput` → `FlightInputRouter`): AI enemies will just be another input source producing a `FlightInputState`, reusing `DroneFlightController` + `DroneWeapon` verbatim.
- **`TargetingSystem` is reusable**: player uses it now; enemy AI points it at the player later.
- Everything procedural/auto-provisioned → the offline `SinglePlayerBootstrap` scene needs zero manual wiring.

## ✅ Verification
- [x] Static checks (brace/reference grep) on all changed scripts each session.
- [x] User playtested each step in-Editor (PC + controller); lap/kill feel confirmed good.
- [ ] Editor compile of the full combat set in one go (user to confirm no Console errors).
- [ ] On-device touch pass.

## 📌 Summary (for `sessions/_index.md` & `changelog.md`)
session 031–042: combat prototype — free-look aim, soft lock-on with class/centered/distance priority + switching + off-screen arrow, two pooled weapons (multi-muzzle-ready), dash, and a full HUD instrument set (reticle, chevron, horizon, SPD/ALT, radar). Next: Tier-1 enemy AI → "Sky Sentinel" demo.

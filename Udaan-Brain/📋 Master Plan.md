---
type: strategy
status: active
updated: 2026-06-30
tags: [roadmap, milestones, planning]
---

# 📋 Master Plan

> Execution detail & timelines: [[UDAAN_MASTER_PROJECT_PLAN]]. Why / assessment / competition: [[UDAAN_Analysis_and_Competition]]. This page is the at-a-glance roadmap.

## 🎯 Guiding Principle: Sequence the Risk
Answer the riskiest, fun-defining questions first and cheapest. Do **not** build later layers until the earlier gate is "yes".
1. Does flying feel good on a **touchscreen**?
2. Is fly + shoot + upgrade fun vs a bot?
3. Is it fun **1v1 online**?
4. Is it fun **3v3 with classes + economy**?
5. Will players come back tomorrow?
6. Will they pay / tell friends?

## 🗺️ Milestone Roadmap

### ✅ Foundations (done)
- [x] Phase 0: Repo, Unity URP, Mobile target, LFS, Udaan-Brain wiki
- [x] Phase 2: Core Offline (Rigidbody flight, FPV/TPV cameras, weapons, dummy targets)
- [x] Phase 3: Network Sandbox (NGO, class-spawn RPCs, host/client routing)
- [x] Phase 4: Scrap Economy & Upgrade Tree (currency, drops, wallet, upgrade framework, HUD)
- [x] Drone class framework (Striker, Bulwark) + 5-axis gamepad flight engine

### 🔴 M1 — Touch Flight Toy *(current priority — Gate Q1)*
- [ ] Prototype 2–3 **touch** control schemes (move/strafe + aim-drag + auto-throttle, etc.)
- [ ] Flight assists: auto-level, banking assist, altitude hold, collision push-off
- [ ] Boost/dash + aim-assist tuning for touch
- [ ] Test on a real mid-range phone — first-timer flies a fun obstacle course in <60s
- [ ] **GATE:** is flying fun on touch? If no, iterate here before advancing.

### 🟠 M2 — Combat Sandbox *(Gate Q2)*
- [ ] 3 weapon archetypes: hitscan (Pulse Laser), arc+splash (Plasma Mortar), utility (EMP Mine)
- [ ] Secondary slot + hot-swap; object pooling for projectiles/VFX
- [ ] Health + shield layers, damage numbers, death/respawn, status-effect framework
- [ ] In-match purchase store UI (tap without stopping flight)
- [ ] Combat **AI bot** (reused later for bot-fill + story mode)
- [ ] Analytics + "fun journal"; 3–5 external playtests
- [ ] **GATE:** fun vs a bot on a phone? Yes → proceed.

### 🟡 M3 — 1v1 Netcode Proof *(Gate Q3 — the deep pit)*
- [ ] Evaluate **Photon Fusion / Quantum** vs NGO for fast physics sync (decide via ADR)
- [ ] Client-side prediction + server reconciliation + interpolation for drone movement
- [ ] Lag compensation for hit registration
- [ ] Smallest possible networked 1v1 on one map — **timeboxed**
- [ ] **GATE:** does the fun survive the wire? Simplify design if not (slower drones / smaller arena / lower tick).

### 🟢 M4 — The Core Game *(Gate Q4)*
- [ ] 3rd class: Retriever/Support + 1 active + 1 passive ability per class
- [ ] 3v3 Team Deathmatch (score, timer, end screen, spawn protection)
- [ ] Matchmaking (Node backend), lobbies, reconnect, **bot-fill**
- [ ] Server-authoritative combat + economy; basic anti-cheat
- [ ] 2 maps (Backyard ✅ blockout, Garage)

### 🔵 M5 — Soft Launch Readiness
- [ ] Progression/XP, Garage/loadout, tutorial onboarding
- [ ] Analytics funnels, remote config, crash reporting, backend scaling/load test
- [ ] Art pass (toon shader, stylized props, class silhouettes), audio pass, optimization
- [ ] Store setup + 1 monetization hook (**cosmetic-only**)
- [ ] Soft launch in 1–2 small markets; tune D1/D7 retention

### ⚪ M6 — Launch + LiveOps
- [ ] Cosmetics pipeline, battle pass / seasons, content cadence
- [ ] Global launch + ASO + trailer

### 🗃️ Post-Launch (Cut List — deferred on purpose)
- [ ] Modes: Base Assault, CTF, Pure Racing, Racing-with-abilities (Death Race), FFA
- [ ] **Story campaign** (Pokémon-route bosses → "greatest drone pilot")
- [ ] More classes / maps; ranked ladder

## ⏱️ Timeline Snapshot (to soft-launchable M5)
- Solo, part-time (~20h/wk): **~24–36 months**
- Solo, full-time: **~14–22 months**
- Small team (+1 netcode/backend, +part-time artist): **~10–15 months**
- Total effort to a *live* game: **~3,000–6,000+ hrs** (most of it after "feature complete")

## ⚠️ Top Risks to Manage
1. **Scope** — ship M4 before any extra mode or the story. Default new features to post-launch.
2. **Touch flight feel** — under-tested; product is touch, current model is gamepad.
3. **Netcode for 6-DoF flight** — don't hand-roll; timebox M3.
4. **Empty lobbies** — solve population with bots/bot-fill/small counts before launch.
5. **Pay-to-win** — never; cosmetics only.

## 📉 Burndown Tracker
*(Track remaining hours per active sprint here — see Google Sheet master plan for the full task-level burndown.)*

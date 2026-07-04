---
type: strategy
status: active
updated: 2026-06-30
tags: [plan, wbs, timelines]
---

# 🛸 Udaan — Master Project Plan (Execution)

*The actionable **what / when**. The **why** (honest assessment, competition, brainstorming) lives in [[UDAAN_Analysis_and_Competition]]. At-a-glance roadmap: [[📋 Master Plan]]. Updated 30 June 2026.*

> **Working assumptions** (change these and the timelines shift): effectively a **solo dev / very small team**, leveraging pre-made & AI-assisted art, **part-time (~15–25 hrs/wk)**, **bootstrapped**, aiming for a **commercial soft launch** on iOS + Android. Team / full-time changes are called out where they matter.

---

## 1. Guiding Principle — Sequence the Risk
Answer the riskiest, fun-defining questions first and cheapest. Each gate is blocked by the previous; do not advance until it's "yes". (Rationale: [[UDAAN_Analysis_and_Competition#2. Strategic Reframe — Why "Sequence the Risk"]].)
1. Does flying feel good on a **touchscreen**?
2. Is fly + shoot + upgrade fun **vs a bot**?
3. Is it fun **1v1 online**?
4. Is it fun **3v3 with classes + economy**?
5. Will players **return tomorrow**?
6. Will they **pay / tell friends**?

Everything below maps to answering these in order. Do **not** build extra modes or the story until 1–4 are answered yes.

---

## 2. Work Breakdown (Full)
Exhaustive on purpose — not all of it is v1. The Release Phasing (§3) says what to cut. ☑️ = done / in progress per the repo.

### Phase 0 — Foundation & Pre-Production *(mostly done)*
**0.1 Project & tooling**
- ☑️ Git monorepo, .gitignore (Unity/Node), Git LFS
- ☑️ Unity URP, mobile target (iOS/Android)
- ☑️ Knowledge base (Udaan-Brain)
- [ ] Dedicated LFS host (GitHub LFS gets costly), build/version scheme, player-facing CHANGELOG
- [ ] Issue tracker for bugs (GitHub Projects/Linear) beyond the task sheet

**0.2 Design pillars & scope lock**
- [ ] 1-page GDD "constitution": 3 pillars, the 30-second fantasy, what the game is NOT
- [ ] Minimum-Viable-Fun spec (the vertical slice)
- [ ] Explicit **Cut List** (racing, CTF, story… deferred on purpose)
- [ ] Target-device bar (e.g., 60fps on a 3-yr-old mid-range Android)

**0.3 Reference & feel**
- [ ] "Feel" reference clips (Future Cop, Zanzarah, Re-Volt, Ace Combat)
- [ ] Default camera per mode (FPV vs TPV) — pick the touch-friendly default

### Phase 1 — Core Game Feel (Single-Player, No Network) → *Gates Q1 & Q2*
**1.1 Flight model**
- ☑️ Rigidbody drone controller (hover, gravity comp, drag)
- ☑️ 5-axis analog (gamepad) flight engine
- [ ] **▶ TOUCH control scheme (critical):** prototype 2–3 layouts (move/strafe + aim-drag + auto-throttle; tilt-assist; tap-to-target)
- [ ] Flight assists: auto-level, banking assist, altitude hold, collision push-off
- [ ] Boost/dash (cooldown), aim-assist/"snap-to" tuning for touch
- [ ] Speed/agility/drag/turn curves as per-class data
- [ ] Juice: screen shake, FOV kick on boost, motion lines, velocity-pitched thruster audio
- [ ] **GATE Q1:** on a real phone, a first-timer flies a fun figure-8 / obstacle course in <60s

**1.2 Camera**
- ☑️ FPV (tilt anticipation), ☑️ TPV (collision-aware)
- [ ] Camera tuning per speed/mode; lock-on / soft-target behavior

**1.3 Combat — weapons**
- ☑️ Modular weapon system (ScriptableObjects), ☑️ projectile prefab (Nerf Dart)
- [ ] Future Cop archetypes: hitscan (Pulse Laser), arc+splash (Plasma Mortar), utility (EMP Mine)
- [ ] Secondary slot + hot-swap; **object pooling** for projectiles/VFX (do early, not "polish")
- [ ] Hit registration, modular hitboxes (body vs rotor multipliers), lock-on/homing, ammo/heat/reload

**1.4 Damage & state**
- ☑️ Target health / dummy targets
- [ ] Health + shield layers + regen; damage numbers, hit markers, kill feedback
- [ ] Death/explosion/respawn; generic **status-effect framework** (EMP/stun/slow/burn — reused by abilities)

**1.5 Classes & abilities**
- ☑️ DroneClassData template; ☑️ Striker + Bulwark
- [ ] 3rd archetype: **Retriever/Support** (fast, low armor, utility)
- [ ] **Signature ability per class** (1 active + 1 passive) + cooldown/resource framework
- [ ] Class selection flow (☑️ basic UI). *Hold at 3 classes — balance cost is quadratic.*

**1.6 Economy loop (single-player first)**
- ☑️ Scrap currency, ☑️ pickup prefab + piñata drop, ☑️ wallet, ☑️ upgrade-tree framework, ☑️ HUD counter
- [ ] In-match purchase UI (tap without stopping flight)
- [ ] Upgrade content: tiers, costs, effects, caps; economy-curve balance pass (sets match pacing)

**1.7 Single-player content for testing & onboarding**
- [ ] **Combat AI bot** (steering/navmesh flight) — reused for bot-fill *and* story mode
- [ ] Training/tutorial flow (teaches touch flight = onboarding)
- [ ] One greybox arena
- [ ] **GATE Q2:** fly + shoot + upgrade is fun vs a bot, on a phone

### Phase 2 — Multiplayer Core → *Gates Q3 & Q4 (the expensive phase)*
**2.1 Foundation**
- ☑️ Netcode framework imported (NGO)
- [ ] **Re-evaluate:** Photon Fusion / **Quantum** (deterministic, fast physics, anti-cheat-friendly) vs NGO → decide via ADR
- [ ] Tick rate, transport, ownership model

**2.2 The hard part — movement sync**
- [ ] Client-side prediction; server reconciliation/rollback
- [ ] Interpolation/extrapolation for remote drones; lag compensation for hits
- [ ] **Timebox this.** Prove 1v1 first; budget months, not the sheet's 24h

**2.3 Sessions & matchmaking**
- [ ] Lobby: host/join by code, ready-up (☑️ partial)
- [ ] Node backend: auth (guest-first), matchmaking, session assignment (☑️ scaffolded)
- [ ] Dedicated vs relay/host-migration decision (cost model)
- [ ] Region/ping matchmaking; reconnect/disconnect; **bot-fill** (population insurance)

**2.4 Networked combat & economy**
- [ ] Sync projectiles/explosions/splash; server-authoritative damage/death
- [ ] Server-authoritative Scrap/upgrades (☑️ architecture in place)
- [ ] Anti-cheat basics (authority + validation + rate limits)

**2.5 First networked mode**
- [ ] Pick ONE: 1v1 duel or 3v3 TDM (TDM = genre default, most forgiving)
- [ ] Score limit, timer, end screen, spawn/respawn + spawn protection
- [ ] **GATE Q4:** fun 3v3 with classes + economy + matchmaking

### Phase 3 — Content & Systems Breadth *(only after Q4 = yes)*
**3.1 Maps**
- ☑️ "Backyard" blockout
- [ ] "Garage" blockout (vertical, tight); per-map collision, kill planes, spawns, Scrap caches, cover, balance
- [ ] Reusable map-building checklist/pipeline

**3.2 Additional modes** *(Cut List — post-launch unless proven essential)*
- [ ] Base Assault (generator + AI turrets), CTF, Pure Racing, Racing-with-abilities (Death Race), FFA
- *Each = new UI + balance + tutorial + bugs. Ship 1, add rest as live content.*

**3.3 Progression & meta**
- [ ] Account level/XP; unlock path (classes/weapons/cosmetics); Garage/loadout screen
- [ ] Daily/weekly challenges (retention); Ranked/MMR ladder (schema exists)

### Phase 4 — Art, Audio & Identity
- [ ] Lock Ghibli/sunset + Re-Volt-scale art bible (☑️ Aesthetic Canvas started); toon/cel shader in URP
- [ ] Replace greybox with stylized props; class-readable drone silhouettes
- [ ] VFX (muzzle/tracers/explosions/thruster trails/abilities — VFX Graph); environment polish
- [ ] Audio: thruster loops (velocity-pitched), weapon/hit/UI SFX, announcer, menu+match music, mobile mix
- [ ] Brand: name lock, logo, **store icon** (conversion lever — A/B), trailer, social identity

### Phase 5 — Mobile Optimization & Hardening
- [ ] Object pooling everywhere; baked lighting (static), draw-call reduction, LODs, atlasing
- [ ] Profiler passes on target devices; **thermal-throttle** + battery tests; memory/bandwidth budgets
- [ ] Per-tier frame targets, dynamic resolution; crash/ANR reduction

### Phase 6 — Backend, LiveOps & Infrastructure
- [ ] Auth (guest-first; never hand-roll credential storage), profile/inventory service
- [ ] Matchmaking scaling + auto-scale + load tests; multi-region dedicated hosting + cost model
- [ ] **Analytics** (funnels, retention, match telemetry — instrument before launch); remote config (tune without app update)
- [ ] Live-event/content scheduling; crash reporting + monitoring/alerting
- [ ] Anti-cheat hardening, report/abuse systems, support pathway, ToS/Privacy, age gating (COPPA/GDPR-K)

### Phase 7 — Monetization & Business
- [ ] Pick model early (shapes design): **cosmetic-only / battle pass** for competitive fairness
- [ ] Store + IAP (StoreKit / Play Billing — never handle card data); cosmetic pipeline (skins/trails/liveries)
- [ ] Battle pass / seasons; pricing + regional pricing + store compliance
- [ ] (Optional) rewarded ads kept out of competitive integrity

### Phase 8 — QA, Soft Launch & Launch
- [ ] Test plan (unit tests for systems logic + manual passes + device matrix); CI builds → TestFlight + Play Internal
- [ ] Closed beta / recurring playtests; balance telemetry loop (class win-rates, match length, economy curve)
- [ ] Soft launch in 1–2 small markets (NZ/PH/CA); iterate D1/D7 before global
- [ ] ASO (keywords, screenshots, trailer, icon A/B); global launch beat
- [ ] Post-launch content cadence plan (the real work *starts* at launch)

---

## 3. Release Phasing — What Actually Ships First
The single most important section for not drowning.

| Milestone | Goal | Contains | Cuts |
|---|---|---|---|
| **M1 Touch Flight Toy** | Prove Q1 | Touch controls + assists, 1 drone, 1 greybox space, juice | No combat / net / classes |
| **M2 Combat Sandbox** | Prove Q2 | Weapons, health, Scrap loop, 1 bot, in-match store | No multiplayer |
| **M3 1v1 Netcode Proof** | Prove Q3 | Smallest multiplayer, movement sync, 1 map | No class/economy depth, no MM UI |
| **M4 The Core Game** | Prove Q4 | 3 classes + abilities, 3v3 TDM, economy, 2 maps, matchmaking, bot-fill | All other modes, story, cosmetics |
| **M5 Soft Launch** | Prove retention & cost | Progression, tutorial, analytics, backend scale, store, 1 monetization hook | Global marketing |
| **M6 Launch + LiveOps** | Grow & retain | Cosmetics, battle pass, mode #2, content cadence | — |
| **Post-launch** | Breadth | Racing, CTF, Death Race, **story campaign**, more classes/maps | — |

*The story campaign is post-launch — but the combat **bots/AI** built for M2 are the foundation for both bot-fill and the campaign. Build the AI once, reuse it three ways.*

---

## 4. Realistic Timelines
Calendar estimates including rework, life interruptions, and the last-20%-takes-80% rule.

### 4.1 Effort by milestone (solo, ~20 hrs/wk)
| Milestone | Optimistic | Realistic |
|---|---|---|
| M1 Touch Flight Toy | 3–4 wks | 6–8 wks |
| M2 Combat Sandbox | 5–6 wks | 8–12 wks |
| M3 1v1 Netcode | 8–10 wks | **4–6 months** |
| M4 Core Game (3v3, classes, MM) | 3–4 months | **6–9 months** |
| M5 Soft Launch readiness | 3–4 months | **5–7 months** |
| M6 Launch + first season | 2–3 months | **4–6 months (+ ongoing forever)** |

### 4.2 Scenarios (to a soft-launchable M5)
- **Solo, part-time (~20h/wk): ~24–36 months.** (The popular "10–15 months" figure reaches a rough prototype ≈ M3/M4, not a launchable live game.)
- **Solo, full-time (~40h/wk): ~14–22 months.**
- **Small team (you + 1 netcode/backend dev + part-time artist): ~10–15 months**, and the scariest risk (netcode) gets a specialist. *If you can afford one hire, make it a multiplayer/backend engineer.*

### 4.3 Total-hours reality
- To a playable prototype (≈M3–M4): **~900–1,200 hrs**, maybe slightly low.
- To a launchable, populated, monetized live game (M5–M6): **~3,000–6,000+ hrs**, the majority *after* "feature complete" — content, balance, liveops, population never stop.

---

## 5. What You Need to Succeed (actionable)
1. **Ruthless scope discipline** — default new features to "post-launch"; ship M4 before mode #2.
2. **A real phone in hand, constantly** — feel and performance lie in the editor.
3. **Solve population before launch** — bots, bot-fill, async, small counts.
4. **Don't write netcode from scratch** — Photon Fusion/Quantum or NGO; buy specialist time if possible.
5. **Playtesters early and often** — a dozen weekly players beat any document.
6. **Instrument everything** — analytics from M2 onward.
7. **Cosmetic-only monetization** — protect competitive trust.
8. **A finishing mindset** — pick a scope you can 100%-finish.
9. **Keep the Udaan-Brain discipline** — it keeps a long solo journey coherent.

## 6. What to Avoid
1. Building modes 2–5 / the story before the core 3v3 is fun and stable. *(The #1 trap.)*
2. Polishing art before the loop is fun — greybox until M4.
3. Hand-rolling netcode, auth, matchmaking, or anti-cheat when middleware exists.
4. Pay-to-win — poisons a competitive community instantly.
5. Designing for a full lobby you won't have — assume empty servers.
6. Tuning flight only on gamepad — the product is touch; validate touch now.
7. Treating "feature complete" as "done" — for a live game it's the start.
8. Perfectionism / infinite tuning on one system before the whole loop is proven.
9. Underestimating server costs — model multi-region dedicated hosting before committing.
10. Skipping legal/privacy basics (ToS, privacy, age gating) — the style attracts minors.

## 7. Next 30 / 60 / 90 Days
**30 — Prove flight on touch (M1).** Build 2–3 touch prototypes + assists; test on a real mid-range phone; first-timer flies a fun course in <60s. Park gamepad/netcode work; one greybox playground only.

**60 — Combat sandbox (M2).** Add 3 weapon archetypes, health/shields, Scrap loop + in-match store, one combat bot. Hook up analytics + a "fun journal". Run 3–5 external playtests. Gate: fun vs a bot on a phone?

**90 — 1v1 netcode proof (M3).** Evaluate Photon Fusion/Quantum (ADR), then stand up the smallest networked 1v1 with movement sync. Timebox it — prove the fun survives the wire before 3v3, class depth, and matchmaking.

---

*If the working assumptions are wrong (team size, hours/week, budget, commercial vs passion), say so and the timelines/phasing get re-cut.*

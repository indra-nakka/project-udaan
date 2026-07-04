---
type: strategy
status: living
updated: 2026-06-30
tags: [analysis, competition, brainstorming]
---

# 🧭 Udaan — Analysis, Competition & Brainstorming

*Companion to [[UDAAN_MASTER_PROJECT_PLAN]]. This file holds the **why** — honest assessment, market/competition analysis, and open ideas. The plan doc holds the **what/when**. Updated 30 June 2026.*

---

## 1. Honest Assessment

### 1.1 What is genuinely strong
- **Clear, specific, personal vision.** "Future Cop weapon/upgrade economy + Zanzarah z-axis arena + Re-Volt scale + Ghibli look + flight" is a precise north star. Most failed projects can't state their pillars in one sentence; this one can.
- **A real market gap** (see §3). The drone-game space is dominated by gritty military FPV sims. A playful, stylized, hero-class drone arena on **mobile** is genuine white space.
- **Exceptional documentation discipline.** The Udaan-Brain wiki — ADRs, glossary, session logs, append-only changelog, BFS/DFS rubrics — is better process hygiene than most funded studios. A real force multiplier.
- **Past the talk stage.** Working flight controller, netcode sandbox, two classes, Scrap economy + upgrade tree, HUD. Runnable progress, not a pitch.
- **Right architectural shape.** ScriptableObject-driven classes/weapons/upgrades + server-authoritative economy is exactly how a scalable competitive title should be built.

### 1.2 What worries me most (priority order)
1. **Scope is the existential risk.** Real-time physics-synced mobile multiplayer **and** hero classes **and** a MOBA-lite economy **and** 4+ modes **and** a story campaign — any one is a project. Stacked, this is multi-year multi-person scope approached part-time/solo. Most likely cause of death.
2. **Netcode for 6-DoF flight is one of the hardest problems in the medium.** Prediction/reconciliation is hard for grounded shooters; harder for fast free-flying physics drones. The sheet budgets NET-02 at 24h; realistically it's *months* and never fully "done".
3. **Touch flight is make-or-break and under-tested.** The current 5-axis model is tuned for **gamepad**, but the product is **mobile touch**. Your own note: *"Z-axis movement on touch is hard. Requires intense tuning."* If flying isn't joyful in 10 seconds on a phone, nothing else matters.
4. **Building the hardest layer before proving fun.** Deep investment in RPCs/class-spawn/netcode, but no evidence yet that fly+shoot+upgrade is *fun* in a juicy single-player slice. Prove fun before paying the multiplayer tax.
5. **Multiplayer-first mobile games die of empty lobbies, not bad code.** Without bots/bot-fill/async/small counts, a perfectly synced arena is a ghost town. Population is a design problem to solve *before* launch.
6. **Effort estimate is optimistic.** ~900–1,200h reaches a rough prototype. A populated, retained, monetized *live* game is realistically 3,000–6,000+h, most of it after "feature complete".
7. **Hardest commercial category.** Original-IP + multiplayer + mobile + competitive — four hard modifiers at once. Doable, but the graveyard is huge and almost none failed on code.

### 1.3 One-paragraph verdict
Strong concept, real gap, excellent process, genuine momentum. The danger is **scope and sequencing**: building a live competitive game's hardest systems before proving, on a phone, that flying + shooting is fun. **Cut to a vertical slice, prove the fun on touch, then earn each additional layer.** That one shift moves this from "likely to stall" to "real shot".

---

## 2. Strategic Reframe — Why "Sequence the Risk"
Re-order work so the riskiest, fun-defining questions are answered first and cheapest. Each gate is blocked by the previous:
1. Does flying feel good on a touchscreen?
2. Is fly + shoot + upgrade fun vs a bot?
3. Is it fun 1v1 online? *(the netcode proof)*
4. Is it fun 3v3 with classes + economy? *(the real core)*
5. Will players return tomorrow? *(retention/population)*
6. Will they pay / tell friends? *(monetization/virality)*

Do **not** build extra modes (racing, CTF, Death Race) or the story until 1–4 are "yes". The whole milestone plan in [[UDAAN_MASTER_PROJECT_PLAN]] maps to answering these in order.

---

## 3. Competition Analysis

**Headline: no single shipping game sits in Udaan's exact spot** — mobile-first, toy-scale, hero-class drones, MOBA-lite economy, multiple modes. Competition is fragmented across four buckets, and the closest design cousins aren't on mobile. That's white space — but it also means there's no proven mobile audience to point at yet.

### Closest in design DNA (the real reference points)
- **Hoverloop** — nearest thing to the PvP mode: arena shooter with customizable hover-drones and Overwatch-style signature abilities (teleport, deathray, jammer, super-speed, shields), Deathmatch/TDM/Invasion. But PC/Xbox only, tiny (~40 reviews), stalled in early access. Proof the combat is fun; caution that it never scaled.
- **Micro Machines World Series** — toy/household scale + role classes (racer/tank/shooter/speedster) + combat *and* racing modes. Cars not drones, not actively live, but basically the Re-Volt-scale + classes + kart-combat idea executed once.
- **Drone Wars** (Steam) — sci-fi drone hero-shooter with class types; smaller and simpler.

### Arcade drone racing-with-items (the racing / racing-with-abilities modes)
- **Drone Fight** (Switch) — item-box power-ups, CPU battle + time attack.
- **Zero-G-Racer** — FPV racing with onboard weapons.
- **Drone Racing Genesis** (arcade) — boost pickups, stylized tracks.
- None combine racing with arena combat or classes — validates "drone Mario Kart" as a *separate* mode only.

### Crowded but tonally opposite — military FPV sims
- **FPV Kamikaze Drone** (Very Positive, ~2,400 reviews), **Ukrainian Fight Drone Simulator**, **Remote Reaper**, **Firehawk FPV**, **FPV Battleground**, **Drones of War**.
- This category is booming, but it's gritty war realism — the opposite of Udaan's tone. It's **SEO/keyword** competition (people searching "drone game"), not design competition. The stylized direction is the separator.

### Mobile arena shooters generally
- **Gridpunk** (3v3 cyberpunk real-time, can deploy drones) — closest mobile-native comp for the *format* (fast 3v3, cross-platform), though soldiers not drones.

### Strategic takeaway
The defensible wedge is the **combination** nobody ships: drone flight feel + hero classes + toy-scale Ghibli world + several modes, on mobile. Hoverloop proves the combat is fun; Micro Machines proves scale+classes+modes works; nobody has put it on a phone. The biggest risk isn't a competitor — it's **scope**: trying to ship a hero-shooter *and* a kart-racer *and* a MOBA-lite economy at once.

### Sources
- Hoverloop — store.steampowered.com/app/613620 ; gamasutra press release (drone combat arena milestone)
- Micro Machines World Series — grokipedia.com/page/Micro_Machines_World_Series
- Drone Wars — store.steampowered.com/app/1155580
- Drone Fight (Switch) — nintendo.com ; Zero-G-Racer — store.steampowered.com/app/2220520 ; Drone Racing Genesis — segaarcade.com
- FPV Kamikaze Drone — store.steampowered.com/app/2707940 ; Firehawk FPV — app/3365170 ; UFDS — app/2862860
- Gridpunk — play.google.com (com.NeverGames.Gridpunk) ; Drone Arena — store.steampowered.com/app/4734320

---

## 4. Brainstorming & Open Ideas

### Story / campaign direction
The Pokémon-route-bosses → "greatest drone pilot" idea is exactly the **Zanzarah** structure and a proven single-player spine. It sequences naturally on top of the PvP arenas already being built. **Defer to post-launch** — but note the hidden synergy: the **combat AI** built for M2 (bot opponents) is the foundation for bot-fill *and* the campaign. Build the AI once, reuse it three ways.

### Game-mode ideas (post-core)
Pure racing, racing-with-abilities ("Death Race"), CTF, Base Assault (Future Cop homage), FFA. Each is a separate game-feel + balance + UI + tutorial problem. Ship **one** mode polished (3v3 TDM), add the rest as live content. They can share the flight controller and classes but each needs its own tuning and maps.

### Class design (Overwatch/LoL layer)
Best differentiator *and* biggest live-ops cost — hero balance on a tiny team is brutal. Start with **3 tight archetypes**: Striker (balanced ✅), Bulwark (tank ✅), Retriever/Support (fast/utility — named in the sheet). 1 active + 1 passive each. Resist adding classes; balance cost is quadratic.

### Open questions to resolve
- Default camera on mobile: FPV vs TPV? (TPV is usually more forgiving on touch.)
- Player count for v1: 1v1 vs 3v3? (Fewer = less netcode pain *and* less population pressure.)
- Netcode middleware: NGO vs Photon Fusion vs **Photon Quantum** (deterministic — strong fit for fast competitive physics). Decide via ADR before M3.
- Monetization: cosmetic-only / battle pass (recommended for competitive fairness).
- Audience/age: playful style attracts minors → plan age gating / privacy (COPPA/GDPR-K) early.

---
type: router
status: active
updated: 2026-07-06
tags: [moc, index, start-here]
---

# Project: Udaan — Kids' Build-and-Play Drone Game

> ⚠️ **PIVOT NOTICE (session 065).** Udaan is now a **build-and-play drone game for children ages 5–10 (learn + play)**: a kid builds a real-ish quadcopter in a **garage** (stealth STEM via visible part trade-offs) and plays **mixed playful missions** (delivery, rescue, race, photo, gentle skirmish) with **non-lethal toy weapons** (enemies power down, never destroyed). It is **NOT** the old adult 3v3 PvP arena shooter. Docs framed around PvP / MOBA / netcode-first are **pre-pivot legacy** (see §Legacy).

> **Vault note:** files use two naming styles — plain folders (`architecture/`, `rubrics/`, `context/`, `tasks/`) and emoji-prefixed display files. Wikilinks use **basename only**.

## ⭐ Single source of truth
**[[Game-Design-Document]]** — current vision, pillars, audience, loop, and how built systems map to the new direction. **If two docs disagree, the GDD wins.**

## 🧭 Read First (current direction)
1. [[Game-Design-Document]] — what Udaan is now.
2. [[Design-Garage-and-Learning]] — the heart: modular components + garage + learning by trade-offs.
3. [[Art-Direction]] — realistic-quad player / creative-enemy split, the T0→T5 tier ladder, palette. (Supersedes 🎨 Aesthetic Canvas.)
4. [[Design-Weapons-and-Tools]] — non-lethal toy weapons + tools + the DIY→advanced mount ladder.
5. [[tasks/active]] + latest [[changelog]] lines — current focus & history.

## 📐 Design specs (systems)
[[Design-Garage-and-Learning]] · [[Design-Weapons-and-Tools]] · [[Design-Onboarding]] · [[Design-SoftLock-UtilityScore]] · [[Design-Audio-and-QA]] · [[Design-Story]] (narrative: hero, arc, Indian-city chapters) · [[Enemy-Roster-and-Campaign]] (re-skin to non-lethal/mixed pending)

## 📊 Reference & telemetry
[[Market-Research-2026]] (audience framing updating to kids-STEM) · `Balance-Model.xlsx` (+ `udaan_runs.csv` telemetry) · [[architecture/Performance-and-Architecture-Audit]] (single-player; netcode parked) · `blender-previews/` (model renders).

## On-demand, by trigger
- Touching gameplay code? → [[code-review]], [[invariants]] (don't break these), [[glossary]]
- Input / controls / flight feel? → [[controller-map]], [[DEC-002_Classic_Input_Manager]]
- Mobile perf? → [[Mobile_Optimization]], [[architecture/Performance-and-Architecture-Audit]]
- Debugging? → [[debugging]], [[gotchas]] · Unsure? → log in [[assumptions]]

## ✍️ Write every session (post-session hook)
- Append exactly one line to [[changelog]] per substantive change. Do not skip.
- Update [[tasks/active]] (move done to [[tasks/done]], don't delete).
- New architectural choice → new ADR in `⚖️ Tradeoffs & Decisions/`.

## Rules of the road (strict)
- **Append-only:** never delete [[changelog]] or ADR entries; mark old ones `status: superseded`.
- **Citations:** cite `file:line` for code, don't paraphrase.
- **Verification:** before claiming a feature works, run [[verification]].
- **Uncertainty:** if unsure, log in [[assumptions]] `confidence: low` — don't guess code.

## 🗄️ Legacy — pre-pivot, reference ONLY (do NOT treat as current direction)
Describe the **old adult 3v3 PvP arena/MOBA**. Kept for salvageable reasoning (aim-assist, input architecture), not direction:
- [[🎮 Combat & Game Vision]] — old "north-star" (Future Cop MOBA). Aim-assist/input reasoning still valid.
- [[📋 Master Plan]], [[UDAAN_MASTER_PROJECT_PLAN]] — old PvP/netcode roadmap.
- [[UDAAN_Analysis_and_Competition]] — old PvP competitive framing (treated kids as a *risk*, not the target).
- [[📋 API Contract Sandbox]] — PvP backend (only if multiplayer un-parks).
- [[🏗️ Architecture]] — netcode-first map (multiplayer parked; see GDD §11).
- [[💰 Economy_Balance]] — "scrap-per-kill" stub → superseded by the **bolts** economy in [[Design-Garage-and-Learning]].
- [[🎨 Aesthetic Canvas]] — now UI-only; [[Art-Direction]] is the art source of truth.
- Netcode ADRs [[DEC-001_Netcode_NGO]], [[DEC-003_Network_Stack_Reevaluation]] — dormant while multiplayer is parked.

## 🗂️ File index (what exists today)
- **Current design:** `Game-Design-Document`, `Design-Garage-and-Learning`, `Design-Weapons-and-Tools`, `Art-Direction`, `Design-Onboarding`, `Design-SoftLock-UtilityScore`, `Design-Audio-and-QA`, `Enemy-Roster-and-Campaign`
- **Reference:** `Market-Research-2026`, `Balance-Model.xlsx`, `architecture/Performance-and-Architecture-Audit`, `architecture/glossary`, `architecture/controller-map`, `architecture/invariants`
- **Process:** `context/conventions`, `context/gotchas`, `context/assumptions`, `context/code-review`, `rubrics/*`, `🧠 Technical Rubrics/*`
- **Legacy (pre-pivot):** `🎮 Combat & Game Vision`, `📋 Master Plan`, `UDAAN_MASTER_PROJECT_PLAN`, `UDAAN_Analysis_and_Competition`, `🏗️ Architecture`, `📋 API Contract Sandbox`, `💰 Economy_Balance`, `🎨 Aesthetic Canvas`, ADRs `DEC-001/003`
- **Tasks / log:** `tasks/active`, `tasks/done`, `changelog`, `🪵 Session Logs/`

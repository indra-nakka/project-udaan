---
type: design
status: brainstorm
updated: 2026-07-01
tags: [vision, combat, moba, single-player, ai, reference]
---

# 🎮 Combat & Game Vision (brainstorm)

**Role:** The north-star for what Udaan *is* as a game — objective, fighting, the demo we build to prove it, and how it scales. Living brainstorm; nothing here is locked. Sibling docs: [[📋 Master Plan]], [[💰 Economy_Balance]], [[controller-map]], [[🏗️ Architecture]].

Primary reference: **Future Cop: LAPD** (EA, 1998) — a gamepad third-person shooter with **aim-assist/lock-on** targeting and a proto-MOBA mode ("Precinct Assault") credited as an inspiration for DotA/LoL. Retrospective that prompted this: Josh Strife Hayes, "Was it Good? — Future Cop: LAPD."

---

## 1. What we adapt from Future Cop: LAPD

| Future Cop mechanic | How it maps to Udaan |
|---|---|
| **X1-Alpha transforms**: fast hover *pursuit* mode ⇆ slower *combat* mech | We already have a version: **NoseAim** (fast traverse/strafe) ⇆ **FreeAim** (combat). Lean into it — traverse fast, then "settle" into a combat stance with tighter handling + full aim. |
| **Aim-assist / lock-on** (no mouse; you steer, the game helps you hit) | **This is the core targeting model for Udaan** (see §2). Confirmed by your note: the reference plays great without a mouse. |
| **Primary + selectable secondary weapons** (MG + missiles/mortars/etc.) | We have bullets + rockets. Expand to a small arsenal with a selectable secondary (mines, homing swarm, mortar, shield-breaker). |
| **In-match economy** to buy units/upgrades | Your **scrap economy** already exists — spend scrap mid-match on upgrades / deployables. |
| **Precinct Assault** (bases, capture neutral outposts/turrets, buy & deploy AI units to push the enemy base, defensive units, win by breaching base) | The long-term **PvE/PvP objective mode**. Capturable outposts that spawn allied drones; push lane(s) into the enemy core. Fits your classes (Striker/Bulwark) + NGO plans. |
| **Crime War** (story missions: destroy / defend / escort / boss; co-op with a *shared* life bar) | The **single-player campaign** shape: varied mission objectives + boss fights. Co-op later. |
| **Sky Captain** (single boss AI in a superplane, tuned across difficulty tiers) | Our single-player **boss AI** template + difficulty scaling. |
| **Distinct arenas** (Griffith Park, LAX, Long Beach…) | Themed arenas; we already build arenas procedurally, so variety is cheap early. |

What we deliberately **don't** copy: fixed camera quirks, PS1-era mission opacity, and mouse-less-but-clunky menus. We keep our device-agnostic input + touch-first HUD.

---

## 2. Targeting model — aim assist (the make-or-break)

Decision direction: **soft lock-on + assist, manual override available.**

- **Soft lock:** the reticle finds the best enemy in a forward cone (nearest / most-centered). Primary fire is **magnetized** onto the locked target (hitscan or lightly-homing bullets), so "point in the general direction" connects.
- **Hard lock (secondary):** hold to lock, release to fire homing missiles at the locked target.
- **Manual free-aim** stays as the skill/override layer (right-stick reticle we already built) and for "no target" situations.
- **Assist strength is a difficulty/accessibility dial** (full assist = easy mode; light assist = high skill). This is exactly how Future Cop stayed playable without a mouse.

Why this fits Udaan: touch + controller both struggle with precise 3D aim; assist makes combat *feel* skillful without demanding twitch precision. It also makes enemy AI viable (they use the same assist).

---

## 3. The Demo — one comprehensive challenge ("vertical slice")

Goal: a single, self-contained mission that exercises **most** systems, so we have something to show, feel, and perfect. Working title: **"Sky Sentinel."**

Flow (≈4–6 min):
1. **Traverse** — fly a short hoop/gate approach to the arena (proves flight + traversal; reuses the race circuit).
2. **Skirmish** — clear 2–3 waves of AI enemy drones using lock-on primary + rockets; scrap drops from kills (proves combat + aim assist + economy).
3. **Capture** — take 2 neutral **outposts** that then spawn allied drones to fight alongside you (proves the Precinct-Assault seed + ally AI).
4. **Objective** — defend a point / escort an ally for 60s against a push (proves objective variety + defensive play).
5. **Spend** — a mid-mission upgrade beat: spend collected scrap on a quick buff or a deployable (proves the economy loop matters *in* combat).
6. **Boss** — a scripted multi-phase boss drone (proves boss AI + a memorable finale).

If a first-timer can complete "Sky Sentinel" and *want to replay it for a better time/score*, the core loop works. Everything else is content on top.

---

## 4. Expansion paths (after the demo works)

- **Content scaling** (cheap-ish): more arenas, enemy archetypes, weapons, drone classes, missions.
- **Campaign** (Crime War shape): a run of missions with escalating objectives + bosses; optional co-op with shared-fate tension.
- **Objective mode** (Precinct Assault shape): the flagship — PvE vs an AI commander first, PvP later. Outposts, lanes, deployables, base breach.
- **Meta progression:** persistent upgrades/unlocks between matches (ties to scrap economy + upgrade tree you have).
- **Racing** (already built): keep as a time-trial side mode + onboarding/tutorial.

Sequencing logic: **single-player first** (demo → campaign → PvE objective mode), multiplayer **parked** until the SP game is fun. A good SP game de-risks everything and is showable to players/investors.

---

## 5. Scale & resources (honest view)

- **The demo** ("Sky Sentinel") is achievable with the current setup + AI assistance: it's mostly systems we already have plus enemy AI, lock-on, and one boss. Programmer-art is fine for the slice.
- **Beyond the demo, the cost centers are content and polish**, not core code:
  - **Art**: drone/enemy models, environments, VFX (tracers, explosions, shields), UI art.
  - **Audio**: weapons, engines, music, hit feedback (huge for "feel").
  - **Design/balance**: mission design, difficulty tuning, economy balance.
  - **Netcode**: only when multiplayer returns (already have NGO groundwork + [[DEC-003_Network_Stack_Reevaluation]]).
  - **QA / device testing**: real mid-range phones, controllers.
- **Team to go past a demo** (rough): 1–2 gameplay engineers, 1 3D/tech artist, 1 VFX/audio (or contract), 1 designer, part-time QA. That's where **funding / more developers** becomes the gate.
- **Recommendation:** build the vertical slice solo/AI-assisted → use it to pitch (players + funding) → staff up for content. The slice *is* the fundraising artifact.

---

## 6. Enemy AI — how hard, really?

**Good news: our architecture makes basic enemy AI cheap.** Input is device-agnostic (`IFlightInput` → `FlightInputRouter`). An **AI pilot is just another input source**: a "brain" that outputs a `FlightInputState` (steer/throttle/aim/fire) each frame. Enemy drones reuse the *exact* `DroneFlightController` + `DroneWeapon` the player uses — no separate movement/shooting code. This is a big cost saver.

Difficulty tiers (build in this order):
- **Tier 0 — Dummies (done):** static/orbiting targets. ✅
- **Tier 1 — Basic combatant (days):** steering behaviors (seek/pursue/strafe/keep-distance) + a small FSM (Patrol → Engage → Evade → Reposition), fires when the player is roughly in front (uses the same aim assist). Genuinely fun enough for the demo.
- **Tier 2 — Competent (1–2 weeks):** lead/predictive aim, altitude play, break-off when low HP, simple formations, ability use.
- **Tier 3 — Boss / commander (scripted):** multi-phase boss patterns; and later a "Sky Captain"-style AI *commander* that buys/deploys units in the objective mode (this is the harder, strategic AI — but it's post-demo).

Verdict: **Tier 1 enemies + one scripted boss are very achievable now** and are all the demo needs. Strategic commander AI is the only genuinely hard part, and it's deferred to the objective mode.

---

## 7. Open questions to resolve next
- Lock-on feel: full auto-track vs "assist nudge"? (Prototype both, test — same as we did for flight.)
- Demo enemy count / wave pacing / boss identity.
- Art direction for readability (enemies must pop — see the magenta-target visibility lesson).
- Does the demo use the race circuit as the "traverse" phase, or a bespoke arena?

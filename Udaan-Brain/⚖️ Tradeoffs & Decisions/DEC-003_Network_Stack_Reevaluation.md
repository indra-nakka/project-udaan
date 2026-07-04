---
type: adr
status: PROPOSED
date: 2026-06-30
supersedes: none
tags: [decision, networking]
---

# DEC-003: Network Stack Re-evaluation — Stay on NGO, or Switch?

- **Status:** PROPOSED — leaning **SWITCH to Photon Fusion**. Confirm with a spike before M3. Updates/possibly supersedes [[DEC-001_Netcode_NGO]].
- **Date:** 2026-06-30

## Context
Udaan is a **fast, 6-DoF, physics-driven, competitive mobile PvP** game. That combination is the single hardest networking case in the medium. The current stack is **Unity Netcode for GameObjects (NGO)** with **client-authoritative** movement (see [[DEC-001_Netcode_NGO]]). Two problems are already visible:
1. NGO has **no built-in client-side prediction & reconciliation** — only "client anticipation" (`AnticipatedNetworkTransform`), and Unity explicitly recommends *only advanced users* build a full prediction loop on top. Unity physics is **non-deterministic**, which makes that even harder.
2. Client-authoritative movement is **cheatable**, and combat is currently broken in multiplayer (see [[code-review]]: projectiles aren't networked).

For a solo/small team, hand-rolling prediction on NGO is the deepest risk in the whole project.

## Options

### A. Stay on NGO
- ✅ First-party, free, already integrated; fine for co-op / slow / small games.
- ❌ Must **build prediction/reconciliation yourself** for acceptable fast-physics PvP (months, advanced-only). Non-deterministic physics fights you. Weak anti-cheat with client authority.
- **Viable only if** we descope to casual feel and/or 1v1 with smaller, slower arenas.

### B. Switch to **Photon Fusion** *(recommended)*
- ✅ **Built-in** client-side prediction, snapshot interpolation, lag compensation, rollback — exactly the hard part, solved.
- ✅ Keeps **Unity GameObjects + Rigidbody/PhysX**, so the existing flight model ports with far less rewrite than Quantum.
- ✅ Topologies for our case: **Server/Host mode** = true server authority (fair, anti-cheat); **Shared mode** = cheap, mobile/WebGL-friendly (less authoritative).
- ✅ Pricing fits indie: **100 CCU free (~40k MAU)**, 200 CCU $95/yr, 500 CCU $125/mo, up to 2000 CCU $500/mo (~$0.50/CCU).
- ❌ Third-party dependency + CCU cost at scale; a real but bounded migration.

### C. Switch to **Photon Quantum 3** *(higher ceiling, higher cost)*
- ✅ **100% deterministic** predict/rollback → frame-perfect, **cheat-proof** (inputs-only over the wire), built-in **bot SDK** (directly serves our bot-fill + AI-campaign need), runs physics-heavy games on mobile. Same CCU pricing; free for development.
- ❌ Requires Quantum's **own deterministic ECS physics** — the Unity `Rigidbody` flight model must be **rewritten** in Quantum's fixed-point physics/ECS. Steepest learning curve and biggest port.
- **Best if** we commit to a serious competitive/esports posture with strong anti-cheat and heavy bot use, and accept the rewrite.

## Decision (proposed)
**Lean SWITCH → Photon Fusion**, because it directly removes the project's deepest technical risk (prediction for fast physics) while preserving the existing Unity physics flight model. Treat **Quantum as the upgrade path** if, after M2, competitive integrity + anti-cheat + bots become first-order priorities and we're willing to invest in the deterministic rewrite. **Keep NGO only if we consciously descope** to casual/1v1.

## Why decide now (even though netcode is M3)
- Migration cost is **lowest now** — the netcode surface is a thin sandbox (~12 scripts, mostly logic). It only grows.
- It dictates how M2 combat is built (server-authoritative, pooled, networked projectiles per [[code-review]]). Building M2 combat twice is the waste to avoid.

## Validation before committing (the spike)
- Build the **M1 flight model transport-agnostic** (separate flight/combat *logic* from networking) so the stack can be swapped.
- At start of M3, spend a **timeboxed spike** porting 1v1 drone movement to Fusion (Host mode). Measure: feel under 150ms + 2% loss, rubber-band, dev effort. If Fusion's prediction makes it feel good with reasonable effort → commit and supersede DEC-001. If competitive fairness/bots dominate → evaluate Quantum instead.

## Consequences
- If approved: new ADR supersedes [[DEC-001_Netcode_NGO]]; [[Unity_NGO_Patterns]] is replaced/renamed with the chosen stack's patterns; [[🏗️ Architecture]] authority model updates; [[assumptions]] netcode row resolves.
- Until the spike, **do not build more NGO-coupled netcode**; keep combat logic transport-agnostic.

## Related
[[DEC-001_Netcode_NGO]] · [[code-review]] · [[Unity_NGO_Patterns]] · [[UDAAN_MASTER_PROJECT_PLAN]] (Phase 2) · [[assumptions]]

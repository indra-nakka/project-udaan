# DEC-001: Netcode for GameObjects (NGO)

- **Status:** APPROVED *(revisit before M3 — see Consequences)*
- **Date:** 2026-06-16

## Context
Choosing the multiplayer framework for a mobile PvP arena game with fast, free-flying drones. Needs responsive controls on mobile and a manageable learning curve solo.

## Decision
Use **Unity Netcode for GameObjects (NGO)** with **client-authoritative** ownership for drone movement (NetworkTransform/Rigidbody, Authority = Owner) so local control feels instant. Gameplay state (currency, health, damage, upgrades) remains **server-authoritative**.

## Alternatives considered
- **Photon Fusion** — mature, strong for fast physics; external dependency/cost.
- **Photon Quantum** — deterministic rollback, excellent for competitive fairness + anti-cheat; steeper learning curve.
- **Hand-rolled netcode** — rejected (see [[invariants]]: don't hand-roll).

## Consequences
- ✅ Responsive feel, first-party Unity integration, fastest path to a sandbox.
- ⚠️ Client-authoritative movement is **cheatable** and prone to **rubber-banding** for fast 6-DoF flight without client-side prediction + reconciliation.
- 🔁 **Open risk** (tracked in [[assumptions]]): if M3's 1v1 netcode test shows desync/cheating, reconsider prediction/reconciliation or migrate to **Photon Quantum**. If superseded, mark this ADR `status: superseded` and add DEC-00N.

## Related
[[Unity_NGO_Patterns]] · [[🏗️ Architecture]] · [[UDAAN_MASTER_PROJECT_PLAN]] (Phase 2)

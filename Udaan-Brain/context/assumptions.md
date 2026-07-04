# 🎲 Assumptions Log

**Role:** Running list of things we're acting on but haven't verified. When code or design relies on an unproven belief, log it here with a confidence level instead of guessing silently. Revisit and resolve (✅ confirmed / ❌ refuted → move the lesson to [[gotchas]] or [[invariants]]).

> Append-only in spirit: don't delete; mark `resolved:` with the outcome.

| Date | Assumption | Confidence | Status / Resolution |
|---|---|---|---|
| 2026-06-26 | Custom axes `TargetYaw`/`TargetPitch` are defined in the legacy Input Manager, enabling true dual-stick analog aim. | med | open — verify in Project Settings → Input Manager |
| 2026-06-30 | Flying will feel good on a **touchscreen** (current model is tuned for gamepad). | **low** | open — the M1 gate; must validate on a real phone |
| 2026-06-30 | NGO client-authoritative movement will be acceptable for fast 6-DoF PvP without rubber-banding or trivial cheating. | **low** | ⚠️ refuting — NGO has no built-in prediction (Unity: "advanced users only"); see [[DEC-003_Network_Stack_Reevaluation]] (leaning switch → Photon Fusion). Confirm via M3 spike. |
| 2026-06-30 | ~900–1,200 hrs reaches a playable prototype; a live game is 3,000–6,000+ hrs. | low | open — track actuals against [[UDAAN_MASTER_PROJECT_PLAN]] §4 |
| 2026-06-30 | 3v3 Team Deathmatch is the right first networked mode (vs 1v1). | med | open — revisit after M2 fun-test |

## How to use
- Add a row the moment you write code on an unproven belief (DFS Uncertainty Gate, [[bfs-dfs]]).
- Low-confidence assumptions blocking a milestone become explicit **gates** in [[📋 Master Plan]].
- When resolved, note the outcome here and propagate the lesson (a confirmed rule → [[invariants]]; a footgun → [[gotchas]]).

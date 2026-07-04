---
type: architecture
status: active
updated: 2026-06-30
tags: [invariants, rules, must-hold]
---

# 🧱 Architecture Invariants

**Role:** The hard rules that must *never* be silently broken. If a change violates one of these, STOP and either redesign or write an ADR in `⚖️ Tradeoffs & Decisions/` that explicitly supersedes the rule. Every session's verification step checks against this file.

> Sibling files: *what must hold* = here · *traps that bite* = [[gotchas]] · *how to write code* = [[conventions]] · *open uncertainties* = [[assumptions]].

## 🔐 Networking & Authority
- **Movement is client-authoritative** (NetworkTransform/Rigidbody, Authority = Owner) — chosen for responsive feel. See [[DEC-001_Netcode_NGO]].
- **Gameplay state is server-authoritative.** Currency, health, damage, upgrades are mutated **only** on the server. Guard every such path with `if (!IsServer) return;`.
- **Networked objects are spawned/despawned via `NetworkObject`**, never plain `Instantiate`/`Destroy`. Use `.Spawn()` after instantiating and `.Despawn()` to remove. A client must never `Destroy()` a networked object.
- **Ownership-gated logic** (camera, local input) guards with `if (!IsOwner) return;`.

## 🧮 Data & State
- **Every `NetworkVariable` and Core gameplay variable must have a row in [[glossary]]** (Type / Scope / Authority) created or updated in the *same session* it is added or changed.
- **Class/weapon/upgrade data lives in ScriptableObjects**, not hardcoded values. Code reads stats from the data asset; inspector values are fallbacks only.

## 🎮 Input
- **Active Input Handling = "Both"** in Player Settings (legacy `Input.GetAxis` + New Input System coexist). See [[DEC-002_Classic_Input_Manager]].
- **The canonical flight scheme is the 5-axis model in [[controller-map]]** (RT thrust, L-stick altitude+strafe, R-stick aim pitch/yaw). Do not redefine axes without updating both [[controller-map]] and [[glossary]].

## 📱 Mobile / Performance
- **Object pooling is mandatory** for projectiles, VFX, and frequently spawned objects. No per-shot `Instantiate`/`Destroy` in gameplay. See [[Mobile_Optimization]].
- **Target: 60fps on a 3-year-old mid-range Android.** Performance budget is a feature, not an afterthought.
- **URP only**, mobile-tuned render pipeline.

## 🗂️ Process
- **Append-only:** never delete entries from [[changelog]] or the decisions folder. Mark superseded items `status: superseded`.
- **Touch is the shipping target.** Gamepad/keyboard are dev conveniences (Phase pre-5); a feature isn't "mobile-done" until validated on a touchscreen.

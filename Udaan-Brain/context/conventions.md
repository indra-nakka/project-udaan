# 📐 Conventions — Code & Commit Style

**Role:** *How* we write code and history so the project stays consistent across sessions. (Visual/UI conventions live in [[🎨 Aesthetic Canvas]]; this file is code + git only.)

## C# Naming (as used in `udaan-client/Assets/Scripts`)
- **Public fields:** `camelCase` — `forwardThrust`, `maxHealth`, `scrapCount`.
- **Private fields:** `camelCase` — `rb`, `playerEconomy`, `isManuallyOverridingHover`.
- **Methods / events:** `PascalCase` — `TryPurchaseUpgrade()`, `InitializeClassData()`, `OnSpeedModifierUpgraded`.
- **ScriptableObject types:** `PascalCase` ending in `Data` — `DroneClassData`, `DroneUpgradeData`.
- **Group inspector fields with `[Header("...")]`**; expose tunables, keep derived/runtime state private.

## Unity / NGO Patterns
- Networked behaviours inherit **`NetworkBehaviour`**; synced state uses **`NetworkVariable<T>`**.
- Initialize synced data in **`OnNetworkSpawn`**, not `Awake`/`Start`.
- Server-authoritative actions guarded by **`if (!IsServer) return;`**; owner-only logic by **`if (!IsOwner) return;`**.
- **Defensive null-guards** on every inspector/asset reference, with a clear `Debug.LogWarning`/`LogError` fallback (see `DroneFlightController.Awake`).
- Editor-only test hooks via **`[ContextMenu("...")]`** (see `PlayerEconomy.DebugPurchaseTestAsset`).
- Logs use interpolation: `Debug.Log($"...{value}")`. Prefix alerts so they're greppable.
- Data-driven first: read stats from ScriptableObjects; inspector values are fallbacks only ([[invariants]]).

## File Organization
- Scripts → `Assets/Scripts/` · class assets → `Assets/DroneClasses/` · prefabs → `Assets/Prefabs/` · materials → `Assets/Materials/`.
- One primary `MonoBehaviour`/`NetworkBehaviour` per file, filename = class name.

## Git Commits
- Format: **`TYPE(scope): summary`** or **`TASK-ID: summary`** (matching history) — e.g. `feat(flight): ...`, `ECON-04: ...`, `DRONE-09: ...`, `INFRA: ...`.
- One logical change per commit. Binary assets via **Git LFS** only.
- Every substantive change also gets a one-line entry in [[changelog]] (append-only).

## Documentation (the Brain)
- New files go in the matching folder with a **`**Role:**` line** at the top stating their single purpose.
- Key docs carry YAML **frontmatter** for Obsidian sorting/filtering: `type:` (router/strategy/architecture/adr/review/moc), `status:` (active/living/PROPOSED/superseded), `updated:` (YYYY-MM-DD), `tags: [...]`.
- Cross-link with `[[basename]]` wikilinks (resolve regardless of folder/emoji — see [[UDAAN_ROUTER]]).
- Follow the session loop in [[UDAAN_ROUTER]] every session.

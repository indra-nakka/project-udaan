# Udaan — Map Design System & Guidelines

_Session 066. Maps are the **#1 connection between the player and the cities/environments** — so map detailing is a constant, high-priority task. We can't hand-build every level, and pure random generation can't give us **designed encounters**. The answer is a **hybrid: authored layout skeletons + rule-based procedural detailing + data theming.** This doc is the ruleset so it scales._

## The shift

- **From:** fully-random `ParkMapGenerator` (props scattered, enemies/Core/outposts spawned at random angles).
- **To:** an **authored layout** (fixed **spawn posts** + landmarks + zones) whose **detailing is filled procedurally** and whose **look is swapped by city theme.** Same skeleton → many dressed levels.

## The hybrid pipeline (3 layers)

1. **Layout — authored (the skeleton).** A human (or a small tool) places the bones: **spawn posts** (player start, ally spawns, enemy spawn points, Core, boss arena, outposts, pickups), major **landmarks**, **lanes/zones**, and the boundary. Stored as **data** — a `MapLayout` ScriptableObject or in-scene marker objects — so it's reusable and tweakable, and drives designed encounters.
2. **Detailing — procedural, rule-based (the dressing).** A themed detailer fills **props/decoration within the authored zones** (density, prop-sets, scatter) from a **seed** (reproducible). This is where `ParkMapGenerator` evolves to — it stops choosing *where the fight happens* and just *dresses* the authored zones.
3. **Theming — data (the city connection).** Per-city **prop-sets + palette + landmark swaps** reskin the same layout (Hyderabad rooftops vs Mumbai seafront). This is how one system serves the whole Pokémon-style city ladder.

## Spawn-post system (the immediate build)

Replace random spawn math with **named markers/data**. A `SpawnPost` = `{ Transform, PostType }` where `PostType ∈ { PlayerStart, AllySpawn, EnemySpawn, Core, BossArena, Outpost, Pickup, Landmark }`. The bootstrap/`MissionDirector` **read the posts** instead of computing angles — enabling designed placement (a boss arena that's actually an arena; outposts at meaningful spots; safe player start). Random remains a *fallback* if a level has no posts.

## Design guidelines (the constant-task ruleset)

- **Readability first.** Clear sightlines, one obvious central landmark, distinct lanes. The Core/objective must be findable from anywhere (beam/marker rules already in place).
- **Three vertical layers** (it's a flying game): ground clutter, mid-height cover, open **air lanes**. Reward flying high *and* threading low.
- **Cover & verticality, not mazes.** Give cover and landmarks, but avoid tight corridors that trap the AI (respect the enemy obstacle-avoidance limits).
- **Scale discipline.** Arena radius, prop scale vs drone size, and **spacing between spawn posts** (no spawn-camping; enemies arrive from off-ish, boss arena stays open).
- **Spawn safety.** Player start clear + a moment to orient (launch stealth already exists); enemy/boss spawns not on top of the player.
- **Performance budget.** Cap prop counts / draw calls per level; share materials; use the target registry + pooling. Mobile frame budget is the ceiling.
- **Theming discipline.** Every prop tagged to a **city set**; one signature **landmark per city** (Charminar, Gateway of India…). The theme swaps sets, not the layout.
- **Pacing.** Place objectives for a good rhythm; a readable "golden path" with optional verticality/secrets (supports the show-don't-tell, replayable design).

## Migration plan (demo → system)

1. **SpawnPost + MapLayout** data types; `MissionDirector`/bootstrap read posts (fallback to current random).
2. Convert `ParkMapGenerator` into a **zone detailer** driven by the layout (dress authored zones, don't decide encounters).
3. Author **one designed park layout** for the demo (fixed player/ally/enemy/Core/boss/outpost posts) — gives us dedicated, tunable placement.
4. Add a **city theme** data object (prop-set + palette) — start with the park; prove the swap.

## Phase +1 (city levels)

- A **tile/block or spline-based city generator** (streets, buildings, rooftops) themed per city, with hand-authored landmark set-pieces dropped in — the scalable way to build Hyderabad→Mumbai without hand-placing every building.
- Reuse the layout+detailer+theme split; cities = new theme sets + new landmark prefabs + new layouts.

## Open questions

- Authoring UX: in-scene marker objects (easy, visual) vs a `MapLayout` ScriptableObject (portable) vs a small custom editor tool. (Lean: scene markers now, tool later.)
- How much procedural vs hand-placed per city (likely: procedural fabric + hand-placed landmarks/set-pieces).
- Do outposts/pickups stay semi-random within zones, or fully authored? (Lean: authored anchor + small random jitter.)

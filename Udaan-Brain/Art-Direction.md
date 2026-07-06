# Udaan — Art Direction

_Locked session 064. Pairs with `🎨 Aesthetic Canvas.md` (UI) and drives all Blender modeling (`build_models.py`, tasks #77–79)._

## Pillars

1. **Ghibli retro flying-machines.** Not sleek sci-fi — think *Nausicaä* / *Castle in the Sky*: brass, bronze, wood, riveted panels, **exposed propellers**, canvas wings/sails. Handcrafted, warm, a little worn. Machines feel *built by people*, not printed.
2. **Warm "sunset Ghibli" world.** Golden-hour light, soft warm palette, painterly sky. Cozy even in combat.
3. **Low-poly now, shaders later.** Build clean low-poly **silhouettes + flat material colors** now (great mobile perf, drops into the greybox). Toon/painterly shaders, rim light, outlines, and a painted sky come in a later art pass — silhouette + palette carry the look until then.
4. **Readability first.** Shape and value read instantly at drone-combat speed and distance. Faction identity survives colorblindness (shape language, below).

## Player vs enemy — the core split (updated)

- **PLAYER drone = a REALISTIC QUADCOPTER.** It must *look like it can fly*: four motors at symmetric arm tips, **big props with a believable prop-to-body ratio**, real materials. Cute and likable, grounded in real market drones. Physics-plausible. (Ref: user's DIY plywood/stick quad photos.)
- **ENEMIES = creative / fantasy.** This is where the invented stuff lives — the junk trash-can bot, brass flying-machines, insectoid archetypes. Enemies can be weird; the player is real.

## Player drone progression ladder — the visual progress bar

**The player's quad IS the progression**, grounded in the real drone market (see `Market-Research-2026.md` / quad reference). It levels along three axes: **materials** (raw → molded → carbon/graphite), **camera** (none → fixed → 3-axis gimbal → multi-sensor), **props** (open 2-blade → integrated-guard → open 3-blade → **ducted/enclosed**). Keep the four-motor symmetric layout and correct prop/body ratio at *every* rung so it always reads as flyable. Distinct model per tier, shared pivot/scale so they hot-swap in the spawner.

| Tier | Name | Real analog | Key visual cues |
|------|------|-------------|-----------------|
| **0** | **The Scrapper** | popsicle-stick / Crazyflie / DIY plywood quad | laser-cut plywood X-frame, gold motor bells, big black 2-blade props, exposed blue battery + green board, zip-ties, no gimbal. *Handmade but real.* |
| **1** | **Toy** | Holy Stone / Potensic sub-$50 | molded plastic body, bright color, **circular prop guards** dominate silhouette, fixed pinhole cam, snap-on props |
| **2** | **Consumer** | DJI Tello / **DJI Neo** | clean seamless matte shell, **integrated prop guards**, small fixed/1-axis cam, correct proportions — looks like a *product* |
| **3** | **Prosumer** | **DJI Mini 4 Pro / Mavic** | iconic **folding-arm** silhouette, aerodynamic body, **prominent 3-axis gimbal ball on the nose**, obstacle-sensor "eyes", open 3-blade props |
| **4** | **Freestyle/FPV** | 5" carbon X / cinewhoop | bare **woven carbon** True-X (or **ducted** cinewhoop), anodized hardware, **deliberately exposed stacked electronics**, action-cam bolted on, antennas — *serious/aggressive* |
| **5** | **Ascendant** | Skydio X10 / ducted-fan concepts | dense machined graphite/matte body, **ducted/enclosed rotors** (near "propless"), multi-lens + thermal + radar **sensor cluster**, autonomous, almost aircraft-grade |

Signals that read **cheap/handmade** (T0–1): raw PCB/plywood, exposed battery on wires, visible seams, toy colors, bolted-on guard rings, mismatched arms, 2-blade push-fit props, no gimbal.
Signals that read **premium/hi-tech** (T3, T5): seamless molded/carbon body, matte graphite, a 3-axis gimbal ball (single strongest premium cue), folding arms, sensor eyes, ducted/enclosed rotors, everything flush.
Tier 4 is a *sideways* "enthusiast" read: exposed guts + premium carbon/anodized = performance, not cheapness.

Build order: **T0 (done) → T2 → T3 → T5** (clearest progression beats) → fill T1, T4. Weapon mounts also visibly upgrade per tier (zip-tied pipe → clipped pod → gimbal-slaved turret → flush energy emitter).

## Faction shape language (readability rule)

| | Silhouette | Materials | Motion feel |
|---|-----------|-----------|-------------|
| **Player / allies (team 1)** | **Realistic quadcopter** — symmetric X-frame, four motors, big believable props; clean & readable, cute proportions | Real materials per tier (plywood→carbon→graphite), cool blue accent + status LED | Stable, precise |
| **Enemy (team 2)** | **Angular, spiky, insectoid, asymmetrical** — sharp blades, thin legs, multiple buzzing rotors | Rusted iron + charcoal, sickly red/purple glow ("blighted machine") | Twitchy, aggressive |
| **Boss** | Large **ornate brass flagship** — multiple props, ornament, a clear weak-point motif | Polished brass + banners, bright core | Ponderous, imposing |

## Palette (hex — extends the Aesthetic Canvas UI colors)

**Machines (friendly):** brass `#C9A227` · bronze/wood `#8C6B3F` · copper `#B87333` · canvas cream `#EFE3C8` · faded-red trim `#C1573C` · friendly glow `#4CAF50` / soft teal `#3E7C7B`
**Machines (enemy/blighted):** rusted iron `#6E4B3A` · charcoal `#3A3238` · warning glow `#A63D5B` (magenta-red) · ember `#E0662E`
**World / sunset:** sky-gold `#F4C06A` · coral `#E8895A` · dusk violet `#7C6A8A` · warm stone `#D9B38C` · grass `#8FB56B` · foliage `#5E8C55`
**UI (from Aesthetic Canvas):** scrap/gold `#FFD700` · health/friendly `#4CAF50` · panels `#000000` @ 40%

Gameplay-critical colors stay readable regardless of palette: **capture/CORE beam blue `#4C9EFF`**, outpost owner tint white→blue(you)→red(enemy), lock yellow. (These override "warmth" where clarity matters.)

## Per-asset silhouette briefs

Tris are targets for the low-poly pass (flat-shaded, per-material color, FBX export into `Assets/Art/Models`).

**Player drone** — a **realistic quadcopter** per the progression ladder above (T0 = DIY plywood X-quad with big black props, exposed battery/board; climbing to consumer, folding-gimbal prosumer, and ducted-sensor advanced). Always four motors + believable props. See the ladder table for per-tier cues.

**Allies** — same quad family as the player, plainer, blue team tint.

**Enemy — Interceptor** (~300–500) — small angular dart, swept blades, twin buzzing rotors. Fast-looking. Rusted iron + red glow.

**Enemy — Sniper** (~500–700) — long, spindly, a single big brass **lens/scope "eye,"** thin insect legs, keeps distance. Menacing stalker.

**Enemy — Bulwark** (~700–900) — bulky, beetle-like, a heavy **front brass shield plate** (the flank-me weak point), stubby rotors. Slow, tanky.

**Enemy — Kamikaze** (~250–400) — round bomb-body wasp: glowing core (the tell), spiky fins, single frantic rotor. Clearly "about to blow."

**Boss** (~2–3k) — ornate brass **flagship-drone**: central hull, ring of propellers, banners/ornament, a bright exposed core as the weak-point motif.

**Core** (defend objective) — a warm glowing **brass reactor / crystal in a wooden cradle**, but keep the tall **blue beacon beam** for gameplay readability (warm housing, cool beam).

**Outpost** — a **windmill/beacon tower**: brass frame + canvas sails, an orb on top that reads white (neutral) → blue (yours) → red (enemy). Fits the flying-machine world and the capture mechanic.

**Park props (stylized, keep the layout)** — restyle existing greybox into the palette: slides/towers → painted wood + canvas flags; jungle gyms → riveted brass climbing frames; trees → rounded Ghibli canopies (`#5E8C55` / `#8FB56B`) on wooden trunks; fence → low wooden posts. Same collision footprints, new look.

## Poly / perf budget (mobile)

Player/allies ≤ ~1.2k tris · standard enemies 300–900 · boss ≤ 3k · props 100–400. Flat shading, one material per color zone (few materials → batching). No textures in this pass; color lives in materials/vertex color.

## Staged shader plan (later art pass)

1. **Toon ramp** (2–3 band) + **rim light** for the storybook read.
2. **Warm directional "sunset" key light** + soft ambient.
3. **Outline pass** (inverted-hull or post) for the illustrated edge.
4. **Painted sky** dome + subtle fog for depth.
5. Optional: gentle propeller blur, canvas flutter.

## Open

- Exact sunset-sky treatment (gradient dome vs painted skybox) — decide at the shader pass.
- Whether allies get a distinct silhouette from the player or just a tint (currently: same family, tint).

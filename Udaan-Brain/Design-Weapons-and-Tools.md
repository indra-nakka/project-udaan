# Udaan — Playful Weapons & Tools (design)

_Session 065. For ages 5–10, "weapons" are **toy, non-lethal gadgets** a kid would bolt onto a drone — foam, water, bubbles, paint, snowballs. No guns, no gore. Enemies don't die; they get **bonked/soaked/bubbled → gently powered down**. Mounts are visible and progress with the drone tier._

## Principles

- **Non-lethal, playful, harmless.** The fantasy is a kid's maker project, not a war machine.
- **Enemy "defeat" is friendly:** hit bots get soaked/painted/bubbled, sputter, and **float gently down / power off / turn friendly** — never destroyed violently. "Recharging!" not "killed."
- **Slightly believable, not realistic** (user's steer): the *gadget* can be whimsical as long as the *quad* sells the realism.
- **Visible on the drone** and swappable in the garage (the tool/blaster mount slot).

## The arsenal (progresses with the drone tier)

| Tier feel | Gadget | Effect | VFX / SFX |
|-----------|--------|--------|-----------|
| T0 junk | **Rubber-band flicker / foam-dart popper** | knocks a bot back a little | *thwip*, foam dart arcs |
| early | **Water squirter** | soaks a bot → it sputters & droops | splash, drips |
| mid | **Paint-splat blaster** | marks bots with color; enough splats → powered down | colorful splat decals |
| mid | **Bubble cannon** | traps a bot in a floaty bubble briefly | shimmer, pop |
| later | **Snowball launcher** | frosty bonk, brief freeze | puff of snow |
| T5 advanced | **"Sparkle beam"** | gentle energy sparkle → bot powers off with a twinkle | soft glow, chimes |

Each is a garage-swappable mount; **payload weight is a trade-off** (a big bubble cannon = more fun, less agile — ties into the learning system).

## Non-combat tools (for the mixed missions)

Because combat is only one mode (delivery, rescue, races, photo), the mount also holds **tools**:

- **Grabber claw / delivery magnet** — pick up and drop cargo (delivery, rescue).
- **Net launcher** — scoop up runaway critters/bots (rescue).
- **Spotlight** — light dark areas (exploration).
- **Camera** — line up shots (photo missions).

Same slot, same "swap in the garage" flow — the drone becomes a multi-tool, not just a blaster.

## Feel & feedback (kid-friendly juice)

- Big, colorful, exaggerated VFX; bouncy SFX; screen-friendly (no harsh flashes).
- Clear "you did it!" feedback on every hit (sparkles, happy sound), and celebratory, not gory, on bot power-down.
- Generous aim-assist (existing soft-lock) so little kids land hits and feel great.

## Implementation hooks

- Reuses `DroneWeapon` (projectile pooling) — projectiles become foam/water/bubble/paint/snowball/sparkle with playful materials + `Vfx`/`Sfx` variants.
- Enemy "defeat" swaps the death-pop for a **power-down** state (float down / deactivate / turn friendly) — a gentler `TargetHealth` death path for the kid tone.
- Mount is a visible child mesh on the drone, set by the garage's tool slot; progresses per tier.
- Weapon/tool stats (rate, "knockback," payload weight) live in the component system.

## Art notes (per tier mount)

Model a mount on each player tier (see `build_models.py`): T0 = taped foam popper (tan tube + red band); mid tiers = clip-on colorful blaster; T5 = flush glowing emitter. Keep it small so the quad still reads as the hero.
